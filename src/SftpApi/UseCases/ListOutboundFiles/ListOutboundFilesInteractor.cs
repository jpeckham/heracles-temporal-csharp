using SftpApi.Gateways;

namespace SftpApi.UseCases.ListOutboundFiles;

public class ListOutboundFilesInteractor(ISftpFileGateway gateway) : IListOutboundFilesInputBoundary
{
    public async Task ExecuteAsync(IListOutboundFilesOutputBoundary presenter)
    {
        var files = await gateway.GetAllTransferredFilesAsync();
        presenter.Present(new ListOutboundFilesResponseModel(files));
    }
}
