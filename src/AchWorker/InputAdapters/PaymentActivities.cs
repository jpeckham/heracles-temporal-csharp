using AchWorker.Presenters.CollectPendingPayments;
using AchWorker.Presenters.HardAuthorizePayment;
using AchWorker.Presenters.RecordAchReturn;
using AchWorker.Presenters.RecordRepresentment;
using AchWorker.Presenters.RecordSettlement;
using AchWorker.Presenters.SignalBankReturn;
using AchWorker.Presenters.SignalPaymentAddedToBatch;
using AchWorker.Presenters.VoidPaymentAuth;
using AchWorker.UseCases.CollectPendingPayments;
using AchWorker.UseCases.HardAuthorizePayment;
using AchWorker.UseCases.RecordAchReturn;
using AchWorker.UseCases.RecordRepresentment;
using AchWorker.UseCases.RecordSettlement;
using AchWorker.UseCases.SignalBankReturn;
using AchWorker.UseCases.SignalPaymentAddedToBatch;
using AchWorker.UseCases.VoidPaymentAuth;
using Shared.Contracts;
using Temporalio.Activities;

namespace AchWorker.Activities;

public class PaymentActivities(
    ICollectPendingPaymentsInputBoundary collectPendingPayments,
    IHardAuthorizePaymentInputBoundary hardAuthorizePayment,
    IVoidPaymentAuthInputBoundary voidPaymentAuth,
    IRecordSettlementInputBoundary recordSettlement,
    IRecordAchReturnInputBoundary recordAchReturn,
    IRecordRepresentmentInputBoundary recordRepresentment,
    ISignalPaymentAddedToBatchInputBoundary signalPaymentAddedToBatch,
    ISignalBankReturnInputBoundary signalBankReturn)
{
    [Activity]
    public async Task<List<Guid>> CollectPendingPaymentsAsync()
    {
        var presenter = new CollectPendingPaymentsPresenter();
        await collectPendingPayments.CollectPendingPaymentsAsync(presenter, new CollectPendingPaymentsRequestModel());
        return presenter.ViewModel!.PaymentIds;
    }

    [Activity]
    public async Task HardAuthAsync(Guid paymentId)
    {
        var presenter = new HardAuthorizePaymentPresenter();
        await hardAuthorizePayment.HardAuthorizePaymentAsync(presenter, new HardAuthorizePaymentRequestModel(paymentId));
    }

    [Activity]
    public async Task VoidPaymentAuthIfExistsAsync(Guid paymentId)
    {
        var presenter = new VoidPaymentAuthPresenter();
        await voidPaymentAuth.VoidPaymentAuthAsync(presenter, new VoidPaymentAuthRequestModel(paymentId));
    }

    [Activity]
    public async Task RecordSettlementAsync(Guid paymentId)
    {
        var presenter = new RecordSettlementPresenter();
        await recordSettlement.RecordSettlementAsync(presenter, new RecordSettlementRequestModel(paymentId));
    }

    [Activity]
    public async Task RecordAchReturnAsync(Guid paymentId, AchReturnDetails details)
    {
        var presenter = new RecordAchReturnPresenter();
        await recordAchReturn.RecordAchReturnAsync(presenter, new RecordAchReturnRequestModel(paymentId, details));
    }

    [Activity]
    public async Task RecordRepresentmentAsync(Guid paymentId, int representmentCount)
    {
        var presenter = new RecordRepresentmentPresenter();
        await recordRepresentment.RecordRepresentmentAsync(presenter, new RecordRepresentmentRequestModel(paymentId, representmentCount));
    }

    [Activity]
    public async Task SignalPaymentAddedToBatchAsync(Guid paymentId, Guid achFileId, bool isSameDayAch)
    {
        var presenter = new SignalPaymentAddedToBatchPresenter();
        await signalPaymentAddedToBatch.SignalPaymentAddedToBatchAsync(presenter, new SignalPaymentAddedToBatchRequestModel(paymentId, achFileId, isSameDayAch));
    }

    [Activity]
    public async Task SignalBankReturnAsync(Guid paymentId, AchReturnDetails details)
    {
        var presenter = new SignalBankReturnPresenter();
        await signalBankReturn.SignalBankReturnAsync(presenter, new SignalBankReturnRequestModel(paymentId, details));
    }
}
