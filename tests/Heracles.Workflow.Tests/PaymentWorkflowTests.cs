using AchWorker.Activities;
using AchWorker.Workflows;
using Shared.Contracts;
using Temporalio.Activities;
using Temporalio.Client;
using Temporalio.Exceptions;
using Temporalio.Testing;
using Temporalio.Worker;

namespace Heracles.Workflow.Tests;

public class PaymentWorkflowTests
{
    [Fact]
    public async Task NoReturn_TimerExpires_RecordsSettlement()
    {
        var paymentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var settlementCalled = false;

        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();

        var workerOptions = new TemporalWorkerOptions($"test-{Guid.NewGuid()}")
            .AddWorkflow<PaymentWorkflow>()
            .AddAllActivities(new MockHardAuth(_ => Task.CompletedTask))
            .AddAllActivities(new MockRecordSettlement(id =>
            {
                settlementCalled = id == paymentId;
                return Task.CompletedTask;
            }))
            .AddAllActivities(new MockRecordAchReturn((_, _) => Task.CompletedTask))
            .AddAllActivities(new MockRecordRepresentment((_, _) => Task.CompletedTask))
            .AddAllActivities(new MockSignalPaymentAddedToBatch((_, _, _) => Task.CompletedTask))
            .AddAllActivities(new MockSignalBankReturn((_, _) => Task.CompletedTask))
            .AddAllActivities(new MockVoidPaymentAuthIfExists(_ => Task.CompletedTask));

        using var worker = new TemporalWorker(env.Client, workerOptions);

        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync(
                (PaymentWorkflow wf) => wf.RunAsync(paymentId, false),
                new WorkflowOptions(id: $"payment-{paymentId}", taskQueue: worker.Options.TaskQueue!));

            // Signal workflow that it was added to a batch (standard ACH)
            await handle.SignalAsync(wf => wf.AddedToBatchAsync(new BatchDetails(fileId, false)));

            // Time-skipping env will skip past the return window automatically
            await handle.GetResultAsync();

            Assert.True(settlementCalled);
        });
    }

    [Fact]
    public async Task R01Return_WithRepresentmentAllowed_RecordsRepresentment()
    {
        var paymentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var representmentCalled = false;
        var returnCalled = false;

        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();

        var workerOptions = new TemporalWorkerOptions($"test-{Guid.NewGuid()}")
            .AddWorkflow<PaymentWorkflow>()
            .AddAllActivities(new MockHardAuth(_ => Task.CompletedTask))
            .AddAllActivities(new MockRecordSettlement(_ => Task.CompletedTask))
            .AddAllActivities(new MockRecordAchReturn((id, details) =>
            {
                returnCalled = true;
                return Task.CompletedTask;
            }))
            .AddAllActivities(new MockRecordRepresentment((id, count) =>
            {
                representmentCalled = id == paymentId && count == 1;
                return Task.CompletedTask;
            }))
            .AddAllActivities(new MockSignalPaymentAddedToBatch((_, _, _) => Task.CompletedTask))
            .AddAllActivities(new MockSignalBankReturn((_, _) => Task.CompletedTask))
            .AddAllActivities(new MockVoidPaymentAuthIfExists(_ => Task.CompletedTask));

        using var worker = new TemporalWorker(env.Client, workerOptions);

        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync(
                (PaymentWorkflow wf) => wf.RunAsync(paymentId, true),
                new WorkflowOptions(id: $"payment-{paymentId}", taskQueue: worker.Options.TaskQueue!));

            // Signal added to first batch
            await handle.SignalAsync(wf => wf.AddedToBatchAsync(new BatchDetails(fileId, false)));

            // Small delay to let workflow process signal
            await Task.Delay(100);

            // Send R01 return (representable)
            var returnDetails = new AchReturnDetails(paymentId, "R01", "Insufficient funds");
            await handle.SignalAsync(wf => wf.BankReturnAsync(returnDetails));

            await Task.Delay(100);

            // Signal added to second batch (representment cycle)
            await handle.SignalAsync(wf => wf.AddedToBatchAsync(new BatchDetails(fileId, false)));

            // Let time-skipping skip past second return window - no return means settlement
            await handle.GetResultAsync();

            Assert.True(returnCalled, "RecordAchReturn should have been called");
            Assert.True(representmentCalled, "RecordRepresentment should have been called");
        });
    }

    [Fact]
    public async Task R02Return_HardFailure_WorkflowEnds()
    {
        var paymentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var representmentCalled = false;
        var settlementCalled = false;

        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();

        var workerOptions = new TemporalWorkerOptions($"test-{Guid.NewGuid()}")
            .AddWorkflow<PaymentWorkflow>()
            .AddAllActivities(new MockHardAuth(_ => Task.CompletedTask))
            .AddAllActivities(new MockRecordSettlement(_ =>
            {
                settlementCalled = true;
                return Task.CompletedTask;
            }))
            .AddAllActivities(new MockRecordAchReturn((_, _) => Task.CompletedTask))
            .AddAllActivities(new MockRecordRepresentment((_, _) =>
            {
                representmentCalled = true;
                return Task.CompletedTask;
            }))
            .AddAllActivities(new MockSignalPaymentAddedToBatch((_, _, _) => Task.CompletedTask))
            .AddAllActivities(new MockSignalBankReturn((_, _) => Task.CompletedTask))
            .AddAllActivities(new MockVoidPaymentAuthIfExists(_ => Task.CompletedTask));

        using var worker = new TemporalWorker(env.Client, workerOptions);

        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync(
                (PaymentWorkflow wf) => wf.RunAsync(paymentId, true),
                new WorkflowOptions(id: $"payment-{paymentId}", taskQueue: worker.Options.TaskQueue!));

            // Signal added to batch
            await handle.SignalAsync(wf => wf.AddedToBatchAsync(new BatchDetails(fileId, false)));

            await Task.Delay(100);

            // Send R02 return (non-representable)
            var returnDetails = new AchReturnDetails(paymentId, "R02", "Account closed");
            await handle.SignalAsync(wf => wf.BankReturnAsync(returnDetails));

            // Workflow should end without representment
            await handle.GetResultAsync();

            Assert.False(representmentCalled, "RecordRepresentment should NOT have been called for R02");
            Assert.False(settlementCalled, "RecordSettlement should NOT have been called for R02 terminal");
        });
    }

    // Mock activity classes

    private class MockHardAuth(Func<Guid, Task> impl)
    {
        [Activity("HardAuth")]
        public Task HardAuthAsync(Guid paymentId) => impl(paymentId);
    }

    private class MockRecordSettlement(Func<Guid, Task> impl)
    {
        [Activity("RecordSettlement")]
        public Task RecordSettlementAsync(Guid paymentId) => impl(paymentId);
    }

    private class MockRecordAchReturn(Func<Guid, AchReturnDetails, Task> impl)
    {
        [Activity("RecordAchReturn")]
        public Task RecordAchReturnAsync(Guid paymentId, AchReturnDetails details) => impl(paymentId, details);
    }

    private class MockRecordRepresentment(Func<Guid, int, Task> impl)
    {
        [Activity("RecordRepresentment")]
        public Task RecordRepresentmentAsync(Guid paymentId, int count) => impl(paymentId, count);
    }

    private class MockSignalPaymentAddedToBatch(Func<Guid, Guid, bool, Task> impl)
    {
        [Activity("SignalPaymentAddedToBatch")]
        public Task SignalPaymentAddedToBatchAsync(Guid paymentId, Guid achFileId, bool isSameDayAch) =>
            impl(paymentId, achFileId, isSameDayAch);
    }

    private class MockSignalBankReturn(Func<Guid, AchReturnDetails, Task> impl)
    {
        [Activity("SignalBankReturn")]
        public Task SignalBankReturnAsync(Guid paymentId, AchReturnDetails details) => impl(paymentId, details);
    }

    private class MockVoidPaymentAuthIfExists(Func<Guid, Task> impl)
    {
        [Activity("VoidPaymentAuthIfExists")]
        public Task VoidPaymentAuthIfExistsAsync(Guid paymentId) => impl(paymentId);
    }
}
