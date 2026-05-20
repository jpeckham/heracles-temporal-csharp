namespace SftpApi.Data;

public class TransferredFile
{
    public Guid FileId { get; set; } = Guid.NewGuid();
    public Guid AchFileId { get; set; }
    public string FileName { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public string ContentHash { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Received";
}
