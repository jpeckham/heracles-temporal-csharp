namespace AchWorker.UseCases.AddAchEntry;

public interface IAddAchEntryOutputBoundary
{
    void Present(AddAchEntryResponseModel response);
}
