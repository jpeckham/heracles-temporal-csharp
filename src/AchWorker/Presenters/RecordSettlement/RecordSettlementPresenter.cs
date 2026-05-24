using AchWorker.UseCases.RecordSettlement;

namespace AchWorker.Presenters.RecordSettlement;

public class RecordSettlementPresenter : IRecordSettlementOutputBoundary
{
    public RecordSettlementResponseModel? ViewModel { get; private set; }

    public void Present(RecordSettlementResponseModel response)
    {
        ViewModel = response;
    }
}
