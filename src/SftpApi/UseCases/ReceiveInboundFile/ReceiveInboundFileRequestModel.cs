namespace SftpApi.UseCases.ReceiveInboundFile;

public record ReceiveInboundFileRequestModel(string FileName, string ContentBase64);
