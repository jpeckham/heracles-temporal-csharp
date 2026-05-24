namespace PaymentApi.UseCases.GetPayment;

public interface IGetPaymentInputBoundary
{
    Task GetPaymentAsync(IGetPaymentOutputBoundary presenter, GetPaymentRequestModel request);
}
