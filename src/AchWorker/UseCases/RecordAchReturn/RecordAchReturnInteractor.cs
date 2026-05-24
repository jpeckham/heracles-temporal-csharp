using AchWorker.Gateways;
using Shared.Models;

namespace AchWorker.UseCases.RecordAchReturn;

public class RecordAchReturnInteractor(IPaymentGateway paymentGateway) : IRecordAchReturnInputBoundary
{
    public async Task RecordAchReturnAsync(IRecordAchReturnOutputBoundary presenter, RecordAchReturnRequestModel request)
    {
        await paymentGateway.AddActivityAsync(request.PaymentId, PaymentActivityType.AchReturn,
            referenceCode: request.Details.RCode, notes: request.Details.Description);
        presenter.Present(new RecordAchReturnResponseModel());
    }
}
