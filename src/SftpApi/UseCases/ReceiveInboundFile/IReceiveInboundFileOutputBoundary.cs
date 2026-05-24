namespace SftpApi.UseCases.ReceiveInboundFile;

public interface IReceiveInboundFileOutputBoundary
{
    void Present(ReceiveInboundFileResponseModel response);
}
