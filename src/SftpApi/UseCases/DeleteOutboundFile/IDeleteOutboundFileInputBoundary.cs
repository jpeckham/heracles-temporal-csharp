namespace SftpApi.UseCases.DeleteOutboundFile;

public interface IDeleteOutboundFileInputBoundary
{
    Task ExecuteAsync(IDeleteOutboundFileOutputBoundary presenter, DeleteOutboundFileRequestModel request);
}
