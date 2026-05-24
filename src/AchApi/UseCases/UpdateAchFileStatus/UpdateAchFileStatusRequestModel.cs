namespace AchApi.UseCases.UpdateAchFileStatus;

public record UpdateAchFileStatusRequestModel(Guid FileId, string Status);
