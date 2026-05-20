using Shared.Models;

namespace AchApi.Data;

public class AchFile
{
    public Guid FileId { get; set; } = Guid.NewGuid();
    public string BatchNumber { get; set; } = DateTime.UtcNow.ToString("yyyyMMdd");
    public AchFileStatus Status { get; set; } = AchFileStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinalizedAt { get; set; }
    public string? NachaContent { get; set; }
    public List<AchEntry> Entries { get; set; } = [];
}
