namespace AchWorker.UseCases.FinalizeAchFile;

public interface IFinalizeAchFileInputBoundary
{
    Task FinalizeAchFileAsync(IFinalizeAchFileOutputBoundary presenter, FinalizeAchFileRequestModel request);
}
