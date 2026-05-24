namespace AchWorker.UseCases.CollectPendingPayments;

public interface ICollectPendingPaymentsOutputBoundary
{
    void Present(CollectPendingPaymentsResponseModel response);
}
