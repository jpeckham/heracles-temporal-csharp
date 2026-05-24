using Microsoft.EntityFrameworkCore;
using SftpApi.Data;
using SftpApi.Entities;
using SftpApi.Gateways;

namespace SftpApi.OutputAdapters;

public class SftpFileGateway(SftpDbContext db) : ISftpFileGateway
{
    public async Task AddTransferredFileAsync(TransferredFile file)
    {
        db.TransferredFiles.Add(file);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<TransferredFile>> GetAllTransferredFilesAsync()
        => await db.TransferredFiles.OrderByDescending(f => f.UploadedAt).ToListAsync();

    public async Task DeleteTransferredFilesByAchFileIdAsync(Guid achFileId)
    {
        var files = db.TransferredFiles.Where(f => f.AchFileId == achFileId);
        db.TransferredFiles.RemoveRange(files);
        await db.SaveChangesAsync();
    }

    public async Task DeleteTransferredFileAsync(Guid fileId)
    {
        var file = await db.TransferredFiles.FindAsync(fileId);
        if (file is not null)
        {
            db.TransferredFiles.Remove(file);
            await db.SaveChangesAsync();
        }
    }

    public async Task AddReceivedFileAsync(ReceivedFile file)
    {
        db.ReceivedFiles.Add(file);
        await db.SaveChangesAsync();
    }

    public async Task<ReceivedFile?> FindReceivedFileAsync(Guid fileId)
        => await db.ReceivedFiles.FindAsync(fileId);

    public async Task UpdateReceivedFileAsync(ReceivedFile file)
        => await db.SaveChangesAsync();
}
