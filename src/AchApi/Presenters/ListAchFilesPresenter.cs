using AchApi.UseCases.ListAchFiles;

namespace AchApi.Presenters;

public class ListAchFilesPresenter : IListAchFilesOutputBoundary
{
    public ListAchFilesViewModel? ViewModel { get; private set; }

    public void Present(ListAchFilesResponseModel response)
    {
        ViewModel = new ListAchFilesViewModel(
            response.Files.Select(f => new AchFileSummaryViewModel(f.FileId, f.BatchNumber, f.Status, f.CreatedAt, f.FinalizedAt)).ToList());
    }
}
