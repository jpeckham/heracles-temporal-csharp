using SftpApi.Entities;

namespace SftpApi.Gateways;

public interface ISftpFileGateway
{
    Task AddTransferredFileAsync(TransferredFile file);
    Task<IReadOnlyList<TransferredFile>> GetAllTransferredFilesAsync();
    Task DeleteTransferredFilesByAchFileIdAsync(Guid achFileId);
    Task DeleteTransferredFileAsync(Guid fileId);

    Task AddReceivedFileAsync(ReceivedFile file);
    Task<ReceivedFile?> FindReceivedFileAsync(Guid fileId);
    Task UpdateReceivedFileAsync(ReceivedFile file);
}
