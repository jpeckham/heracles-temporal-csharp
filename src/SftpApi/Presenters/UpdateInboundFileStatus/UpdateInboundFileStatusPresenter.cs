using SftpApi.UseCases.UpdateInboundFileStatus;

namespace SftpApi.Presenters.UpdateInboundFileStatus;

public class UpdateInboundFileStatusPresenter : IUpdateInboundFileStatusOutputBoundary
{
    public bool NotFound { get; private set; }
    public string? InvalidStatus { get; private set; }
    public bool Success { get; private set; }

    public void Present(UpdateInboundFileStatusResponseModel response) => Success = true;
    public void PresentNotFound() => NotFound = true;
    public void PresentInvalidStatus(string status) => InvalidStatus = status;
}
