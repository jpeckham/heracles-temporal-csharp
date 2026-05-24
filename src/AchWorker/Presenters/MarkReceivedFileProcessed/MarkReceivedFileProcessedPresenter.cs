using AchWorker.UseCases.MarkReceivedFileProcessed;

namespace AchWorker.Presenters.MarkReceivedFileProcessed;

public class MarkReceivedFileProcessedPresenter : IMarkReceivedFileProcessedOutputBoundary
{
    public MarkReceivedFileProcessedResponseModel? ViewModel { get; private set; }

    public void Present(MarkReceivedFileProcessedResponseModel response)
    {
        ViewModel = response;
    }
}
