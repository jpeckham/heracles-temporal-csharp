using AchWorker.UseCases.CreateAchFile;

namespace AchWorker.Presenters.CreateAchFile;

public class CreateAchFilePresenter : ICreateAchFileOutputBoundary
{
    public CreateAchFileResponseModel? ViewModel { get; private set; }

    public void Present(CreateAchFileResponseModel response)
    {
        ViewModel = response;
    }
}
