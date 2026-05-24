namespace AchApi.UseCases.AddAchEntry;

public interface IAddAchEntryOutputBoundary
{
    void Present(AddAchEntryResponseModel response);
    void PresentNotFound();
    void PresentBadRequest(string message);
}
