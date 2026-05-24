namespace AchWorker.UseCases.MarkReceivedFileProcessed;

public interface IMarkReceivedFileProcessedOutputBoundary
{
    void Present(MarkReceivedFileProcessedResponseModel response);
}
