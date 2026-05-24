using PaymentApi.Gateways;

namespace PaymentApi.UseCases.ListPayments;

public class ListPaymentsInteractor(IPaymentGateway paymentGateway) : IListPaymentsInputBoundary
{
    public async Task ListPaymentsAsync(IListPaymentsOutputBoundary presenter, ListPaymentsRequestModel request)
    {
        var payments = await paymentGateway.FindAllAsync();
        var filtered = request.StatusFilter is not null
            ? payments.Where(p => p.CurrentStatus == request.StatusFilter).ToList()
            : payments;

        presenter.Present(new ListPaymentsResponseModel(filtered));
    }
}
