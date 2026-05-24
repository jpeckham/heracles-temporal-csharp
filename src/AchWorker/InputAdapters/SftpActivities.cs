using AchWorker.Presenters.DeleteTransferredFile;
using AchWorker.Presenters.MarkReceivedFileProcessed;
using AchWorker.Presenters.TransferAchFile;
using AchWorker.UseCases.DeleteTransferredFile;
using AchWorker.UseCases.MarkReceivedFileProcessed;
using AchWorker.UseCases.TransferAchFile;
using Temporalio.Activities;

namespace AchWorker.Activities;

public class SftpActivities(
    ITransferAchFileInputBoundary transferAchFile,
    IDeleteTransferredFileInputBoundary deleteTransferredFile,
    IMarkReceivedFileProcessedInputBoundary markReceivedFileProcessed)
{
    [Activity]
    public async Task<Guid> TransferAchFileAsync(Guid achFileId)
    {
        var presenter = new TransferAchFilePresenter();
        await transferAchFile.TransferAchFileAsync(presenter, new TransferAchFileRequestModel(achFileId));
        return presenter.ViewModel!.TransferredFileId;
    }

    [Activity]
    public async Task DeleteTransferredFileIfExistsAsync(Guid achFileId)
    {
        var presenter = new DeleteTransferredFilePresenter();
        await deleteTransferredFile.DeleteTransferredFileAsync(presenter, new DeleteTransferredFileRequestModel(achFileId));
    }

    [Activity]
    public async Task MarkReceivedFileProcessedAsync(Guid receivedFileId)
    {
        var presenter = new MarkReceivedFileProcessedPresenter();
        await markReceivedFileProcessed.MarkReceivedFileProcessedAsync(presenter, new MarkReceivedFileProcessedRequestModel(receivedFileId));
    }
}
