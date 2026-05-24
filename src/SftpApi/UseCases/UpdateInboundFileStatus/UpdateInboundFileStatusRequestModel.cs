namespace SftpApi.UseCases.UpdateInboundFileStatus;

public record UpdateInboundFileStatusRequestModel(Guid FileId, string Status);
