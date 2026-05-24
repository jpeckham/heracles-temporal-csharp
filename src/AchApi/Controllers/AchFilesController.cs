using AchApi.Presenters;
using AchApi.UseCases.AddAchEntry;
using AchApi.UseCases.CreateAchFile;
using AchApi.UseCases.DeleteAchEntry;
using AchApi.UseCases.DeleteAchFile;
using AchApi.UseCases.FinalizeAchFile;
using AchApi.UseCases.GetAchFileContent;
using AchApi.UseCases.ListAchFiles;
using AchApi.UseCases.UpdateAchFileStatus;
using Microsoft.AspNetCore.Mvc;

namespace AchApi.Controllers;

[ApiController]
[Route("files")]
public class AchFilesController(
    ICreateAchFileInputBoundary createAchFile,
    IAddAchEntryInputBoundary addAchEntry,
    IFinalizeAchFileInputBoundary finalizeAchFile,
    IDeleteAchFileInputBoundary deleteAchFile,
    IDeleteAchEntryInputBoundary deleteAchEntry,
    IUpdateAchFileStatusInputBoundary updateAchFileStatus,
    IGetAchFileContentInputBoundary getAchFileContent,
    IListAchFilesInputBoundary listAchFiles) : ControllerBase
{
    // POST /files
    [HttpPost]
    public async Task<IActionResult> CreateFile()
    {
        var presenter = new CreateAchFilePresenter();
        await createAchFile.CreateAchFileAsync(presenter, new CreateAchFileRequestModel());
        return Created($"/files/{presenter.ViewModel!.FileId}", presenter.ViewModel);
    }

    // GET /files
    [HttpGet]
    public async Task<IActionResult> ListFiles()
    {
        var presenter = new ListAchFilesPresenter();
        await listAchFiles.ListAchFilesAsync(presenter, new ListAchFilesRequestModel());
        return Ok(presenter.ViewModel!.Files);
    }

    // POST /files/{id}/entries/full
    [HttpPost("{id:guid}/entries/full")]
    public async Task<IActionResult> AddEntry(Guid id, [FromBody] AchEntryFullRequest req)
    {
        var presenter = new AddAchEntryPresenter();
        await addAchEntry.AddAchEntryAsync(presenter, new AddAchEntryRequestModel(
            id, req.PaymentId, req.RoutingNumber, req.AccountNumber,
            req.AccountHolderName, req.Amount, req.Type, req.RepresentmentCount));

        if (presenter.NotFound) return NotFound();
        if (presenter.BadRequestMessage is not null) return BadRequest(presenter.BadRequestMessage);
        return Created($"/files/{id}/entries/{presenter.ViewModel!.EntryId}", presenter.ViewModel);
    }

    // DELETE /files/{id}/entries/{entryId}
    [HttpDelete("{id:guid}/entries/{entryId:guid}")]
    public async Task<IActionResult> DeleteEntry(Guid id, Guid entryId)
    {
        var presenter = new DeleteAchEntryPresenter();
        await deleteAchEntry.DeleteAchEntryAsync(presenter, new DeleteAchEntryRequestModel(id, entryId));
        return Ok();
    }

    // POST /files/{id}/finalize
    [HttpPost("{id:guid}/finalize")]
    public async Task<IActionResult> FinalizeFile(Guid id)
    {
        var presenter = new FinalizeAchFilePresenter();
        await finalizeAchFile.FinalizeAchFileAsync(presenter, new FinalizeAchFileRequestModel(id));

        if (presenter.NotFound) return NotFound();
        if (presenter.BadRequestMessage is not null) return BadRequest(presenter.BadRequestMessage);
        return Ok(presenter.ViewModel);
    }

    // GET /files/{id}/content
    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> GetContent(Guid id)
    {
        var presenter = new GetAchFileContentPresenter();
        await getAchFileContent.GetAchFileContentAsync(presenter, new GetAchFileContentRequestModel(id));

        if (presenter.NotFound) return NotFound();
        return Ok(presenter.ViewModel);
    }

    // DELETE /files/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        var presenter = new DeleteAchFilePresenter();
        await deleteAchFile.DeleteAchFileAsync(presenter, new DeleteAchFileRequestModel(id));
        return Ok();
    }

    // PATCH /files/{id}/status
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req)
    {
        var presenter = new UpdateAchFileStatusPresenter();
        await updateAchFileStatus.UpdateAchFileStatusAsync(presenter, new UpdateAchFileStatusRequestModel(id, req.Status));

        if (presenter.NotFound) return NotFound();
        if (presenter.BadRequestMessage is not null) return BadRequest(presenter.BadRequestMessage);
        return Ok();
    }
}

public record AchEntryFullRequest(
    Guid PaymentId, string RoutingNumber, string AccountNumber,
    string AccountHolderName, decimal Amount, string Type, int RepresentmentCount = 0);

public record UpdateStatusRequest(string Status);
