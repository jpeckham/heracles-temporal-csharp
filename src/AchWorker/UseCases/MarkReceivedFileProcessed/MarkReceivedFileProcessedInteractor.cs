using AchWorker.Gateways;

namespace AchWorker.UseCases.MarkReceivedFileProcessed;

public class MarkReceivedFileProcessedInteractor(ISftpGateway sftpGateway) : IMarkReceivedFileProcessedInputBoundary
{
    public async Task MarkReceivedFileProcessedAsync(IMarkReceivedFileProcessedOutputBoundary presenter, MarkReceivedFileProcessedRequestModel request)
    {
        await sftpGateway.MarkProcessedAsync(request.ReceivedFileId);
        presenter.Present(new MarkReceivedFileProcessedResponseModel());
    }
}
