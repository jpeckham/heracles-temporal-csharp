namespace AchApi.UseCases.FinalizeAchFile;

public interface IFinalizeAchFileOutputBoundary
{
    void Present(FinalizeAchFileResponseModel response);
    void PresentNotFound();
    void PresentBadRequest(string message);
}
