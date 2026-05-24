namespace AchWorker.UseCases.ParseReturnFile;

public interface IParseReturnFileInputBoundary
{
    Task ParseReturnFileAsync(IParseReturnFileOutputBoundary presenter, ParseReturnFileRequestModel request);
}
