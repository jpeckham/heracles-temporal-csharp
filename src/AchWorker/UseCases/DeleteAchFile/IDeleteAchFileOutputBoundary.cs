namespace AchWorker.UseCases.DeleteAchFile;

public interface IDeleteAchFileOutputBoundary
{
    void Present(DeleteAchFileResponseModel response);
}
