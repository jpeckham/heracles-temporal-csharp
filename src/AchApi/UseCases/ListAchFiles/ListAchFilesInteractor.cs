using AchApi.Gateways;

namespace AchApi.UseCases.ListAchFiles;

public class ListAchFilesInteractor(IAchFileGateway gateway) : IListAchFilesInputBoundary
{
    public async Task ListAchFilesAsync(IListAchFilesOutputBoundary presenter, ListAchFilesRequestModel request)
    {
        var files = await gateway.ListAchFilesAsync();
        var summaries = files.Select(f => new AchFileSummary(f.FileId, f.BatchNumber, f.Status, f.CreatedAt, f.FinalizedAt)).ToList();
        presenter.Present(new ListAchFilesResponseModel(summaries));
    }
}
