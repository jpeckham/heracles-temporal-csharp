using System.Security.Cryptography;
using SftpApi.Entities;
using SftpApi.Gateways;

namespace SftpApi.UseCases.ReceiveOutboundFile;

public class ReceiveOutboundFileInteractor(ISftpFileGateway gateway) : IReceiveOutboundFileInputBoundary
{
    public async Task ExecuteAsync(IReceiveOutboundFileOutputBoundary presenter, ReceiveOutboundFileRequestModel request)
    {
        var bytes = Convert.FromBase64String(request.ContentBase64);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));

        var file = new TransferredFile
        {
            AchFileId = request.AchFileId,
            FileName = request.FileName,
            FileSizeBytes = bytes.Length,
            ContentHash = hash
        };

        await gateway.AddTransferredFileAsync(file);

        presenter.Present(new ReceiveOutboundFileResponseModel(file.FileId));
    }
}
