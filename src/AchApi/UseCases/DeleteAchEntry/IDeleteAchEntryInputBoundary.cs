namespace AchApi.UseCases.DeleteAchEntry;

public interface IDeleteAchEntryInputBoundary
{
    Task DeleteAchEntryAsync(IDeleteAchEntryOutputBoundary presenter, DeleteAchEntryRequestModel request);
}
