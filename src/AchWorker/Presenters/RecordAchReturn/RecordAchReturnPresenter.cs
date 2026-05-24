using AchWorker.UseCases.RecordAchReturn;

namespace AchWorker.Presenters.RecordAchReturn;

public class RecordAchReturnPresenter : IRecordAchReturnOutputBoundary
{
    public RecordAchReturnResponseModel? ViewModel { get; private set; }

    public void Present(RecordAchReturnResponseModel response)
    {
        ViewModel = response;
    }
}
