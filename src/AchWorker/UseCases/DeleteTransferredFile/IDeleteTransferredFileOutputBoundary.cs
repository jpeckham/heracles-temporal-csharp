namespace AchWorker.UseCases.DeleteTransferredFile;

public interface IDeleteTransferredFileOutputBoundary
{
    void Present(DeleteTransferredFileResponseModel response);
}
