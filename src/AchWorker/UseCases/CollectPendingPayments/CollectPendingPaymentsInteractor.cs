using AchWorker.Gateways;

namespace AchWorker.UseCases.CollectPendingPayments;

public class CollectPendingPaymentsInteractor(IPaymentGateway paymentGateway) : ICollectPendingPaymentsInputBoundary
{
    public async Task CollectPendingPaymentsAsync(ICollectPendingPaymentsOutputBoundary presenter, CollectPendingPaymentsRequestModel request)
    {
        var ids = await paymentGateway.CollectPendingAsync();
        presenter.Present(new CollectPendingPaymentsResponseModel(ids));
    }
}
