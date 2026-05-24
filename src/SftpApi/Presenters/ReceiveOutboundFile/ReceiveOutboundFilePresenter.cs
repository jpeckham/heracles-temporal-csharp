using SftpApi.UseCases.ReceiveOutboundFile;

namespace SftpApi.Presenters.ReceiveOutboundFile;

public class ReceiveOutboundFilePresenter : IReceiveOutboundFileOutputBoundary
{
    public object? ViewModel { get; private set; }

    public void Present(ReceiveOutboundFileResponseModel response)
        => ViewModel = new { response.FileId };
}
