namespace PaymentApi.UseCases.ListPayments;

public interface IListPaymentsOutputBoundary
{
    void Present(ListPaymentsResponseModel response);
}
