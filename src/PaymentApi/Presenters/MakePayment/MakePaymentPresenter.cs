using PaymentApi.UseCases.MakePayment;

namespace PaymentApi.Presenters.MakePayment;

public class MakePaymentPresenter : IMakePaymentOutputBoundary
{
    public MakePaymentViewModel? ViewModel { get; private set; }

    public void Present(MakePaymentResponseModel response)
    {
        ViewModel = new MakePaymentViewModel(response.PaymentId);
    }
}
