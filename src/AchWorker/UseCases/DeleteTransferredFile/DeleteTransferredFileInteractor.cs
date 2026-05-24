using AchWorker.Gateways;

namespace AchWorker.UseCases.DeleteTransferredFile;

public class DeleteTransferredFileInteractor(ISftpGateway sftpGateway) : IDeleteTransferredFileInputBoundary
{
    public async Task DeleteTransferredFileAsync(IDeleteTransferredFileOutputBoundary presenter, DeleteTransferredFileRequestModel request)
    {
        await sftpGateway.DeleteTransferredAsync(request.AchFileId);
        presenter.Present(new DeleteTransferredFileResponseModel());
    }
}
