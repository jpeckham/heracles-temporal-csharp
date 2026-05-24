namespace AchWorker.UseCases.RecordRepresentment;

public interface IRecordRepresentmentOutputBoundary
{
    void Present(RecordRepresentmentResponseModel response);
}
