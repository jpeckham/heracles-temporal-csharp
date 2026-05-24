using Shared.Models;

namespace PaymentApi.UseCases.AddPaymentActivity;

public record AddPaymentActivityRequestModel(
    Guid PaymentId,
    PaymentActivityType Type,
    decimal? Amount = null,
    string? ReferenceCode = null,
    string? Notes = null);
