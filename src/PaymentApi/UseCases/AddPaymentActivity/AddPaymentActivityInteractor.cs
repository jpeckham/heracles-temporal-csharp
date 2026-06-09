using PaymentApi.Entities;
using PaymentApi.Gateways;

namespace PaymentApi.UseCases.AddPaymentActivity;

public class AddPaymentActivityInteractor(IPaymentGateway paymentGateway) : IAddPaymentActivityInputBoundary
{
    public async Task AddPaymentActivityAsync(IAddPaymentActivityOutputBoundary presenter, AddPaymentActivityRequestModel request)
    {
        var payment = await paymentGateway.FindByIdAsync(request.PaymentId);
        if (payment is null)
        {
            presenter.PresentNotFound();
            return;
        }

        var activity = new PaymentActivity
        {
            PaymentId = request.PaymentId,
            Type = request.Type,
            Amount = request.Amount,
            ReferenceCode = request.ReferenceCode?.Trim(),
            Notes = request.Notes?.Trim()
        };

        await paymentGateway.SaveActivityAsync(activity);
        presenter.Present(new AddPaymentActivityResponseModel(activity));
    }
}
