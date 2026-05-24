using PaymentApi.Entities;

namespace PaymentApi.Gateways;

public interface IPaymentGateway
{
    Task SaveAsync(Payment payment);
    Task<Payment?> FindByIdAsync(Guid paymentId);
    Task<List<Payment>> FindAllAsync();
    Task SaveActivityAsync(PaymentActivity activity);
}
