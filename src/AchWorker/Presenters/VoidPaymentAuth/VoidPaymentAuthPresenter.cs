using AchWorker.UseCases.VoidPaymentAuth;

namespace AchWorker.Presenters.VoidPaymentAuth;

public class VoidPaymentAuthPresenter : IVoidPaymentAuthOutputBoundary
{
    public VoidPaymentAuthResponseModel? ViewModel { get; private set; }

    public void Present(VoidPaymentAuthResponseModel response)
    {
        ViewModel = response;
    }
}
