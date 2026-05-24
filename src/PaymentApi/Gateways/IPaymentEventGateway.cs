using PaymentApi.Entities;

namespace PaymentApi.Gateways;

public interface IPaymentEventGateway
{
    Task PaymentCreatedAsync(Payment payment);
}
