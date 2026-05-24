using AchWorker.Gateways;

namespace AchWorker.UseCases.AddAchEntry;

public class AddAchEntryInteractor(IAchFileGateway achFileGateway, IPaymentGateway paymentGateway) : IAddAchEntryInputBoundary
{
    public async Task AddAchEntryAsync(IAddAchEntryOutputBoundary presenter, AddAchEntryRequestModel request)
    {
        var payment = await paymentGateway.GetDetailAsync(request.PaymentId);
        var entryId = await achFileGateway.AddEntryAsync(
            request.FileId,
            payment.PaymentId,
            payment.RoutingNumber,
            payment.AccountNumber,
            payment.AccountHolderName,
            payment.Amount,
            payment.Type,
            request.RepresentmentCount);
        presenter.Present(new AddAchEntryResponseModel(entryId));
    }
}
