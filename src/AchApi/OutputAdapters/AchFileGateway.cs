using AchApi.Data;
using AchApi.Entities;
using AchApi.Gateways;
using Microsoft.EntityFrameworkCore;

namespace AchApi.OutputAdapters;

public class AchFileGateway(AchDbContext db) : IAchFileGateway
{
    public async Task<AchFile> CreateAchFileAsync()
    {
        var file = new AchFile();
        db.AchFiles.Add(file);
        await db.SaveChangesAsync();
        return file;
    }

    public async Task<AchFile?> GetAchFileByIdAsync(Guid fileId)
        => await db.AchFiles.FindAsync(fileId);

    public async Task<AchFile?> GetAchFileWithEntriesAsync(Guid fileId)
        => await db.AchFiles.Include(f => f.Entries).FirstOrDefaultAsync(f => f.FileId == fileId);

    public async Task<List<AchFile>> ListAchFilesAsync()
        => await db.AchFiles.OrderByDescending(f => f.CreatedAt).ToListAsync();

    public async Task SaveAchFileAsync(AchFile file)
        => await db.SaveChangesAsync();

    public async Task DeleteAchFileAsync(AchFile file)
    {
        db.AchFiles.Remove(file);
        await db.SaveChangesAsync();
    }

    public async Task<AchEntry?> GetAchEntryByIdAsync(Guid entryId)
        => await db.AchEntries.FindAsync(entryId);

    public async Task<AchEntry> AddAchEntryAsync(AchEntry entry)
    {
        db.AchEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    public async Task DeleteAchEntryAsync(AchEntry entry)
    {
        db.AchEntries.Remove(entry);
        await db.SaveChangesAsync();
    }
}
