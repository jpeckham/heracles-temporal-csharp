namespace Shared.Contracts;

public record AchReturnDetails(Guid PaymentId, string RCode, string? Description = null);
