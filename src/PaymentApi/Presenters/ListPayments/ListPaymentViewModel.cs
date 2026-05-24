namespace PaymentApi.Presenters.ListPayments;

public record ListPaymentViewModel(
    Guid PaymentId,
    string AccountHolderName,
    decimal Amount,
    string Type,
    bool AllowsRepresentment,
    string CurrentStatus,
    DateTime CreatedAt);
