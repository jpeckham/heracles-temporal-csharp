using PaymentApi.Entities;
using PaymentApi.Gateways;

namespace PaymentApi.UseCases.MakePayment;

public class MakePaymentInteractor(IPaymentGateway paymentGateway, IPaymentEventGateway eventGateway)
    : IMakePaymentInputBoundary
{
    public async Task MakePaymentAsync(IMakePaymentOutputBoundary presenter, MakePaymentRequestModel request)
    {
        var payment = new Payment
        {
            RoutingNumber = request.RoutingNumber.Trim(),
            AccountNumber = request.AccountNumber.Trim(),
            AccountHolderName = request.AccountHolderName.Trim(),
            Amount = request.Amount,
            Type = request.Type,
            AllowsRepresentment = request.AllowsRepresentment
        };

        await paymentGateway.SaveAsync(payment);
        await eventGateway.PaymentCreatedAsync(payment);

        presenter.Present(new MakePaymentResponseModel(payment.PaymentId));
    }
}
