namespace AchWorker.UseCases.AddAchEntry;

public record AddAchEntryRequestModel(Guid FileId, Guid PaymentId, int RepresentmentCount = 0);
