using AchWorker.Activities;
using Temporalio.Exceptions;
using Temporalio.Workflows;

namespace AchWorker.Workflows;

[Workflow]
public class AchBatchWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        var compensations = new List<Func<Task>>();
        var shortTimeout = new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(2) };
        var longTimeout = new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(10) };

        try
        {
            // 1. Collect pending payments
            var paymentIds = await Workflow.ExecuteActivityAsync(
                (PaymentActivities a) => a.CollectPendingPaymentsAsync(), shortTimeout);

            if (paymentIds.Count == 0)
                throw new ApplicationFailureException("No pending payments to process.");

            // 2. Fan out HardAuth with semaphore (max 50 concurrent)
            var semaphore = new Temporalio.Workflows.Semaphore(50);
            var authResults = await Workflow.WhenAllAsync(
                paymentIds.Select(id => AuthorizeAsync(id, semaphore, compensations)));

            var authorized = authResults.Where(r => r.Success).Select(r => r.Id).ToList();
            if (authorized.Count == 0)
                throw new ApplicationFailureException("All payment authorizations failed.");

            Workflow.Logger.LogInformation("Authorized {Count}/{Total} payments",
                authorized.Count, paymentIds.Count);

            // 3. Create ACH file
            var fileId = Guid.Empty;
            compensations.Add(() => Workflow.ExecuteActivityAsync(
                (AchActivities a) => a.DeleteAchFileIfExistsAsync(fileId), shortTimeout));
            fileId = await Workflow.ExecuteActivityAsync(
                (AchActivities a) => a.CreateAchFileAsync(), shortTimeout);

            // 4. Fan out AddEntry (parallel)
            await Workflow.WhenAllAsync(
                authorized.Select(id => AddEntryAsync(fileId, id, shortTimeout)));

            // 5. Finalize
            compensations.Add(() => Workflow.ExecuteActivityAsync(
                (AchActivities a) => a.RevertAchFileToDraftAsync(fileId), shortTimeout));
            await Workflow.ExecuteActivityAsync(
                (AchActivities a) => a.FinalizeAchFileAsync(fileId), shortTimeout);

            // 6. Transfer to SFTP
            compensations.Add(() => Workflow.ExecuteActivityAsync(
                (SftpActivities a) => a.DeleteTransferredFileIfExistsAsync(fileId), shortTimeout));
            var transferredFileId = await Workflow.ExecuteActivityAsync(
                (SftpActivities a) => a.TransferAchFileAsync(fileId), longTimeout);

            Workflow.Logger.LogInformation("ACH file {FileId} transferred as {TransferredId}",
                fileId, transferredFileId);

            // 7. Signal each payment workflow
            var isSameDayAch = false; // standard ACH — 2 banking day return window
            await Workflow.WhenAllAsync(
                authorized.Select(id => Workflow.ExecuteActivityAsync(
                    (PaymentActivities a) => a.SignalPaymentAddedToBatchAsync(id, fileId, isSameDayAch),
                    shortTimeout)));
        }
        catch (Exception ex) when (!TemporalException.IsCanceledException(ex))
        {
            Workflow.Logger.LogError(ex, "AchBatchWorkflow failed, running compensation");
            compensations.Reverse();
            foreach (var comp in compensations)
            {
                try { await comp(); }
                catch (Exception ce) { Workflow.Logger.LogError(ce, "Compensation step failed"); }
            }
            throw;
        }
    }

    private async Task<AuthResult> AuthorizeAsync(
        Guid paymentId,
        Temporalio.Workflows.Semaphore semaphore,
        List<Func<Task>> compensations)
    {
        await semaphore.WaitAsync();
        try
        {
            var opts = new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(2) };
            compensations.Add(() => Workflow.ExecuteActivityAsync(
                (PaymentActivities a) => a.VoidPaymentAuthIfExistsAsync(paymentId), opts));
            await Workflow.ExecuteActivityAsync(
                (PaymentActivities a) => a.HardAuthAsync(paymentId), opts);
            return new AuthResult(paymentId, true);
        }
        catch (ActivityFailureException ex)
        {
            Workflow.Logger.LogWarning(ex, "HardAuth failed for payment {PaymentId}", paymentId);
            return new AuthResult(paymentId, false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static async Task AddEntryAsync(Guid fileId, Guid paymentId, ActivityOptions opts)
    {
        await Workflow.ExecuteActivityAsync(
            (AchActivities a) => a.AddEntryAsync(fileId, paymentId, 0), opts);
    }

    private record AuthResult(Guid Id, bool Success);
}
