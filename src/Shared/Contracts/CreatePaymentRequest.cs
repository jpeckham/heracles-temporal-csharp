namespace Shared.Contracts;

public record CreatePaymentRequest(
    string RoutingNumber,
    string AccountNumber,
    string AccountHolderName,
    decimal Amount,
    string Type,           // "Credit" or "Debit"
    bool AllowsRepresentment);
