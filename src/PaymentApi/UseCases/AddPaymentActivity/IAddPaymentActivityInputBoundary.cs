namespace PaymentApi.UseCases.AddPaymentActivity;

public interface IAddPaymentActivityInputBoundary
{
    Task AddPaymentActivityAsync(IAddPaymentActivityOutputBoundary presenter, AddPaymentActivityRequestModel request);
}
