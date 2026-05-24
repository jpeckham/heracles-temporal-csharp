namespace AchWorker.UseCases.SignalBankReturn;

public interface ISignalBankReturnInputBoundary
{
    Task SignalBankReturnAsync(ISignalBankReturnOutputBoundary presenter, SignalBankReturnRequestModel request);
}
