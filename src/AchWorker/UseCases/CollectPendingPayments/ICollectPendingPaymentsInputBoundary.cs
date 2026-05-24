namespace AchWorker.UseCases.CollectPendingPayments;

public interface ICollectPendingPaymentsInputBoundary
{
    Task CollectPendingPaymentsAsync(ICollectPendingPaymentsOutputBoundary presenter, CollectPendingPaymentsRequestModel request);
}
