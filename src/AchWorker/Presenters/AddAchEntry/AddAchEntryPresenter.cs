using AchWorker.UseCases.AddAchEntry;

namespace AchWorker.Presenters.AddAchEntry;

public class AddAchEntryPresenter : IAddAchEntryOutputBoundary
{
    public AddAchEntryResponseModel? ViewModel { get; private set; }

    public void Present(AddAchEntryResponseModel response)
    {
        ViewModel = response;
    }
}
