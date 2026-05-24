using Shared.Models;

namespace AchWorker.Gateways;

public record PaymentDetail(Guid PaymentId, string RoutingNumber, string AccountNumber,
    string AccountHolderName, decimal Amount, string Type);

public interface IPaymentGateway
{
    Task<List<Guid>> CollectPendingAsync();
    Task<PaymentDetail> GetDetailAsync(Guid paymentId);
    Task AddActivityAsync(Guid paymentId, PaymentActivityType type, string? referenceCode = null, string? notes = null);
    Task<bool> ExistsAsync(Guid paymentId);
}
