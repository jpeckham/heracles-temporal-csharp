using SftpApi.Entities;

namespace SftpApi.UseCases.ListOutboundFiles;

public record ListOutboundFilesResponseModel(IReadOnlyList<TransferredFile> Files);
