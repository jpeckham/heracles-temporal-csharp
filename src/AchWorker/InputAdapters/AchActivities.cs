using AchWorker.Entities;
using AchWorker.Presenters.AddAchEntry;
using AchWorker.Presenters.CreateAchFile;
using AchWorker.Presenters.DeleteAchFile;
using AchWorker.Presenters.FinalizeAchFile;
using AchWorker.Presenters.ParseReturnFile;
using AchWorker.Presenters.RevertAchFileToDraft;
using AchWorker.UseCases.AddAchEntry;
using AchWorker.UseCases.CreateAchFile;
using AchWorker.UseCases.DeleteAchFile;
using AchWorker.UseCases.FinalizeAchFile;
using AchWorker.UseCases.ParseReturnFile;
using AchWorker.UseCases.RevertAchFileToDraft;
using Temporalio.Activities;

namespace AchWorker.Activities;

public class AchActivities(
    ICreateAchFileInputBoundary createAchFile,
    IAddAchEntryInputBoundary addAchEntry,
    IFinalizeAchFileInputBoundary finalizeAchFile,
    IDeleteAchFileInputBoundary deleteAchFile,
    IRevertAchFileToDraftInputBoundary revertAchFileToDraft,
    IParseReturnFileInputBoundary parseReturnFile)
{
    [Activity]
    public async Task<Guid> CreateAchFileAsync()
    {
        var presenter = new CreateAchFilePresenter();
        await createAchFile.CreateAchFileAsync(presenter, new CreateAchFileRequestModel());
        return presenter.ViewModel!.FileId;
    }

    [Activity]
    public async Task<Guid> AddEntryAsync(Guid fileId, Guid paymentId, int representmentCount = 0)
    {
        var presenter = new AddAchEntryPresenter();
        await addAchEntry.AddAchEntryAsync(presenter, new AddAchEntryRequestModel(fileId, paymentId, representmentCount));
        return presenter.ViewModel!.EntryId;
    }

    [Activity]
    public async Task FinalizeAchFileAsync(Guid fileId)
    {
        var presenter = new FinalizeAchFilePresenter();
        await finalizeAchFile.FinalizeAchFileAsync(presenter, new FinalizeAchFileRequestModel(fileId));
    }

    [Activity]
    public async Task DeleteAchFileIfExistsAsync(Guid fileId)
    {
        var presenter = new DeleteAchFilePresenter();
        await deleteAchFile.DeleteAchFileAsync(presenter, new DeleteAchFileRequestModel(fileId));
    }

    [Activity]
    public async Task RevertAchFileToDraftAsync(Guid fileId)
    {
        var presenter = new RevertAchFileToDraftPresenter();
        await revertAchFileToDraft.RevertAchFileToDraftAsync(presenter, new RevertAchFileToDraftRequestModel(fileId));
    }

    [Activity]
    public async Task<List<AchReturnRecordDto>> ParseReturnFileAsync(Guid receivedFileId)
    {
        var presenter = new ParseReturnFilePresenter();
        await parseReturnFile.ParseReturnFileAsync(presenter, new ParseReturnFileRequestModel(receivedFileId));
        return presenter.ViewModel!.Records.Select(r => new AchReturnRecordDto(r.PaymentId, r.RCode)).ToList();
    }

    public record AchReturnRecordDto(Guid PaymentId, string RCode);
}
