namespace AchApi.UseCases.GetAchFileContent;

public interface IGetAchFileContentOutputBoundary
{
    void Present(GetAchFileContentResponseModel response);
    void PresentNotFound();
}
