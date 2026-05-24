using PaymentApi.Gateways;

namespace PaymentApi.UseCases.GetPayment;

public class GetPaymentInteractor(IPaymentGateway paymentGateway) : IGetPaymentInputBoundary
{
    public async Task GetPaymentAsync(IGetPaymentOutputBoundary presenter, GetPaymentRequestModel request)
    {
        var payment = await paymentGateway.FindByIdAsync(request.PaymentId);
        if (payment is null)
            presenter.PresentNotFound();
        else
            presenter.Present(new GetPaymentResponseModel(payment));
    }
}
