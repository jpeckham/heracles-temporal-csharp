namespace PaymentApi.Presenters.GetPayment;

public record GetPaymentViewModel(
    Guid PaymentId,
    string RoutingNumber,
    string AccountNumber,
    string AccountHolderName,
    decimal Amount,
    string Type,
    bool AllowsRepresentment,
    string CurrentStatus,
    DateTime CreatedAt,
    List<PaymentActivityViewModel> Activities);

public record PaymentActivityViewModel(
    Guid ActivityId,
    string Type,
    DateTime OccurredAt,
    decimal? Amount,
    string? ReferenceCode,
    string? Notes);
