using PaymentApi.Gateways;

namespace PaymentApi.UseCases.ListPayments;

public class ListPaymentsInteractor(IPaymentGateway paymentGateway) : IListPaymentsInputBoundary
{
    public async Task ListPaymentsAsync(IListPaymentsOutputBoundary presenter, ListPaymentsRequestModel request)
    {
        var payments = await paymentGateway.FindAllAsync();
        var statusFilter = request.StatusFilter?.Trim();
        var filtered = !string.IsNullOrEmpty(statusFilter)
            ? payments.Where(p => string.Equals(p.CurrentStatus, statusFilter, StringComparison.OrdinalIgnoreCase)).ToList()
            : payments;

        presenter.Present(new ListPaymentsResponseModel(filtered));
    }
}
