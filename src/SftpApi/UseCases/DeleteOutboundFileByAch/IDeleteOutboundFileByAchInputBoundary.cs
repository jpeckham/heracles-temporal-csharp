namespace SftpApi.UseCases.DeleteOutboundFileByAch;

public interface IDeleteOutboundFileByAchInputBoundary
{
    Task ExecuteAsync(IDeleteOutboundFileByAchOutputBoundary presenter, DeleteOutboundFileByAchRequestModel request);
}
