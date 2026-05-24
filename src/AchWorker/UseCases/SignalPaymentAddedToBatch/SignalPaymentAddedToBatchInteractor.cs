using AchWorker.Gateways;

namespace AchWorker.UseCases.SignalPaymentAddedToBatch;

public class SignalPaymentAddedToBatchInteractor(IPaymentSignalGateway signalGateway) : ISignalPaymentAddedToBatchInputBoundary
{
    public async Task SignalPaymentAddedToBatchAsync(ISignalPaymentAddedToBatchOutputBoundary presenter, SignalPaymentAddedToBatchRequestModel request)
    {
        await signalGateway.SignalAddedToBatchAsync(request.PaymentId, request.AchFileId, request.IsSameDayAch);
        presenter.Present(new SignalPaymentAddedToBatchResponseModel());
    }
}
