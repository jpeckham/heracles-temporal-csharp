using SftpApi.Entities;
using SftpApi.UseCases.ListOutboundFiles;

namespace SftpApi.Presenters.ListOutboundFiles;

public class ListOutboundFilesPresenter : IListOutboundFilesOutputBoundary
{
    public IReadOnlyList<TransferredFile>? ViewModel { get; private set; }

    public void Present(ListOutboundFilesResponseModel response)
        => ViewModel = response.Files;
}
