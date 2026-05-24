using AchWorker.UseCases.RevertAchFileToDraft;

namespace AchWorker.Presenters.RevertAchFileToDraft;

public class RevertAchFileToDraftPresenter : IRevertAchFileToDraftOutputBoundary
{
    public RevertAchFileToDraftResponseModel? ViewModel { get; private set; }

    public void Present(RevertAchFileToDraftResponseModel response)
    {
        ViewModel = response;
    }
}
