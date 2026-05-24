using Shared.Models;
using SftpApi.Gateways;

namespace SftpApi.UseCases.UpdateInboundFileStatus;

public class UpdateInboundFileStatusInteractor(ISftpFileGateway gateway) : IUpdateInboundFileStatusInputBoundary
{
    public async Task ExecuteAsync(IUpdateInboundFileStatusOutputBoundary presenter, UpdateInboundFileStatusRequestModel request)
    {
        var file = await gateway.FindReceivedFileAsync(request.FileId);
        if (file is null)
        {
            presenter.PresentNotFound();
            return;
        }

        if (!Enum.TryParse<ReceivedFileStatus>(request.Status, out var status))
        {
            presenter.PresentInvalidStatus(request.Status);
            return;
        }

        file.Status = status;
        await gateway.UpdateReceivedFileAsync(file);
        presenter.Present(new UpdateInboundFileStatusResponseModel());
    }
}
