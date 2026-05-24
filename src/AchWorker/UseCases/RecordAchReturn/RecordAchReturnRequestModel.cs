using Shared.Contracts;

namespace AchWorker.UseCases.RecordAchReturn;

public record RecordAchReturnRequestModel(Guid PaymentId, AchReturnDetails Details);
