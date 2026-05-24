namespace AchWorker.UseCases.RecordRepresentment;

public interface IRecordRepresentmentInputBoundary
{
    Task RecordRepresentmentAsync(IRecordRepresentmentOutputBoundary presenter, RecordRepresentmentRequestModel request);
}
