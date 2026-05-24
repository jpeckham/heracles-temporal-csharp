namespace AchApi.UseCases.ListAchFiles;

public interface IListAchFilesOutputBoundary
{
    void Present(ListAchFilesResponseModel response);
}
