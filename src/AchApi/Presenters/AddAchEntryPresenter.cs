using AchApi.UseCases.AddAchEntry;

namespace AchApi.Presenters;

public class AddAchEntryPresenter : IAddAchEntryOutputBoundary
{
    public AddAchEntryViewModel? ViewModel { get; private set; }
    public bool NotFound { get; private set; }
    public string? BadRequestMessage { get; private set; }

    public void Present(AddAchEntryResponseModel response)
    {
        ViewModel = new AddAchEntryViewModel(response.EntryId);
    }

    public void PresentNotFound() => NotFound = true;
    public void PresentBadRequest(string message) => BadRequestMessage = message;
}
