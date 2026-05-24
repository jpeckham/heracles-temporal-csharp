using AchWorker.Gateways;
using Shared.Models;

namespace AchWorker.UseCases.VoidPaymentAuth;

public class VoidPaymentAuthInteractor(IPaymentGateway paymentGateway) : IVoidPaymentAuthInputBoundary
{
    public async Task VoidPaymentAuthAsync(IVoidPaymentAuthOutputBoundary presenter, VoidPaymentAuthRequestModel request)
    {
        var exists = await paymentGateway.ExistsAsync(request.PaymentId);
        if (exists)
            await paymentGateway.AddActivityAsync(request.PaymentId, PaymentActivityType.Void);
        presenter.Present(new VoidPaymentAuthResponseModel());
    }
}
