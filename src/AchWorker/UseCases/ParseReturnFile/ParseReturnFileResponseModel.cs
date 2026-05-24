using AchWorker.Entities;

namespace AchWorker.UseCases.ParseReturnFile;

public record ParseReturnFileResponseModel(List<AchReturnRecord> Records);
