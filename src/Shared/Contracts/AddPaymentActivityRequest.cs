using Shared.Models;

namespace Shared.Contracts;

public record AddPaymentActivityRequest(
    PaymentActivityType Type,
    decimal? Amount = null,
    string? ReferenceCode = null,
    string? Notes = null);
