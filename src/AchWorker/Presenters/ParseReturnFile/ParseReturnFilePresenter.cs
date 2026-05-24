using AchWorker.UseCases.ParseReturnFile;

namespace AchWorker.Presenters.ParseReturnFile;

public class ParseReturnFilePresenter : IParseReturnFileOutputBoundary
{
    public ParseReturnFileResponseModel? ViewModel { get; private set; }

    public void Present(ParseReturnFileResponseModel response)
    {
        ViewModel = response;
    }
}
