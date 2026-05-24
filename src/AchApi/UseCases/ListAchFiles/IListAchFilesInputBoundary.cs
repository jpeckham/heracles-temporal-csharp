namespace AchApi.UseCases.ListAchFiles;

public interface IListAchFilesInputBoundary
{
    Task ListAchFilesAsync(IListAchFilesOutputBoundary presenter, ListAchFilesRequestModel request);
}
