namespace SftpApi.UseCases.ReceiveInboundFile;

public interface IReceiveInboundFileInputBoundary
{
    Task ExecuteAsync(IReceiveInboundFileOutputBoundary presenter, ReceiveInboundFileRequestModel request);
}
