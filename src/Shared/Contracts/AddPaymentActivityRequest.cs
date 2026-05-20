namespace Shared.Contracts;

public record AddPaymentActivityRequest(
    string Type,           // PaymentActivityType name
    decimal? Amount = null,
    string? ReferenceCode = null,
    string? Notes = null);
