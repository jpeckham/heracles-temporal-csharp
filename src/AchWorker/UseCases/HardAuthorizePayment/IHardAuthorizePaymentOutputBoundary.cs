namespace AchWorker.UseCases.HardAuthorizePayment;

public interface IHardAuthorizePaymentOutputBoundary
{
    void Present(HardAuthorizePaymentResponseModel response);
}
