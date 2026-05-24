using Shared.Models;

namespace PaymentApi.UseCases.MakePayment;

public record MakePaymentRequestModel(
    string RoutingNumber,
    string AccountNumber,
    string AccountHolderName,
    decimal Amount,
    PaymentType Type,
    bool AllowsRepresentment);
