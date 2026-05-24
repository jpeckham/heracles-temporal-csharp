namespace AchApi.UseCases.AddAchEntry;

public interface IAddAchEntryInputBoundary
{
    Task AddAchEntryAsync(IAddAchEntryOutputBoundary presenter, AddAchEntryRequestModel request);
}
