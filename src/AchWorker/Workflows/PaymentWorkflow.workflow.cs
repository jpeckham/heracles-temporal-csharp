using AchWorker.Activities;
using AchWorker.Services;
using Shared.Contracts;
using Temporalio.Activities;
using Temporalio.Exceptions;
using Temporalio.Workflows;

namespace AchWorker.Workflows;

[Workflow]
public class PaymentWorkflow
{
    private BatchDetails? _batchDetails;
    private AchReturnDetails? _bankReturn;
    private int _representmentCount;

    [WorkflowInit]
    public PaymentWorkflow(Guid paymentId, bool allowsRepresentment)
    {
        PaymentId = paymentId;
        AllowsRepresentment = allowsRepresentment;
    }

    private Guid PaymentId { get; }
    private bool AllowsRepresentment { get; }

    [WorkflowSignal]
    public async Task AddedToBatchAsync(BatchDetails details)
    {
        _batchDetails = details;
    }

    [WorkflowSignal]
    public async Task BankReturnAsync(AchReturnDetails details)
    {
        _bankReturn = details;
    }

    [WorkflowQuery]
    public string GetStatus()
    {
        if (_batchDetails is null) return "AwaitingBatch";
        if (_bankReturn is null) return "AwaitingSettlement";
        return _representmentCount > 0 ? "Representment" : "Returned";
    }

    [WorkflowRun]
    public async Task RunAsync(Guid paymentId, bool allowsRepresentment)
    {
        var activityOptions = new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(2) };

        await Workflow.ExecuteActivityAsync(
            (PaymentActivities a) => a.HardAuthAsync(paymentId),
            activityOptions);

        // Wait for inclusion in a batch
        await Workflow.WaitConditionAsync(() => _batchDetails != null);

        while (true)
        {
            _bankReturn = null;

            // Use Workflow.UtcNow for determinism
            var returnWindow = BankingCalendar.GetReturnWindow(Workflow.UtcNow, _batchDetails!.IsSameDayAch);

            var returned = await Workflow.WaitConditionAsync(
                () => _bankReturn != null, returnWindow);

            if (!returned)
            {
                // Timer expired — no return received, settle the payment
                await Workflow.ExecuteActivityAsync(
                    (PaymentActivities a) => a.RecordSettlementAsync(paymentId),
                    activityOptions);
                return;
            }

            // Bank return received
            await Workflow.ExecuteActivityAsync(
                (PaymentActivities a) => a.RecordAchReturnAsync(paymentId, _bankReturn!),
                activityOptions);

            var isRepresentable = _bankReturn!.RCode == "R01";

            if (!isRepresentable || !allowsRepresentment || _representmentCount >= 2)
            {
                Workflow.Logger.LogWarning(
                    "Payment {PaymentId} terminal after return {RCode}, representments: {Count}",
                    paymentId, _bankReturn.RCode, _representmentCount);
                return;
            }

            _representmentCount++;
            await Workflow.ExecuteActivityAsync(
                (PaymentActivities a) => a.RecordRepresentmentAsync(paymentId, _representmentCount),
                activityOptions);

            _batchDetails = null;
            await Workflow.WaitConditionAsync(() => _batchDetails != null);
        }
    }
}
