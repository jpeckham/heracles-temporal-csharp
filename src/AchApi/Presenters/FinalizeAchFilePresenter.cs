using AchApi.UseCases.FinalizeAchFile;

namespace AchApi.Presenters;

public class FinalizeAchFilePresenter : IFinalizeAchFileOutputBoundary
{
    public FinalizeAchFileViewModel? ViewModel { get; private set; }
    public bool NotFound { get; private set; }
    public string? BadRequestMessage { get; private set; }

    public void Present(FinalizeAchFileResponseModel response)
    {
        ViewModel = new FinalizeAchFileViewModel(response.FileId, response.Status, response.ContentBase64);
    }

    public void PresentNotFound() => NotFound = true;
    public void PresentBadRequest(string message) => BadRequestMessage = message;
}
