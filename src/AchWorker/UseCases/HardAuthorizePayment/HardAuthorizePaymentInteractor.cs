using AchWorker.Gateways;
using Shared.Models;

namespace AchWorker.UseCases.HardAuthorizePayment;

public class HardAuthorizePaymentInteractor(IPaymentGateway paymentGateway) : IHardAuthorizePaymentInputBoundary
{
    public async Task HardAuthorizePaymentAsync(IHardAuthorizePaymentOutputBoundary presenter, HardAuthorizePaymentRequestModel request)
    {
        await paymentGateway.AddActivityAsync(request.PaymentId, PaymentActivityType.HardAuth);
        presenter.Present(new HardAuthorizePaymentResponseModel());
    }
}
