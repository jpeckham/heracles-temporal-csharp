using AchApi.Entities;
using AchApi.Gateways;
using Shared.Models;

namespace AchApi.UseCases.AddAchEntry;

public class AddAchEntryInteractor(IAchFileGateway gateway) : IAddAchEntryInputBoundary
{
    public async Task AddAchEntryAsync(IAddAchEntryOutputBoundary presenter, AddAchEntryRequestModel request)
    {
        var file = await gateway.GetAchFileByIdAsync(request.FileId);
        if (file is null)
        {
            presenter.PresentNotFound();
            return;
        }

        if (file.Status != AchFileStatus.Draft)
        {
            presenter.PresentBadRequest("File is not in Draft status.");
            return;
        }

        if (request.Amount <= 0)
        {
            presenter.PresentBadRequest("Entry amount must be positive.");
            return;
        }

        if (request.RoutingNumber.Length != 9 || !request.RoutingNumber.All(char.IsDigit))
        {
            presenter.PresentBadRequest("Routing number must be 9 digits.");
            return;
        }

        if (request.AccountNumber.Length > 17)
        {
            presenter.PresentBadRequest("Account number must be 17 chars or fewer.");
            return;
        }

        if (request.Type != "Credit" && request.Type != "Debit")
        {
            presenter.PresentBadRequest("Entry type must be Credit or Debit.");
            return;
        }

        var entry = new AchEntry
        {
            FileId = request.FileId,
            PaymentId = request.PaymentId,
            RoutingNumber = request.RoutingNumber,
            AccountNumber = request.AccountNumber,
            AccountHolderName = request.AccountHolderName,
            Amount = request.Amount,
            TransactionCode = request.Type == "Credit" ? "22" : "27",
            RepresentmentCount = request.RepresentmentCount
        };

        var saved = await gateway.AddAchEntryAsync(entry);
        presenter.Present(new AddAchEntryResponseModel(saved.EntryId));
    }
}
