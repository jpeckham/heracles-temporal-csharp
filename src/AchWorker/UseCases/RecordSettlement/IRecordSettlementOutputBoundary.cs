namespace AchWorker.UseCases.RecordSettlement;

public interface IRecordSettlementOutputBoundary
{
    void Present(RecordSettlementResponseModel response);
}
