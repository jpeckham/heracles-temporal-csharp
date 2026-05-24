namespace SftpApi.UseCases.DeleteOutboundFile;

public interface IDeleteOutboundFileOutputBoundary
{
    void Present(DeleteOutboundFileResponseModel response);
}
