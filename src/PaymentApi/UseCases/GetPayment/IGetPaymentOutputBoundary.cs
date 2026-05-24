namespace PaymentApi.UseCases.GetPayment;

public interface IGetPaymentOutputBoundary
{
    void Present(GetPaymentResponseModel response);
    void PresentNotFound();
}
