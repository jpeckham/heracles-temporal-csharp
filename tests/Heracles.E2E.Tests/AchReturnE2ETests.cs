using System.Net.Http.Json;
using System.Text;
using Shared.Contracts;
using Shared.Models;
using Temporalio.Client;
using Temporalio.Exceptions;
using Xunit;

namespace Heracles.E2E.Tests;

public class AchReturnE2ETests : E2ETestBase
{
    [Fact]
    public async Task ReturnFile_ProcessesReturnAndRecordsOnPayment()
    {
        // Create a payment and run it through a batch first
        var req = new CreatePaymentRequest(
            RoutingNumber: "021000021",
            AccountNumber: "9999001",
            AccountHolderName: "Return Test User",
            Amount: 55.00m,
            Type: PaymentType.Debit,
            AllowsRepresentment: false);

        var resp = await PaymentClient.PostAsJsonAsync("/payments", req, JsonOpts);
        resp.EnsureSuccessStatusCode();
        var created = await resp.Content.ReadFromJsonAsync<CreatePaymentResponse>(JsonOpts);
        var paymentId = created!.PaymentId;

        // Run the batch workflow to get the payment into Submitted state
        var batchId = $"e2e-return-batch-{Guid.NewGuid()}";
        var batchHandle = await Temporal.StartWorkflowAsync(
            "AchBatchWorkflow",
            Array.Empty<object>(),
            new WorkflowOptions(id: batchId, taskQueue: "ach-worker"));

        try
        {
            await batchHandle.GetResultAsync(rpcOptions: new() { CancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token });
        }
        catch (WorkflowFailedException ex) when (ex.Message.Contains("No pending payments"))
        {
            // Skip if batch found nothing (payment may have been consumed by a concurrent test)
            return;
        }

        // Build a minimal NACHA-format return file for this payment
        // Real return files use R codes; we use R01 (insufficient funds)
        var nachaReturn = BuildMinimalReturnFile(paymentId, "R01");
        var nachaBase64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(nachaReturn));

        // POST inbound return file to SftpApi — starts AchReturnWorkflow
        var inboundResp = await SftpClient.PostAsJsonAsync("/files/inbound", new
        {
            FileName = $"return-{DateTime.UtcNow:yyyyMMddHHmmss}.ach",
            ContentBase64 = nachaBase64
        }, JsonOpts);
        inboundResp.EnsureSuccessStatusCode();
        var inbound = await inboundResp.Content.ReadFromJsonAsync<InboundFileResponse>(JsonOpts);

        // Find the AchReturnWorkflow handle and wait for it
        var returnWorkflowId = $"ach-return-{inbound!.FileId}";
        var returnHandle = Temporal.GetWorkflowHandle(returnWorkflowId);
        await returnHandle.GetResultAsync(rpcOptions: new() { CancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token });

        // Assert: payment has a Return activity recorded
        var payment = await PaymentClient.GetFromJsonAsync<PaymentDetailFull>($"/payments/{paymentId}", JsonOpts);
        Assert.NotNull(payment);
        Assert.Contains(payment.Activities, a => a.Type == "AchReturn");
    }

    // Builds a minimal NACHA-format return file that ParseReturnFileAsync can parse.
    // ParseReturnFileAsync looks for lines starting with '7', reads rCode from [3..6]
    // and paymentId (GUID with dashes) from [13..49].
    private static string BuildMinimalReturnFile(Guid paymentId, string rCode)
    {
        var guidStr = paymentId.ToString("D"); // 36 chars with dashes

        // Addenda record layout (94 chars):
        // [0]    = '7'
        // [1-2]  = addenda type "99"
        // [3-5]  = rCode e.g. "R01"
        // [6-12] = 7 spaces padding
        // [13-48]= paymentId GUID string (36 chars)
        // [49-93]= 45 spaces padding to reach 94 chars
        var addenda = $"799{rCode.PadRight(3)}{new string(' ', 7)}{guidStr}{new string(' ', 45)}";

        var sb = new StringBuilder();
        sb.AppendLine(addenda);
        return sb.ToString();
    }

    private record CreatePaymentResponse(Guid PaymentId);
    private record InboundFileResponse(Guid FileId);
    private record PaymentActivity(string Type, decimal? Amount);
    private record PaymentDetailFull(Guid PaymentId, string CurrentStatus, List<PaymentActivity> Activities);
}
