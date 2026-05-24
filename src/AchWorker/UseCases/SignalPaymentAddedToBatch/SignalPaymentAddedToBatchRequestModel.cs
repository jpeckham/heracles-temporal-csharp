namespace AchWorker.UseCases.SignalPaymentAddedToBatch;

public record SignalPaymentAddedToBatchRequestModel(Guid PaymentId, Guid AchFileId, bool IsSameDayAch);
