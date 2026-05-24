namespace SftpApi.UseCases.UpdateInboundFileStatus;

public interface IUpdateInboundFileStatusOutputBoundary
{
    void Present(UpdateInboundFileStatusResponseModel response);
    void PresentNotFound();
    void PresentInvalidStatus(string status);
}
