namespace AchWorker.UseCases.VoidPaymentAuth;

public interface IVoidPaymentAuthInputBoundary
{
    Task VoidPaymentAuthAsync(IVoidPaymentAuthOutputBoundary presenter, VoidPaymentAuthRequestModel request);
}
