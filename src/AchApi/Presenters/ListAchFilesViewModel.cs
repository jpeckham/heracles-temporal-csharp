using Shared.Models;

namespace AchApi.Presenters;

public record AchFileSummaryViewModel(Guid FileId, string BatchNumber, AchFileStatus Status, DateTime CreatedAt, DateTime? FinalizedAt);
public record ListAchFilesViewModel(List<AchFileSummaryViewModel> Files);
