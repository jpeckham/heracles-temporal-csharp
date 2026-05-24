namespace PaymentApi.UseCases.ListPayments;

public interface IListPaymentsInputBoundary
{
    Task ListPaymentsAsync(IListPaymentsOutputBoundary presenter, ListPaymentsRequestModel request);
}
