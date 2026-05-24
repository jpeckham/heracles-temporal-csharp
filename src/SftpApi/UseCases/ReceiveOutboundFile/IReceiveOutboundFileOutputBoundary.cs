namespace SftpApi.UseCases.ReceiveOutboundFile;

public interface IReceiveOutboundFileOutputBoundary
{
    void Present(ReceiveOutboundFileResponseModel response);
}
