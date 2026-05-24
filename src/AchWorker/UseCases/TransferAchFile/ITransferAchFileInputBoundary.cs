namespace AchWorker.UseCases.TransferAchFile;

public interface ITransferAchFileInputBoundary
{
    Task TransferAchFileAsync(ITransferAchFileOutputBoundary presenter, TransferAchFileRequestModel request);
}
