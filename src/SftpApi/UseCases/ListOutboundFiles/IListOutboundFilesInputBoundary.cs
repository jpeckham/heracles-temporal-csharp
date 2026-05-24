namespace SftpApi.UseCases.ListOutboundFiles;

public interface IListOutboundFilesInputBoundary
{
    Task ExecuteAsync(IListOutboundFilesOutputBoundary presenter);
}
