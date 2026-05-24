namespace AchWorker.UseCases.RevertAchFileToDraft;

public interface IRevertAchFileToDraftOutputBoundary
{
    void Present(RevertAchFileToDraftResponseModel response);
}
