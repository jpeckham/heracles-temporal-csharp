namespace AchWorker.UseCases.RecordRepresentment;

public record RecordRepresentmentRequestModel(Guid PaymentId, int RepresentmentCount);
