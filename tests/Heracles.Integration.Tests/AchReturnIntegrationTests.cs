using AchWorker.Workflows;
using Shared.Contracts;
using Shared.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Temporalio.Client;
using Xunit;

namespace Heracles.Integration.Tests;

public class AchReturnIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task InboundReturnFile_SignalsPaymentWorkflow_RecordsAchReturn()
    {
        await Worker.ExecuteAsync(async () =>
        {
            // 1. Create a payment
            var req = new CreatePaymentRequest("021000021", "99887766", "Jane Doe",
                250.00m, PaymentType.Credit, AllowsRepresentment: false);
            var resp = await PaymentClient.PostAsJsonAsync("/payments", req, JsonOpts);
            resp.EnsureSuccessStatusCode();
            var created = await resp.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOpts);
            var paymentId = created!.PaymentId;

            await Task.Delay(500);

            // 2. Run batch to include payment
            var batchHandle = await TemporalEnv.Client.StartWorkflowAsync(
                (AchBatchWorkflow wf) => wf.RunAsync(),
                new WorkflowOptions(id: $"ach-batch-return-test-{Guid.NewGuid()}", taskQueue: "ach-worker"));
            await batchHandle.GetResultAsync();

            await Task.Delay(300);

            // 3. Simulate inbound NACHA return file with R01 for this payment.
            // The parser reads: line[3..6] = rCode, line[13..49] = paymentId (with hyphens).
            // Format: '7' + 2 chars padding + "R01" + 7 chars padding + paymentId (36 chars with hyphens) + padding to 94
            var paymentIdStr = paymentId.ToString(); // 36 chars with hyphens
            var returnLine = ("7  R01       " + paymentIdStr).PadRight(94);
            var returnContent = Convert.ToBase64String(Encoding.ASCII.GetBytes(returnLine + "\n"));

            var inboundResp = await SftpClient.PostAsJsonAsync("/files/inbound", new
            {
                FileName = "return_20260520.txt",
                ContentBase64 = returnContent
            });
            inboundResp.EnsureSuccessStatusCode();

            // Give AchReturnWorkflow time to process
            await Task.Delay(2000);

            // 4. Assert payment has AchReturn activity
            var paymentResp = await PaymentClient.GetAsync($"/payments/{paymentId}");
            paymentResp.EnsureSuccessStatusCode();
            var payment = await paymentResp.Content.ReadFromJsonAsync<PaymentDetailResponse>(JsonOpts);
            Assert.NotNull(payment);
            Assert.Contains(payment.Activities, a => a.Type == PaymentActivityType.AchReturn);
        });
    }

    private record PaymentCreatedResponse(Guid PaymentId);
    private record PaymentDetailResponse(Guid PaymentId, List<ActivityEntry> Activities);
    private record ActivityEntry(PaymentActivityType Type, string? ReferenceCode);
}
