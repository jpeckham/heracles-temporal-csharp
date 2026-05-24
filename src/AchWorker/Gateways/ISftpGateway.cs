namespace AchWorker.Gateways;

public interface ISftpGateway
{
    Task<Guid> TransferFileAsync(Guid achFileId, string fileName, string contentBase64);
    Task DeleteTransferredAsync(Guid achFileId);
    Task<string?> GetInboundContentBase64Async(Guid receivedFileId);
    Task MarkProcessedAsync(Guid receivedFileId);
}
