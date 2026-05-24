using AchApi.Entities;
using Shared.Models;

namespace AchApi.Gateways;

public interface IAchFileGateway
{
    Task<AchFile> CreateAchFileAsync();
    Task<AchFile?> GetAchFileByIdAsync(Guid fileId);
    Task<AchFile?> GetAchFileWithEntriesAsync(Guid fileId);
    Task<List<AchFile>> ListAchFilesAsync();
    Task SaveAchFileAsync(AchFile file);
    Task DeleteAchFileAsync(AchFile file);
    Task<AchEntry?> GetAchEntryByIdAsync(Guid entryId);
    Task<AchEntry> AddAchEntryAsync(AchEntry entry);
    Task DeleteAchEntryAsync(AchEntry entry);
}
