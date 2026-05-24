namespace AchWorker.UseCases.CollectPendingPayments;

public record CollectPendingPaymentsResponseModel(List<Guid> PaymentIds);
