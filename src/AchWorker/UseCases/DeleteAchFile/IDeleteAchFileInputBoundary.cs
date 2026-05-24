namespace AchWorker.UseCases.DeleteAchFile;

public interface IDeleteAchFileInputBoundary
{
    Task DeleteAchFileAsync(IDeleteAchFileOutputBoundary presenter, DeleteAchFileRequestModel request);
}
