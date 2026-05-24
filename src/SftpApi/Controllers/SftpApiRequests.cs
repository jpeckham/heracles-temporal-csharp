namespace SftpApi.Controllers;

public record InboundFileRequest(string FileName, string ContentBase64);
public record UpdateInboundStatusRequest(string Status);
