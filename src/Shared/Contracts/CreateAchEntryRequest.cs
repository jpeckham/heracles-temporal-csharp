namespace Shared.Contracts;

public record CreateAchEntryRequest(
    Guid PaymentId,
    int RepresentmentCount = 0);
