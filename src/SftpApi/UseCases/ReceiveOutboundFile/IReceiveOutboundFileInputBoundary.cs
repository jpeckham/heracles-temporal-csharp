namespace SftpApi.UseCases.ReceiveOutboundFile;

public interface IReceiveOutboundFileInputBoundary
{
    Task ExecuteAsync(IReceiveOutboundFileOutputBoundary presenter, ReceiveOutboundFileRequestModel request);
}
