using Shared.Models;

namespace AchApi.UseCases.FinalizeAchFile;

public record FinalizeAchFileResponseModel(Guid FileId, AchFileStatus Status, string ContentBase64);
