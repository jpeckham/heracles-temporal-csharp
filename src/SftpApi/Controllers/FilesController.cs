using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using SftpApi.Presenters.DeleteOutboundFile;
using SftpApi.Presenters.DeleteOutboundFileByAch;
using SftpApi.Presenters.GetInboundFileContent;
using SftpApi.Presenters.ListOutboundFiles;
using SftpApi.Presenters.ReceiveInboundFile;
using SftpApi.Presenters.ReceiveOutboundFile;
using SftpApi.Presenters.UpdateInboundFileStatus;
using SftpApi.UseCases.DeleteOutboundFile;
using SftpApi.UseCases.DeleteOutboundFileByAch;
using SftpApi.UseCases.GetInboundFileContent;
using SftpApi.UseCases.ListOutboundFiles;
using SftpApi.UseCases.ReceiveInboundFile;
using SftpApi.UseCases.ReceiveOutboundFile;
using SftpApi.UseCases.UpdateInboundFileStatus;

namespace SftpApi.Controllers;

[ApiController]
public class FilesController(
    IReceiveOutboundFileInputBoundary receiveOutbound,
    IListOutboundFilesInputBoundary listOutbound,
    IDeleteOutboundFileByAchInputBoundary deleteOutboundByAch,
    IDeleteOutboundFileInputBoundary deleteOutbound,
    IReceiveInboundFileInputBoundary receiveInbound,
    IGetInboundFileContentInputBoundary getInboundContent,
    IUpdateInboundFileStatusInputBoundary updateInboundStatus) : ControllerBase
{
    // POST /files/outbound
    [HttpPost("files/outbound")]
    public async Task<IActionResult> PostOutbound([FromBody] TransferFileRequest req)
    {
        var presenter = new ReceiveOutboundFilePresenter();
        await receiveOutbound.ExecuteAsync(presenter, new ReceiveOutboundFileRequestModel(req.AchFileId, req.FileName, req.ContentBase64));
        return Created($"/files/outbound/{((dynamic)presenter.ViewModel!).FileId}", presenter.ViewModel);
    }

    // GET /files/outbound
    [HttpGet("files/outbound")]
    public async Task<IActionResult> GetOutbound()
    {
        var presenter = new ListOutboundFilesPresenter();
        await listOutbound.ExecuteAsync(presenter);
        return Ok(presenter.ViewModel);
    }

    // DELETE /files/outbound/by-ach/{achFileId}
    [HttpDelete("files/outbound/by-ach/{achFileId:guid}")]
    public async Task<IActionResult> DeleteOutboundByAch(Guid achFileId)
    {
        var presenter = new DeleteOutboundFileByAchPresenter();
        await deleteOutboundByAch.ExecuteAsync(presenter, new DeleteOutboundFileByAchRequestModel(achFileId));
        return Ok();
    }

    // DELETE /files/outbound/{id}
    [HttpDelete("files/outbound/{id:guid}")]
    public async Task<IActionResult> DeleteOutbound(Guid id)
    {
        var presenter = new DeleteOutboundFilePresenter();
        await deleteOutbound.ExecuteAsync(presenter, new DeleteOutboundFileRequestModel(id));
        return Ok();
    }

    // POST /files/inbound
    [HttpPost("files/inbound")]
    public async Task<IActionResult> PostInbound([FromBody] InboundFileRequest req)
    {
        var presenter = new ReceiveInboundFilePresenter();
        await receiveInbound.ExecuteAsync(presenter, new ReceiveInboundFileRequestModel(req.FileName, req.ContentBase64));
        return Created($"/files/inbound/{((dynamic)presenter.ViewModel!).FileId}", presenter.ViewModel);
    }

    // GET /files/inbound/{id}/content
    [HttpGet("files/inbound/{id:guid}/content")]
    public async Task<IActionResult> GetInboundContent(Guid id)
    {
        var presenter = new GetInboundFileContentPresenter();
        await getInboundContent.ExecuteAsync(presenter, new GetInboundFileContentRequestModel(id));
        if (presenter.NotFound) return NotFound();
        return Ok(presenter.ViewModel);
    }

    // PATCH /files/inbound/{id}/status
    [HttpPatch("files/inbound/{id:guid}/status")]
    public async Task<IActionResult> PatchInboundStatus(Guid id, [FromBody] UpdateInboundStatusRequest req)
    {
        var presenter = new UpdateInboundFileStatusPresenter();
        await updateInboundStatus.ExecuteAsync(presenter, new UpdateInboundFileStatusRequestModel(id, req.Status));
        if (presenter.NotFound) return NotFound();
        if (presenter.InvalidStatus is not null) return BadRequest($"Invalid status: {presenter.InvalidStatus}");
        return Ok();
    }
}
