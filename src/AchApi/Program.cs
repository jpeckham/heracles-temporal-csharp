using AchApi.Data;
using AchApi.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AchDbContext>(opt =>
    opt.UseSqlite("Data Source=ach.db"));
builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AchDbContext>().Database.EnsureCreated();

// POST /files
app.MapPost("/files", async (AchDbContext db) =>
{
    var file = new AchFile();
    db.AchFiles.Add(file);
    await db.SaveChangesAsync();
    return Results.Created($"/files/{file.FileId}", new { file.FileId, file.BatchNumber });
});

// GET /files
app.MapGet("/files", async (AchDbContext db) =>
{
    var files = await db.AchFiles.OrderByDescending(f => f.CreatedAt)
        .Select(f => new { f.FileId, f.BatchNumber, f.Status, f.CreatedAt, f.FinalizedAt })
        .ToListAsync();
    return Results.Ok(files);
});

// POST /files/{id}/entries/full
app.MapPost("/files/{id:guid}/entries/full", async (Guid id, AchEntryFullRequest req, AchDbContext db) =>
{
    var file = await db.AchFiles.FindAsync(id);
    if (file is null) return Results.NotFound();
    if (file.Status != AchFileStatus.Draft)
        return Results.BadRequest("File is not in Draft status.");

    var entry = new AchEntry
    {
        FileId = id,
        PaymentId = req.PaymentId,
        RoutingNumber = req.RoutingNumber,
        AccountNumber = req.AccountNumber,
        AccountHolderName = req.AccountHolderName,
        Amount = req.Amount,
        TransactionCode = req.Type == "Credit" ? "22" : "27",
        RepresentmentCount = req.RepresentmentCount
    };
    db.AchEntries.Add(entry);
    await db.SaveChangesAsync();
    return Results.Created($"/files/{id}/entries/{entry.EntryId}", new { entry.EntryId });
});

// DELETE /files/{id}/entries/{entryId}
app.MapDelete("/files/{id:guid}/entries/{entryId:guid}", async (Guid id, Guid entryId, AchDbContext db) =>
{
    var entry = await db.AchEntries.FindAsync(entryId);
    if (entry is null) return Results.Ok();
    db.AchEntries.Remove(entry);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// POST /files/{id}/finalize
app.MapPost("/files/{id:guid}/finalize", async (Guid id, AchDbContext db) =>
{
    var file = await db.AchFiles.Include(f => f.Entries).FirstOrDefaultAsync(f => f.FileId == id);
    if (file is null) return Results.NotFound();
    if (file.Entries.Count == 0) return Results.BadRequest("Cannot finalize empty file.");

    file.NachaContent = NachaFileGenerator.Generate(file);
    file.Status = AchFileStatus.Finalized;
    file.FinalizedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Ok(new {
        file.FileId, file.Status,
        ContentBase64 = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(file.NachaContent))
    });
});

// GET /files/{id}/content
app.MapGet("/files/{id:guid}/content", async (Guid id, AchDbContext db) =>
{
    var file = await db.AchFiles.FindAsync(id);
    if (file?.NachaContent is null) return Results.NotFound();
    return Results.Ok(new { ContentBase64 = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(file.NachaContent)) });
});

// DELETE /files/{id}
app.MapDelete("/files/{id:guid}", async (Guid id, AchDbContext db) =>
{
    var file = await db.AchFiles.FindAsync(id);
    if (file is null) return Results.Ok();
    db.AchFiles.Remove(file);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// PATCH /files/{id}/status
app.MapMethods("/files/{id:guid}/status", ["PATCH"], async (Guid id, UpdateStatusRequest req, AchDbContext db) =>
{
    var file = await db.AchFiles.FindAsync(id);
    if (file is null) return Results.NotFound();
    if (!Enum.TryParse<AchFileStatus>(req.Status, out var status))
        return Results.BadRequest($"Invalid status: {req.Status}");
    file.Status = status;
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapGet("/health", () => Results.Ok("healthy"));
app.Run();

public record AchEntryFullRequest(
    Guid PaymentId, string RoutingNumber, string AccountNumber,
    string AccountHolderName, decimal Amount, string Type, int RepresentmentCount = 0);
public record UpdateStatusRequest(string Status);
public partial class Program { }
