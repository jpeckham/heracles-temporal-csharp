namespace AchWorker.UseCases.RevertAchFileToDraft;

public interface IRevertAchFileToDraftInputBoundary
{
    Task RevertAchFileToDraftAsync(IRevertAchFileToDraftOutputBoundary presenter, RevertAchFileToDraftRequestModel request);
}
