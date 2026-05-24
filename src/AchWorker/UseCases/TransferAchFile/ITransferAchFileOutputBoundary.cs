namespace AchWorker.UseCases.TransferAchFile;

public interface ITransferAchFileOutputBoundary
{
    void Present(TransferAchFileResponseModel response);
}
