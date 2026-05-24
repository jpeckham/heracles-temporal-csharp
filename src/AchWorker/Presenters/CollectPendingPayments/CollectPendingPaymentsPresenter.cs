using AchWorker.UseCases.CollectPendingPayments;

namespace AchWorker.Presenters.CollectPendingPayments;

public class CollectPendingPaymentsPresenter : ICollectPendingPaymentsOutputBoundary
{
    public CollectPendingPaymentsResponseModel? ViewModel { get; private set; }

    public void Present(CollectPendingPaymentsResponseModel response)
    {
        ViewModel = response;
    }
}
