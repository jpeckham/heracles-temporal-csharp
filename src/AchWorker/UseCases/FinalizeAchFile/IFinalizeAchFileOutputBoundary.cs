namespace AchWorker.UseCases.FinalizeAchFile;

public interface IFinalizeAchFileOutputBoundary
{
    void Present(FinalizeAchFileResponseModel response);
}
