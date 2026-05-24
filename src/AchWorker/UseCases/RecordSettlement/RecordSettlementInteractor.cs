using AchWorker.Gateways;
using Shared.Models;

namespace AchWorker.UseCases.RecordSettlement;

public class RecordSettlementInteractor(IPaymentGateway paymentGateway) : IRecordSettlementInputBoundary
{
    public async Task RecordSettlementAsync(IRecordSettlementOutputBoundary presenter, RecordSettlementRequestModel request)
    {
        await paymentGateway.AddActivityAsync(request.PaymentId, PaymentActivityType.Settlement);
        await paymentGateway.AddActivityAsync(request.PaymentId, PaymentActivityType.PaidOut);
        presenter.Present(new RecordSettlementResponseModel());
    }
}
