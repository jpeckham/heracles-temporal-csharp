namespace AchApi.UseCases.GetAchFileContent;

public interface IGetAchFileContentInputBoundary
{
    Task GetAchFileContentAsync(IGetAchFileContentOutputBoundary presenter, GetAchFileContentRequestModel request);
}
