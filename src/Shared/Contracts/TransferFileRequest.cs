namespace Shared.Contracts;

public record TransferFileRequest(
    Guid AchFileId,
    string FileName,
    string ContentBase64);
