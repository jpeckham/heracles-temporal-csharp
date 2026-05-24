using AchWorker.UseCases.HardAuthorizePayment;

namespace AchWorker.Presenters.HardAuthorizePayment;

public class HardAuthorizePaymentPresenter : IHardAuthorizePaymentOutputBoundary
{
    public HardAuthorizePaymentResponseModel? ViewModel { get; private set; }

    public void Present(HardAuthorizePaymentResponseModel response)
    {
        ViewModel = response;
    }
}
