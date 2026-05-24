namespace PaymentApi.Presenters.AddPaymentActivity;

public record AddPaymentActivityViewModel(
    Guid ActivityId,
    Guid PaymentId,
    string Type,
    DateTime OccurredAt,
    decimal? Amount,
    string? ReferenceCode,
    string? Notes);
