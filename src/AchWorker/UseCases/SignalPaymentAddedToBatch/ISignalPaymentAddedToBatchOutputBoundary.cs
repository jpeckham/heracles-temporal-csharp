namespace AchWorker.UseCases.SignalPaymentAddedToBatch;

public interface ISignalPaymentAddedToBatchOutputBoundary
{
    void Present(SignalPaymentAddedToBatchResponseModel response);
}
