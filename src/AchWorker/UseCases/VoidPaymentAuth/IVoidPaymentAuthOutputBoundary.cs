namespace AchWorker.UseCases.VoidPaymentAuth;

public interface IVoidPaymentAuthOutputBoundary
{
    void Present(VoidPaymentAuthResponseModel response);
}
