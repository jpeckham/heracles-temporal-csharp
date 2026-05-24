using SftpApi.Gateways;

namespace SftpApi.UseCases.GetInboundFileContent;

public class GetInboundFileContentInteractor(ISftpFileGateway gateway) : IGetInboundFileContentInputBoundary
{
    public async Task ExecuteAsync(IGetInboundFileContentOutputBoundary presenter, GetInboundFileContentRequestModel request)
    {
        var file = await gateway.FindReceivedFileAsync(request.FileId);
        if (file is null)
        {
            presenter.PresentNotFound();
            return;
        }
        presenter.Present(new GetInboundFileContentResponseModel(file.ContentBase64));
    }
}
