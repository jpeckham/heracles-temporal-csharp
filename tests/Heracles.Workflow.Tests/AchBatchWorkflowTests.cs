using AchWorker.Activities;
using AchWorker.Workflows;
using Shared.Contracts;
using Temporalio.Activities;
using Temporalio.Client;
using Temporalio.Exceptions;
using Temporalio.Testing;
using Temporalio.Worker;

namespace Heracles.Workflow.Tests;

public class AchBatchWorkflowTests
{
    [Fact]
    public async Task NoPendingPayments_WorkflowFails()
    {
        await using var env = await WorkflowEnvironment.StartLocalAsync();

        var workerOptions = new TemporalWorkerOptions($"test-{Guid.NewGuid()}")
        {
            WorkflowFailureExceptionTypes = [typeof(Exception)]
        };
        workerOptions.AddWorkflow<AchBatchWorkflow>();
        workerOptions.AddAllActivities(new MockCollectPendingPayments(_ => Task.FromResult(new List<Guid>())));
        workerOptions.AddAllActivities(new MockHardAuth(_ => Task.CompletedTask));
        workerOptions.AddAllActivities(new MockVoidPaymentAuthIfExists(_ => Task.CompletedTask));
        workerOptions.AddAllActivities(new MockCreateAchFile(() => Task.FromResult(Guid.NewGuid())));
        workerOptions.AddAllActivities(new MockAddEntry((_, _, _) => Task.FromResult(Guid.NewGuid())));
        workerOptions.AddAllActivities(new MockFinalizeAchFile(_ => Task.CompletedTask));
        workerOptions.AddAllActivities(new MockTransferAchFile(_ => Task.FromResult(Guid.NewGuid())));
        workerOptions.AddAllActivities(new MockDeleteAchFileIfExists(_ => Task.CompletedTask));
        workerOptions.AddAllActivities(new MockRevertAchFileToDraft(_ => Task.CompletedTask));
        workerOptions.AddAllActivities(new MockDeleteTransferredFileIfExists(_ => Task.CompletedTask));
        workerOptions.AddAllActivities(new MockSignalPaymentAddedToBatch((_, _, _) => Task.CompletedTask));

        using var worker = new TemporalWorker(env.Client, workerOptions);

        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync(
                (AchBatchWorkflow wf) => wf.RunAsync(),
                new WorkflowOptions(id: $"ach-batch-{Guid.NewGuid()}", taskQueue: worker.Options.TaskQueue!));

            var ex = await Assert.ThrowsAsync<WorkflowFailedException>(() => handle.GetResultAsync());
            Assert.Contains("No pending payments", ex.Message + (ex.InnerException?.Message ?? ""));
        });
    }

    [Fact]
    public async Task HappyPath_5Payments_TransfersFile()
    {
        await using var env = await WorkflowEnvironment.StartLocalAsync();

        var paymentIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var fileId = Guid.NewGuid();
        var transferredFileId = Guid.NewGuid();
        var transferCalled = false;
        var signalledIds = new List<Guid>();

        var workerOptions = new TemporalWorkerOptions($"test-{Guid.NewGuid()}");
        workerOptions.AddWorkflow<AchBatchWorkflow>();
        workerOptions.AddAllActivities(new MockCollectPendingPayments(_ => Task.FromResult(paymentIds)));
        workerOptions.AddAllActivities(new MockHardAuth(_ => Task.CompletedTask));
        workerOptions.AddAllActivities(new MockVoidPaymentAuthIfExists(_ => Task.CompletedTask));
        workerOptions.AddAllActivities(new MockCreateAchFile(() => Task.FromResult(fileId)));
        workerOptions.AddAllActivities(new MockAddEntry((_, _, _) => Task.FromResult(Guid.NewGuid())));
        workerOptions.AddAllActivities(new MockFinalizeAchFile(_ => Task.CompletedTask));
        workerOptions.AddAllActivities(new MockTransferAchFile(id =>
        {
            transferCalled = true;
            return Task.FromResult(transferredFileId);
        }));
        workerOptions.AddAllActivities(new MockDeleteAchFileIfExists(_ => Task.CompletedTask));
        workerOptions.AddAllActivities(new MockRevertAchFileToDraft(_ => Task.CompletedTask));
        workerOptions.AddAllActivities(new MockDeleteTransferredFileIfExists(_ => Task.CompletedTask));
        workerOptions.AddAllActivities(new MockSignalPaymentAddedToBatch((paymentId, _, _) =>
        {
            lock (signalledIds) signalledIds.Add(paymentId);
            return Task.CompletedTask;
        }));

        using var worker = new TemporalWorker(env.Client, workerOptions);

        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync(
                (AchBatchWorkflow wf) => wf.RunAsync(),
                new WorkflowOptions(id: $"ach-batch-{Guid.NewGuid()}", taskQueue: worker.Options.TaskQueue!));

            await handle.GetResultAsync();

            Assert.True(transferCalled, "TransferAchFile should have been called");
            Assert.Equal(5, signalledIds.Count);
            foreach (var id in paymentIds)
                Assert.Contains(id, signalledIds);
        });
    }

    [Fact]
    public async Task SftpFailure_CompensationDeletesAchFile()
    {
        await using var env = await WorkflowEnvironment.StartLocalAsync();

        var paymentIds = Enumerable.Range(0, 2).Select(_ => Guid.NewGuid()).ToList();
        var fileId = Guid.NewGuid();
        var deleteAchFileCalled = false;
        var voidAuthCalled = false;

        var workerOptions = new TemporalWorkerOptions($"test-{Guid.NewGuid()}")
        {
            WorkflowFailureExceptionTypes = [typeof(Exception)]
        };
        workerOptions.AddWorkflow<AchBatchWorkflow>();
        workerOptions.AddAllActivities(new MockCollectPendingPayments(_ => Task.FromResult(paymentIds)));
        workerOptions.AddAllActivities(new MockHardAuth(_ => Task.CompletedTask));
        workerOptions.AddAllActivities(new MockVoidPaymentAuthIfExists(_ =>
        {
            voidAuthCalled = true;
            return Task.CompletedTask;
        }));
        workerOptions.AddAllActivities(new MockCreateAchFile(() => Task.FromResult(fileId)));
        workerOptions.AddAllActivities(new MockAddEntry((_, _, _) => Task.FromResult(Guid.NewGuid())));
        workerOptions.AddAllActivities(new MockFinalizeAchFile(_ => Task.CompletedTask));
        workerOptions.AddAllActivities(new MockTransferAchFile(_ =>
            throw new ApplicationFailureException("SFTP connection failed", nonRetryable: true)));
        workerOptions.AddAllActivities(new MockDeleteAchFileIfExists(_ =>
        {
            deleteAchFileCalled = true;
            return Task.CompletedTask;
        }));
        workerOptions.AddAllActivities(new MockRevertAchFileToDraft(_ => Task.CompletedTask));
        workerOptions.AddAllActivities(new MockDeleteTransferredFileIfExists(_ => Task.CompletedTask));
        workerOptions.AddAllActivities(new MockSignalPaymentAddedToBatch((_, _, _) => Task.CompletedTask));

        using var worker = new TemporalWorker(env.Client, workerOptions);

        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync(
                (AchBatchWorkflow wf) => wf.RunAsync(),
                new WorkflowOptions(id: $"ach-batch-{Guid.NewGuid()}", taskQueue: worker.Options.TaskQueue!));

            await Assert.ThrowsAsync<WorkflowFailedException>(() => handle.GetResultAsync());

            Assert.True(deleteAchFileCalled, "DeleteAchFileIfExists should have been called as compensation");
            Assert.True(voidAuthCalled, "VoidPaymentAuthIfExists should have been called as compensation");
        });
    }

    // Mock activity classes

    private class MockCollectPendingPayments(Func<object?, Task<List<Guid>>> impl)
    {
        [Activity("CollectPendingPayments")]
        public Task<List<Guid>> CollectPendingPaymentsAsync() => impl(null);
    }

    private class MockHardAuth(Func<Guid, Task> impl)
    {
        [Activity("HardAuth")]
        public Task HardAuthAsync(Guid paymentId) => impl(paymentId);
    }

    private class MockVoidPaymentAuthIfExists(Func<Guid, Task> impl)
    {
        [Activity("VoidPaymentAuthIfExists")]
        public Task VoidPaymentAuthIfExistsAsync(Guid paymentId) => impl(paymentId);
    }

    private class MockCreateAchFile(Func<Task<Guid>> impl)
    {
        [Activity("CreateAchFile")]
        public Task<Guid> CreateAchFileAsync() => impl();
    }

    private class MockAddEntry(Func<Guid, Guid, int, Task<Guid>> impl)
    {
        [Activity("AddEntry")]
        public Task<Guid> AddEntryAsync(Guid fileId, Guid paymentId, int representmentCount) =>
            impl(fileId, paymentId, representmentCount);
    }

    private class MockFinalizeAchFile(Func<Guid, Task> impl)
    {
        [Activity("FinalizeAchFile")]
        public Task FinalizeAchFileAsync(Guid fileId) => impl(fileId);
    }

    private class MockTransferAchFile(Func<Guid, Task<Guid>> impl)
    {
        [Activity("TransferAchFile")]
        public Task<Guid> TransferAchFileAsync(Guid achFileId) => impl(achFileId);
    }

    private class MockDeleteAchFileIfExists(Func<Guid, Task> impl)
    {
        [Activity("DeleteAchFileIfExists")]
        public Task DeleteAchFileIfExistsAsync(Guid fileId) => impl(fileId);
    }

    private class MockRevertAchFileToDraft(Func<Guid, Task> impl)
    {
        [Activity("RevertAchFileToDraft")]
        public Task RevertAchFileToDraftAsync(Guid fileId) => impl(fileId);
    }

    private class MockDeleteTransferredFileIfExists(Func<Guid, Task> impl)
    {
        [Activity("DeleteTransferredFileIfExists")]
        public Task DeleteTransferredFileIfExistsAsync(Guid achFileId) => impl(achFileId);
    }

    private class MockSignalPaymentAddedToBatch(Func<Guid, Guid, bool, Task> impl)
    {
        [Activity("SignalPaymentAddedToBatch")]
        public Task SignalPaymentAddedToBatchAsync(Guid paymentId, Guid achFileId, bool isSameDayAch) =>
            impl(paymentId, achFileId, isSameDayAch);
    }
}
