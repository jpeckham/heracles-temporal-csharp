namespace AchWorker.UseCases.HardAuthorizePayment;

public interface IHardAuthorizePaymentInputBoundary
{
    Task HardAuthorizePaymentAsync(IHardAuthorizePaymentOutputBoundary presenter, HardAuthorizePaymentRequestModel request);
}
