namespace AchApi.UseCases.CreateAchFile;

public interface ICreateAchFileInputBoundary
{
    Task CreateAchFileAsync(ICreateAchFileOutputBoundary presenter, CreateAchFileRequestModel request);
}
