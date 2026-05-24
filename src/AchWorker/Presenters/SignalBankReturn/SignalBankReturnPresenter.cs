using AchWorker.UseCases.SignalBankReturn;

namespace AchWorker.Presenters.SignalBankReturn;

public class SignalBankReturnPresenter : ISignalBankReturnOutputBoundary
{
    public SignalBankReturnResponseModel? ViewModel { get; private set; }

    public void Present(SignalBankReturnResponseModel response)
    {
        ViewModel = response;
    }
}
