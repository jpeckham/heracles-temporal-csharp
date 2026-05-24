namespace AchWorker.UseCases.ParseReturnFile;

public interface IParseReturnFileOutputBoundary
{
    void Present(ParseReturnFileResponseModel response);
}
