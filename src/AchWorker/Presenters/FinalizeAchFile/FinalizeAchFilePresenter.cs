using AchWorker.UseCases.FinalizeAchFile;

namespace AchWorker.Presenters.FinalizeAchFile;

public class FinalizeAchFilePresenter : IFinalizeAchFileOutputBoundary
{
    public FinalizeAchFileResponseModel? ViewModel { get; private set; }

    public void Present(FinalizeAchFileResponseModel response)
    {
        ViewModel = response;
    }
}
