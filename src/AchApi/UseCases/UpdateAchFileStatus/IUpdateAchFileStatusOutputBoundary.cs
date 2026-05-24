namespace AchApi.UseCases.UpdateAchFileStatus;

public interface IUpdateAchFileStatusOutputBoundary
{
    void Present(UpdateAchFileStatusResponseModel response);
    void PresentNotFound();
    void PresentBadRequest(string message);
}
