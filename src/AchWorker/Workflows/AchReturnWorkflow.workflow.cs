using AchWorker.Activities;
using Shared.Contracts;
using Temporalio.Workflows;

namespace AchWorker.Workflows;

[Workflow]
public class AchReturnWorkflow
{
    [WorkflowRun]
    public async Task RunAsync(Guid receivedFileId)
    {
        var shortTimeout = new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(5) };

        var returnRecords = await Workflow.ExecuteActivityAsync(
            (AchActivities a) => a.ParseReturnFileAsync(receivedFileId), shortTimeout);

        Workflow.Logger.LogInformation("Processing {Count} ACH return records", returnRecords.Count);

        await Workflow.WhenAllAsync(returnRecords.Select(record =>
            ProcessReturnAsync(record, shortTimeout)));

        await Workflow.ExecuteActivityAsync(
            (SftpActivities a) => a.MarkReceivedFileProcessedAsync(receivedFileId), shortTimeout);
    }

    private async Task ProcessReturnAsync(
        AchActivities.AchReturnRecordDto record,
        ActivityOptions opts)
    {
        var details = new AchReturnDetails(record.PaymentId, record.RCode);

        try
        {
            await Workflow.ExecuteActivityAsync(
                (PaymentActivities a) => a.RecordAchReturnAsync(record.PaymentId, details), opts);
        }
        catch (Exception ex)
        {
            Workflow.Logger.LogWarning(ex, "Failed to record return for payment {Id}", record.PaymentId);
        }

        try
        {
            await Workflow.ExecuteActivityAsync(
                (PaymentActivities a) => a.SignalBankReturnAsync(record.PaymentId, details), opts);
        }
        catch (Exception ex)
        {
            Workflow.Logger.LogWarning(ex, "Could not signal PaymentWorkflow for {Id}", record.PaymentId);
        }
    }
}
