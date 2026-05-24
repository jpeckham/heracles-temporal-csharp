using PaymentApi.UseCases.GetPayment;

namespace PaymentApi.Presenters.GetPayment;

public class GetPaymentPresenter : IGetPaymentOutputBoundary
{
    public GetPaymentViewModel? ViewModel { get; private set; }
    public bool NotFound { get; private set; }

    public void Present(GetPaymentResponseModel response)
    {
        var p = response.Payment;
        ViewModel = new GetPaymentViewModel(
            p.PaymentId, p.RoutingNumber, p.AccountNumber, p.AccountHolderName, p.Amount, p.Type.ToString(),
            p.AllowsRepresentment, p.CurrentStatus, p.CreatedAt,
            p.Activities.Select(a => new PaymentActivityViewModel(
                a.ActivityId, a.Type.ToString(), a.OccurredAt, a.Amount, a.ReferenceCode, a.Notes)).ToList());
    }

    public void PresentNotFound() => NotFound = true;
}
