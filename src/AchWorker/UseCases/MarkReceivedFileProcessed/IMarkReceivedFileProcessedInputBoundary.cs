namespace AchWorker.UseCases.MarkReceivedFileProcessed;

public interface IMarkReceivedFileProcessedInputBoundary
{
    Task MarkReceivedFileProcessedAsync(IMarkReceivedFileProcessedOutputBoundary presenter, MarkReceivedFileProcessedRequestModel request);
}
