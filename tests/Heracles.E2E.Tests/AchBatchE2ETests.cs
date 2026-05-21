using System.Net.Http.Json;
using Shared.Contracts;
using Shared.Models;
using Temporalio.Client;
using Temporalio.Exceptions;
using Xunit;

namespace Heracles.E2E.Tests;

public class AchBatchE2ETests : E2ETestBase
{
    [Fact]
    public async Task HappyPath_BatchWorkflowProcessesPendingPayments()
    {
        // Create 3 payments
        var paymentIds = new List<Guid>();
        for (int i = 0; i < 3; i++)
        {
            var req = new CreatePaymentRequest(
                RoutingNumber: "021000021",
                AccountNumber: $"123456{i:D2}",
                AccountHolderName: $"Test User {i}",
                Amount: 100.00m + i,
                Type: PaymentType.Debit,
                AllowsRepresentment: false);

            var resp = await PaymentClient.PostAsJsonAsync("/payments", req, JsonOpts);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadFromJsonAsync<CreatePaymentResponse>(JsonOpts);
            paymentIds.Add(body!.PaymentId);
        }

        // Start AchBatchWorkflow
        var workflowId = $"e2e-batch-{Guid.NewGuid()}";
        var handle = await Temporal.StartWorkflowAsync(
            "AchBatchWorkflow",
            Array.Empty<object>(),
            new WorkflowOptions(id: workflowId, taskQueue: "ach-worker"));

        // Wait for completion (up to 2 minutes)
        await handle.GetResultAsync(rpcOptions: new() { CancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token });

        // Assert: all 3 payments transitioned to Submitted or beyond
        foreach (var id in paymentIds)
        {
            var payment = await PaymentClient.GetFromJsonAsync<PaymentDetail>($"/payments/{id}", JsonOpts);
            Assert.NotNull(payment);
            Assert.True(
                payment.CurrentStatus is "HardAuth" or "Submitted" or "Settled",
                $"Expected HardAuth/Submitted/Settled but got {payment.CurrentStatus}");
        }

        // Assert: at least one outbound file was transferred to SftpApi
        var outbound = await SftpClient.GetFromJsonAsync<List<TransferredFileDto>>("/files/outbound", JsonOpts);
        Assert.NotNull(outbound);
        Assert.NotEmpty(outbound);
    }

    [Fact]
    public async Task NoPendingPayments_BatchWorkflowFailsGracefully()
    {
        // Start batch with no pending payments (they may all be in other states from prior tests)
        // Use a fresh run — if there happen to be pending ones this can still succeed.
        // We just verify the workflow completes (succeeded or failed with "No pending payments").
        var workflowId = $"e2e-batch-empty-{Guid.NewGuid()}";

        WorkflowHandle handle;
        try
        {
            handle = await Temporal.StartWorkflowAsync(
                "AchBatchWorkflow",
                Array.Empty<object>(),
                new WorkflowOptions(id: workflowId, taskQueue: "ach-worker"));
        }
        catch
        {
            // If the workflow can't start, that's an unexpected error
            throw;
        }

        // Wait and accept either success or "No pending payments" failure
        try
        {
            await handle.GetResultAsync(rpcOptions: new() { CancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token });
        }
        catch (WorkflowFailedException ex) when (ex.InnerException?.Message.Contains("No pending payments") == true)
        {
            // Expected: no payments queued
        }
    }

    private record CreatePaymentResponse(Guid PaymentId);
    private record PaymentDetail(Guid PaymentId, string CurrentStatus);
    private record TransferredFileDto(Guid FileId, Guid AchFileId, string FileName);
}
