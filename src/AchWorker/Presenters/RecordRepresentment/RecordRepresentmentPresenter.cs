using AchWorker.UseCases.RecordRepresentment;

namespace AchWorker.Presenters.RecordRepresentment;

public class RecordRepresentmentPresenter : IRecordRepresentmentOutputBoundary
{
    public RecordRepresentmentResponseModel? ViewModel { get; private set; }

    public void Present(RecordRepresentmentResponseModel response)
    {
        ViewModel = response;
    }
}
