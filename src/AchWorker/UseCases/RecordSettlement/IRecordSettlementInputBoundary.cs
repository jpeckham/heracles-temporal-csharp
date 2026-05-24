namespace AchWorker.UseCases.RecordSettlement;

public interface IRecordSettlementInputBoundary
{
    Task RecordSettlementAsync(IRecordSettlementOutputBoundary presenter, RecordSettlementRequestModel request);
}
