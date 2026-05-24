namespace AchWorker.UseCases.CreateAchFile;

public interface ICreateAchFileOutputBoundary
{
    void Present(CreateAchFileResponseModel response);
}
