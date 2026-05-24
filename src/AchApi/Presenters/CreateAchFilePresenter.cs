using AchApi.UseCases.CreateAchFile;

namespace AchApi.Presenters;

public class CreateAchFilePresenter : ICreateAchFileOutputBoundary
{
    public CreateAchFileViewModel? ViewModel { get; private set; }

    public void Present(CreateAchFileResponseModel response)
    {
        ViewModel = new CreateAchFileViewModel(response.FileId, response.BatchNumber);
    }
}
