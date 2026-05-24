using SftpApi.Gateways;

namespace SftpApi.UseCases.DeleteOutboundFile;

public class DeleteOutboundFileInteractor(ISftpFileGateway gateway) : IDeleteOutboundFileInputBoundary
{
    public async Task ExecuteAsync(IDeleteOutboundFileOutputBoundary presenter, DeleteOutboundFileRequestModel request)
    {
        await gateway.DeleteTransferredFileAsync(request.FileId);
        presenter.Present(new DeleteOutboundFileResponseModel());
    }
}
