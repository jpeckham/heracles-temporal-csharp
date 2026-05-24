namespace SftpApi.UseCases.ReceiveOutboundFile;

public record ReceiveOutboundFileRequestModel(Guid AchFileId, string FileName, string ContentBase64);
