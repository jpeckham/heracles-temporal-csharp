using Shared.Models;

namespace AchApi.UseCases.ListAchFiles;

public record AchFileSummary(Guid FileId, string BatchNumber, AchFileStatus Status, DateTime CreatedAt, DateTime? FinalizedAt);
public record ListAchFilesResponseModel(List<AchFileSummary> Files);
