using SftpApi.Gateways;

namespace SftpApi.UseCases.DeleteOutboundFileByAch;

public class DeleteOutboundFileByAchInteractor(ISftpFileGateway gateway) : IDeleteOutboundFileByAchInputBoundary
{
    public async Task ExecuteAsync(IDeleteOutboundFileByAchOutputBoundary presenter, DeleteOutboundFileByAchRequestModel request)
    {
        await gateway.DeleteTransferredFilesByAchFileIdAsync(request.AchFileId);
        presenter.Present(new DeleteOutboundFileByAchResponseModel());
    }
}
