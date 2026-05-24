namespace AchApi.UseCases.DeleteAchEntry;

public interface IDeleteAchEntryOutputBoundary
{
    void Present(DeleteAchEntryResponseModel response);
}
