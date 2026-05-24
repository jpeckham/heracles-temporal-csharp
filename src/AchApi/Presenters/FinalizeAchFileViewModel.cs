using Shared.Models;

namespace AchApi.Presenters;

public record FinalizeAchFileViewModel(Guid FileId, AchFileStatus Status, string ContentBase64);
