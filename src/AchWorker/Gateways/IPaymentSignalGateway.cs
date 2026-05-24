using Shared.Contracts;

namespace AchWorker.Gateways;

public interface IPaymentSignalGateway
{
    Task SignalAddedToBatchAsync(Guid paymentId, Guid achFileId, bool isSameDayAch);
    Task SignalBankReturnAsync(Guid paymentId, AchReturnDetails details);
}
