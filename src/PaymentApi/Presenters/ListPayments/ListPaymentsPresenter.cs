using PaymentApi.UseCases.ListPayments;

namespace PaymentApi.Presenters.ListPayments;

public class ListPaymentsPresenter : IListPaymentsOutputBoundary
{
    public List<ListPaymentViewModel>? ViewModel { get; private set; }

    public void Present(ListPaymentsResponseModel response)
    {
        ViewModel = response.Payments.Select(p => new ListPaymentViewModel(
            p.PaymentId, p.AccountHolderName, p.Amount, p.Type.ToString(),
            p.AllowsRepresentment, p.CurrentStatus, p.CreatedAt)).ToList();
    }
}
