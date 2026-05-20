using Shared.Models;

namespace Shared.Contracts;

public record CreatePaymentRequest(
    string RoutingNumber,
    string AccountNumber,
    string AccountHolderName,
    decimal Amount,
    PaymentType Type,
    bool AllowsRepresentment);
