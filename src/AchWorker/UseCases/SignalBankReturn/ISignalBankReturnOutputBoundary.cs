namespace AchWorker.UseCases.SignalBankReturn;

public interface ISignalBankReturnOutputBoundary
{
    void Present(SignalBankReturnResponseModel response);
}
