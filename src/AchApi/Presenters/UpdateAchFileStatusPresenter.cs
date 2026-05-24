using AchApi.UseCases.UpdateAchFileStatus;

namespace AchApi.Presenters;

public class UpdateAchFileStatusPresenter : IUpdateAchFileStatusOutputBoundary
{
    public bool NotFound { get; private set; }
    public string? BadRequestMessage { get; private set; }

    public void Present(UpdateAchFileStatusResponseModel response) { }
    public void PresentNotFound() => NotFound = true;
    public void PresentBadRequest(string message) => BadRequestMessage = message;
}
