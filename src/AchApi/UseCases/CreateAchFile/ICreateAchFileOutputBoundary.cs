namespace AchApi.UseCases.CreateAchFile;

public interface ICreateAchFileOutputBoundary
{
    void Present(CreateAchFileResponseModel response);
}
