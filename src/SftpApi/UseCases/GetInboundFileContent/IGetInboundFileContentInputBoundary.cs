namespace SftpApi.UseCases.GetInboundFileContent;

public interface IGetInboundFileContentInputBoundary
{
    Task ExecuteAsync(IGetInboundFileContentOutputBoundary presenter, GetInboundFileContentRequestModel request);
}
