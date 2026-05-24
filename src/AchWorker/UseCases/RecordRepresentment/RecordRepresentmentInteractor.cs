using AchWorker.Gateways;
using Shared.Models;

namespace AchWorker.UseCases.RecordRepresentment;

public class RecordRepresentmentInteractor(IPaymentGateway paymentGateway) : IRecordRepresentmentInputBoundary
{
    public async Task RecordRepresentmentAsync(IRecordRepresentmentOutputBoundary presenter, RecordRepresentmentRequestModel request)
    {
        await paymentGateway.AddActivityAsync(request.PaymentId, PaymentActivityType.Representment,
            notes: $"Attempt {request.RepresentmentCount}");
        await paymentGateway.AddActivityAsync(request.PaymentId, PaymentActivityType.SoftAuth,
            notes: "Re-queued for representment");
        presenter.Present(new RecordRepresentmentResponseModel());
    }
}
