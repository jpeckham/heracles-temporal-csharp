using PaymentApi.UseCases.AddPaymentActivity;

namespace PaymentApi.Presenters.AddPaymentActivity;

public class AddPaymentActivityPresenter : IAddPaymentActivityOutputBoundary
{
    public AddPaymentActivityViewModel? ViewModel { get; private set; }
    public bool NotFound { get; private set; }

    public void Present(AddPaymentActivityResponseModel response)
    {
        var a = response.Activity;
        ViewModel = new AddPaymentActivityViewModel(
            a.ActivityId, a.PaymentId, a.Type.ToString(), a.OccurredAt, a.Amount, a.ReferenceCode, a.Notes);
    }

    public void PresentNotFound() => NotFound = true;
}
