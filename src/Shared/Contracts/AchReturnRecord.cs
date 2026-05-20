namespace Shared.Contracts;

public record AchReturnRecord(Guid PaymentId, string RCode, string? Description);
