namespace AchWorker.UseCases.SignalPaymentAddedToBatch;

public interface ISignalPaymentAddedToBatchInputBoundary
{
    Task SignalPaymentAddedToBatchAsync(ISignalPaymentAddedToBatchOutputBoundary presenter, SignalPaymentAddedToBatchRequestModel request);
}
