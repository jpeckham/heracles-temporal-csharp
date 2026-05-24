namespace SftpApi.UseCases.UpdateInboundFileStatus;

public interface IUpdateInboundFileStatusInputBoundary
{
    Task ExecuteAsync(IUpdateInboundFileStatusOutputBoundary presenter, UpdateInboundFileStatusRequestModel request);
}
