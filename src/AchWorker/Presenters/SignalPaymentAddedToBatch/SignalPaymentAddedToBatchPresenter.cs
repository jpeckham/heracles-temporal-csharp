using AchWorker.UseCases.SignalPaymentAddedToBatch;

namespace AchWorker.Presenters.SignalPaymentAddedToBatch;

public class SignalPaymentAddedToBatchPresenter : ISignalPaymentAddedToBatchOutputBoundary
{
    public SignalPaymentAddedToBatchResponseModel? ViewModel { get; private set; }

    public void Present(SignalPaymentAddedToBatchResponseModel response)
    {
        ViewModel = response;
    }
}
