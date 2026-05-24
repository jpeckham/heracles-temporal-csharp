using AchApi.UseCases.GetAchFileContent;

namespace AchApi.Presenters;

public class GetAchFileContentPresenter : IGetAchFileContentOutputBoundary
{
    public GetAchFileContentViewModel? ViewModel { get; private set; }
    public bool NotFound { get; private set; }

    public void Present(GetAchFileContentResponseModel response)
    {
        ViewModel = new GetAchFileContentViewModel(response.ContentBase64);
    }

    public void PresentNotFound() => NotFound = true;
}
