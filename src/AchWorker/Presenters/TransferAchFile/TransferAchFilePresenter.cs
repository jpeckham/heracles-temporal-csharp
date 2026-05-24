using AchWorker.UseCases.TransferAchFile;

namespace AchWorker.Presenters.TransferAchFile;

public class TransferAchFilePresenter : ITransferAchFileOutputBoundary
{
    public TransferAchFileResponseModel? ViewModel { get; private set; }

    public void Present(TransferAchFileResponseModel response)
    {
        ViewModel = response;
    }
}
