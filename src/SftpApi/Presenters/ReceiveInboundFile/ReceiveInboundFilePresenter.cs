using SftpApi.UseCases.ReceiveInboundFile;

namespace SftpApi.Presenters.ReceiveInboundFile;

public class ReceiveInboundFilePresenter : IReceiveInboundFileOutputBoundary
{
    public object? ViewModel { get; private set; }

    public void Present(ReceiveInboundFileResponseModel response)
        => ViewModel = new { response.FileId };
}
