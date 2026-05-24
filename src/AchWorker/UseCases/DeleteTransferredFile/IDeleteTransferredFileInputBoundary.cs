namespace AchWorker.UseCases.DeleteTransferredFile;

public interface IDeleteTransferredFileInputBoundary
{
    Task DeleteTransferredFileAsync(IDeleteTransferredFileOutputBoundary presenter, DeleteTransferredFileRequestModel request);
}
