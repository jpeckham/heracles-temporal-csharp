namespace AchApi.UseCases.AddAchEntry;

public record AddAchEntryRequestModel(
    Guid FileId,
    Guid PaymentId,
    string RoutingNumber,
    string AccountNumber,
    string AccountHolderName,
    decimal Amount,
    string Type,
    int RepresentmentCount = 0);
