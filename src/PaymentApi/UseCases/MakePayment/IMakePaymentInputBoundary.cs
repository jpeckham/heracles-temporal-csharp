namespace PaymentApi.UseCases.MakePayment;

public interface IMakePaymentInputBoundary
{
    Task MakePaymentAsync(IMakePaymentOutputBoundary presenter, MakePaymentRequestModel request);
}
