namespace AchWorker.UseCases.RecordAchReturn;

public interface IRecordAchReturnOutputBoundary
{
    void Present(RecordAchReturnResponseModel response);
}
