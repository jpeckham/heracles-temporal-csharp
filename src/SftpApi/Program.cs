using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Models;
using SftpApi.Data;
using Temporalio.Client;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<SftpDbContext>(opt =>
    opt.UseSqlite("Data Source=sftp.db"));
builder.Services.AddSingleton<ITemporalClient>(_ =>
    TemporalClient.ConnectAsync(new("localhost:7233")).GetAwaiter().GetResult());
builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<SftpDbContext>().Database.EnsureCreated();

// POST /files/outbound — receive outbound file (TransferFileRequest from AchWorker)
app.MapPost("/files/outbound", async (TransferFileRequest req, SftpDbContext db) =>
{
    var bytes = Convert.FromBase64String(req.ContentBase64);
    var hash = Convert.ToHexString(SHA256.HashData(bytes));

    var file = new TransferredFile
    {
        AchFileId = req.AchFileId,
        FileName = req.FileName,
        FileSizeBytes = bytes.Length,
        ContentHash = hash
    };
    db.TransferredFiles.Add(file);
    await db.SaveChangesAsync();
    return Results.Created($"/files/outbound/{file.FileId}", new { file.FileId });
});

// GET /files/outbound
app.MapGet("/files/outbound", async (SftpDbContext db) =>
    Results.Ok(await db.TransferredFiles.OrderByDescending(f => f.UploadedAt).ToListAsync()));

// DELETE /files/outbound/by-ach/{achFileId} — compensation by ACH file ID (idempotent)
app.MapDelete("/files/outbound/by-ach/{achFileId:guid}", async (Guid achFileId, SftpDbContext db) =>
{
    var files = db.TransferredFiles.Where(f => f.AchFileId == achFileId);
    db.TransferredFiles.RemoveRange(files);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// DELETE /files/outbound/{id} — compensation (idempotent)
app.MapDelete("/files/outbound/{id:guid}", async (Guid id, SftpDbContext db) =>
{
    var file = await db.TransferredFiles.FindAsync(id);
    if (file is null) return Results.Ok();
    db.TransferredFiles.Remove(file);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// POST /files/inbound — record inbound return file, start AchReturnWorkflow
app.MapPost("/files/inbound", async (InboundFileRequest req, SftpDbContext db, ITemporalClient temporal) =>
{
    var bytes = Convert.FromBase64String(req.ContentBase64);
    var hash = Convert.ToHexString(SHA256.HashData(bytes));

    var file = new ReceivedFile
    {
        FileName = req.FileName,
        ContentBase64 = req.ContentBase64,
        ContentHash = hash
    };
    db.ReceivedFiles.Add(file);
    await db.SaveChangesAsync();

    await temporal.StartWorkflowAsync(
        "AchReturnWorkflow",
        new object[] { file.FileId },
        new WorkflowOptions(id: $"ach-return-{file.FileId}", taskQueue: "ach-worker"));

    return Results.Created($"/files/inbound/{file.FileId}", new { file.FileId });
});

// GET /files/inbound/{id}/content
app.MapGet("/files/inbound/{id:guid}/content", async (Guid id, SftpDbContext db) =>
{
    var file = await db.ReceivedFiles.FindAsync(id);
    return file is null ? Results.NotFound() : Results.Ok(new { file.ContentBase64 });
});

// PATCH /files/inbound/{id}/status
app.MapMethods("/files/inbound/{id:guid}/status", ["PATCH"], async (Guid id, UpdateInboundStatusRequest req, SftpDbContext db) =>
{
    var file = await db.ReceivedFiles.FindAsync(id);
    if (file is null) return Results.NotFound();
    if (!Enum.TryParse<ReceivedFileStatus>(req.Status, out var status))
        return Results.BadRequest($"Invalid status: {req.Status}");
    file.Status = status;
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapGet("/health", () => Results.Ok("healthy"));
app.Run();

public record InboundFileRequest(string FileName, string ContentBase64);
public record UpdateInboundStatusRequest(string Status);
public partial class Program { }
