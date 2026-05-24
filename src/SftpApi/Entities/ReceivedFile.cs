using Shared.Models;

namespace SftpApi.Entities;

public class ReceivedFile
{
    public Guid FileId { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = "";
    public string ContentBase64 { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public ReceivedFileStatus Status { get; set; } = ReceivedFileStatus.Pending;
}
