using AchWorker.UseCases.DeleteAchFile;

namespace AchWorker.Presenters.DeleteAchFile;

public class DeleteAchFilePresenter : IDeleteAchFileOutputBoundary
{
    public DeleteAchFileResponseModel? ViewModel { get; private set; }

    public void Present(DeleteAchFileResponseModel response)
    {
        ViewModel = response;
    }
}
