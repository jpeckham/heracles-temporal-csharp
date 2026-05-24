using Shared.Contracts;

namespace AchWorker.UseCases.SignalBankReturn;

public record SignalBankReturnRequestModel(Guid PaymentId, AchReturnDetails Details);
