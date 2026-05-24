namespace SftpApi.UseCases.GetInboundFileContent;

public interface IGetInboundFileContentOutputBoundary
{
    void Present(GetInboundFileContentResponseModel response);
    void PresentNotFound();
}
