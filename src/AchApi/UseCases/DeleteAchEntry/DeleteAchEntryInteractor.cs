using AchApi.Gateways;

namespace AchApi.UseCases.DeleteAchEntry;

public class DeleteAchEntryInteractor(IAchFileGateway gateway) : IDeleteAchEntryInputBoundary
{
    public async Task DeleteAchEntryAsync(IDeleteAchEntryOutputBoundary presenter, DeleteAchEntryRequestModel request)
    {
        var entry = await gateway.GetAchEntryByIdAsync(request.EntryId);
        if (entry is not null)
            await gateway.DeleteAchEntryAsync(entry);

        presenter.Present(new DeleteAchEntryResponseModel());
    }
}
