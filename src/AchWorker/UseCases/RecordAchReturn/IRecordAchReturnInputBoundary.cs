namespace AchWorker.UseCases.RecordAchReturn;

public interface IRecordAchReturnInputBoundary
{
    Task RecordAchReturnAsync(IRecordAchReturnOutputBoundary presenter, RecordAchReturnRequestModel request);
}
