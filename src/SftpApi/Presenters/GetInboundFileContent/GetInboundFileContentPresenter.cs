using SftpApi.UseCases.GetInboundFileContent;

namespace SftpApi.Presenters.GetInboundFileContent;

public class GetInboundFileContentPresenter : IGetInboundFileContentOutputBoundary
{
    public object? ViewModel { get; private set; }
    public bool NotFound { get; private set; }

    public void Present(GetInboundFileContentResponseModel response)
        => ViewModel = new { response.ContentBase64 };

    public void PresentNotFound()
        => NotFound = true;
}
