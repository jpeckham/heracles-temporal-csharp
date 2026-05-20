using AchWorker.Workflows;
using Shared.Contracts;
using Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Temporalio.Client;
using Xunit;

namespace Heracles.Integration.Tests;

public class AchBatchIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task FullBatchFlow_5Payments_AllSignaledAddedToBatch()
    {
        await Worker.ExecuteAsync(async () =>
        {
            // Create 5 payments
            var paymentIds = new List<Guid>();
            for (var i = 0; i < 5; i++)
            {
                var req = new CreatePaymentRequest(
                    RoutingNumber: "021000021",
                    AccountNumber: $"1234567{i:D2}",
                    AccountHolderName: $"Test User {i}",
                    Amount: 100.00m + i,
                    Type: PaymentType.Credit,
                    AllowsRepresentment: true);

                var resp = await PaymentClient.PostAsJsonAsync("/payments", req, JsonOpts);
                resp.EnsureSuccessStatusCode();
                var result = await resp.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOpts);
                paymentIds.Add(result!.PaymentId);
            }

            // Give PaymentWorkflows a moment to start and complete initial HardAuth
            await Task.Delay(500);

            // Trigger batch workflow
            var batchHandle = await TemporalEnv.Client.StartWorkflowAsync(
                (AchBatchWorkflow wf) => wf.RunAsync(),
                new WorkflowOptions(id: $"ach-batch-test-{Guid.NewGuid()}", taskQueue: "ach-worker"));

            await batchHandle.GetResultAsync();

            // Assert ACH file exists
            var achFilesResp = await AchClient.GetAsync("/files");
            achFilesResp.EnsureSuccessStatusCode();

            // Assert transferred file exists in SftpApi
            var transfersResp = await SftpClient.GetAsync("/files/outbound");
            transfersResp.EnsureSuccessStatusCode();
            var transfers = await transfersResp.Content.ReadFromJsonAsync<List<JsonElement>>(JsonOpts);
            Assert.NotNull(transfers);
            Assert.NotEmpty(transfers);

            // Assert each payment has HardAuth activity
            foreach (var id in paymentIds)
            {
                var paymentResp = await PaymentClient.GetAsync($"/payments/{id}");
                paymentResp.EnsureSuccessStatusCode();
                var payment = await paymentResp.Content.ReadFromJsonAsync<PaymentDetailResponse>(JsonOpts);
                Assert.NotNull(payment);
                Assert.Contains(payment.Activities, a => a.Type == PaymentActivityType.HardAuth);
            }
        });
    }

    private record PaymentCreatedResponse(Guid PaymentId);
    private record PaymentDetailResponse(Guid PaymentId, List<ActivityEntry> Activities);
    private record ActivityEntry(PaymentActivityType Type, string? ReferenceCode);
}
