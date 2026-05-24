using AchWorker.Gateways;

namespace AchWorker.UseCases.SignalBankReturn;

public class SignalBankReturnInteractor(IPaymentSignalGateway signalGateway) : ISignalBankReturnInputBoundary
{
    public async Task SignalBankReturnAsync(ISignalBankReturnOutputBoundary presenter, SignalBankReturnRequestModel request)
    {
        await signalGateway.SignalBankReturnAsync(request.PaymentId, request.Details);
        presenter.Present(new SignalBankReturnResponseModel());
    }
}
