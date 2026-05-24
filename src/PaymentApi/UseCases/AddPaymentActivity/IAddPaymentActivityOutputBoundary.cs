namespace PaymentApi.UseCases.AddPaymentActivity;

public interface IAddPaymentActivityOutputBoundary
{
    void Present(AddPaymentActivityResponseModel response);
    void PresentNotFound();
}
