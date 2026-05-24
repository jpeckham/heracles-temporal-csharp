namespace PaymentApi.UseCases.MakePayment;

public interface IMakePaymentOutputBoundary
{
    void Present(MakePaymentResponseModel response);
}
