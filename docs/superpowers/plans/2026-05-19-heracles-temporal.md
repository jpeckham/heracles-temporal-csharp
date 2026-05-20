# Heracles Temporal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reimplement Heracles ACH batch processing using Temporal.io — replacing Hangfire + MassTransit with Temporal Schedules, fan-out/fan-in workflows, and the saga compensation pattern.

**Architecture:** Approach B — dedicated AchWorker host separate from AchApi. PaymentApi and SftpApi are plain HTTP APIs; PaymentApi gets a Temporal client to start PaymentWorkflow at payment creation; SftpApi gets a Temporal client to start AchReturnWorkflow when an inbound return file is detected. AchWorker owns all three workflows and all activity implementations.

**Tech Stack:** .NET 8, C# 12, Temporalio 1.x, Temporalio.Extensions.Hosting, EF Core 8 + SQLite, FluentValidation 11, xUnit 2, Temporalio.Testing

**Spec correction:** PaymentApi gets a Temporal client (not "None") so it can start a `PaymentWorkflow` at payment creation time — required since workflows start at "create payment."

---

## File Map

```
src/
  Shared/
    Models/PaymentActivityType.cs
    Models/PaymentType.cs
    Models/AchFileStatus.cs
    Models/ReceivedFileStatus.cs
    Contracts/CreatePaymentRequest.cs
    Contracts/AddPaymentActivityRequest.cs
    Contracts/CreateAchEntryRequest.cs
    Contracts/TransferFileRequest.cs
    Contracts/BatchDetails.cs
    Contracts/AchReturnDetails.cs
  PaymentApi/
    Data/Payment.cs
    Data/PaymentActivity.cs
    Data/PaymentDbContext.cs
    Program.cs
  AchApi/
    Data/AchFile.cs
    Data/AchEntry.cs
    Data/AchDbContext.cs
    Services/NachaFileGenerator.cs
    Program.cs
  SftpApi/
    Data/TransferredFile.cs
    Data/ReceivedFile.cs
    Data/SftpDbContext.cs
    Program.cs
  AchWorker/
    Services/BankingCalendar.cs
    Activities/PaymentActivities.cs
    Activities/AchActivities.cs
    Activities/SftpActivities.cs
    Workflows/PaymentWorkflow.workflow.cs
    Workflows/AchBatchWorkflow.workflow.cs
    Workflows/AchReturnWorkflow.workflow.cs
    Program.cs
tests/
  Heracles.Activities.Tests/
    PaymentActivitiesTests.cs
    AchActivitiesTests.cs
  Heracles.Workflow.Tests/
    PaymentWorkflowTests.cs
    AchBatchWorkflowTests.cs
    AchReturnWorkflowTests.cs
  Heracles.Integration.Tests/
    AchBatchIntegrationTests.cs
    AchReturnIntegrationTests.cs
.editorconfig
docker-compose.yml
```

---

## Task 1: Solution Scaffold

**Files:**
- Create: `Heracles.sln` + all project files + `.editorconfig`

- [ ] **Step 1: Install Temporal CLI (Windows)**

Download from https://temporal.download/cli/archive/latest?platform=windows&arch=amd64, extract, add `temporal.exe` to PATH.

Verify: `temporal --version`

- [ ] **Step 2: Create solution and projects**

```powershell
cd C:\Users\james\source\repos\heracles-temporal-csharp

dotnet new sln -n Heracles
dotnet new classlib -n Shared -o src/Shared --framework net8.0
dotnet new webapi -n PaymentApi -o src/PaymentApi --framework net8.0 --no-openapi
dotnet new webapi -n AchApi -o src/AchApi --framework net8.0 --no-openapi
dotnet new webapi -n SftpApi -o src/SftpApi --framework net8.0 --no-openapi
dotnet new worker -n AchWorker -o src/AchWorker --framework net8.0
dotnet new xunit -n Heracles.Activities.Tests -o tests/Heracles.Activities.Tests --framework net8.0
dotnet new xunit -n Heracles.Workflow.Tests -o tests/Heracles.Workflow.Tests --framework net8.0
dotnet new xunit -n Heracles.Integration.Tests -o tests/Heracles.Integration.Tests --framework net8.0

dotnet sln add src/Shared src/PaymentApi src/AchApi src/SftpApi src/AchWorker
dotnet sln add tests/Heracles.Activities.Tests tests/Heracles.Workflow.Tests tests/Heracles.Integration.Tests
```

- [ ] **Step 3: Add project references**

```powershell
dotnet add src/PaymentApi reference src/Shared
dotnet add src/AchApi reference src/Shared
dotnet add src/SftpApi reference src/Shared
dotnet add src/AchWorker reference src/Shared

dotnet add tests/Heracles.Activities.Tests reference src/AchWorker
dotnet add tests/Heracles.Workflow.Tests reference src/AchWorker
dotnet add tests/Heracles.Integration.Tests reference src/PaymentApi
dotnet add tests/Heracles.Integration.Tests reference src/AchApi
dotnet add tests/Heracles.Integration.Tests reference src/SftpApi
dotnet add tests/Heracles.Integration.Tests reference src/AchWorker
```

- [ ] **Step 4: Add NuGet packages**

```powershell
# Shared
dotnet add src/Shared package FluentValidation

# PaymentApi
dotnet add src/PaymentApi package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/PaymentApi package Microsoft.EntityFrameworkCore.Design
dotnet add src/PaymentApi package FluentValidation.AspNetCore
dotnet add src/PaymentApi package Temporalio

# AchApi
dotnet add src/AchApi package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/AchApi package Microsoft.EntityFrameworkCore.Design
dotnet add src/AchApi package Temporalio

# SftpApi
dotnet add src/SftpApi package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/SftpApi package Microsoft.EntityFrameworkCore.Design
dotnet add src/SftpApi package Temporalio

# AchWorker
dotnet add src/AchWorker package Temporalio
dotnet add src/AchWorker package Temporalio.Extensions.Hosting

# Test projects
dotnet add tests/Heracles.Activities.Tests package Temporalio
dotnet add tests/Heracles.Activities.Tests package NSubstitute

dotnet add tests/Heracles.Workflow.Tests package Temporalio

dotnet add tests/Heracles.Integration.Tests package Temporalio
dotnet add tests/Heracles.Integration.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/Heracles.Integration.Tests package Microsoft.EntityFrameworkCore.Sqlite
```

- [ ] **Step 5: Write .editorconfig**

Create `.editorconfig` in repo root:

```ini
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# Temporal workflow files — suppress rules that conflict with workflow patterns
[*.workflow.cs]
dotnet_diagnostic.CA1024.severity = none
dotnet_diagnostic.CA1822.severity = none
dotnet_diagnostic.CA2007.severity = none
dotnet_diagnostic.CA2008.severity = none
dotnet_diagnostic.CA5394.severity = none
dotnet_diagnostic.CS1998.severity = none
dotnet_diagnostic.VSTHRD103.severity = none
dotnet_diagnostic.VSTHRD105.severity = none
```

- [ ] **Step 6: Verify build**

```powershell
dotnet build
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "chore: scaffold solution with all projects and packages"
```

---

## Task 2: Shared Contracts

**Files:**
- Create: all files under `src/Shared/`

- [ ] **Step 1: Write enum types**

`src/Shared/Models/PaymentType.cs`:
```csharp
namespace Shared.Models;
public enum PaymentType { Credit = 0, Debit = 1 }
```

`src/Shared/Models/PaymentActivityType.cs`:
```csharp
namespace Shared.Models;

public enum PaymentActivityType
{
    SoftAuth,
    HardAuth,
    Capture,
    Settlement,
    AchSubmitted,
    AchReturn,
    Representment,
    Void,
    Dispute,
    DisputeReversed,
    Refund,
    PaidOut
}
```

`src/Shared/Models/AchFileStatus.cs`:
```csharp
namespace Shared.Models;
public enum AchFileStatus { Draft, Finalized, Submitted, ReturnReceived, Reconciled }
```

`src/Shared/Models/ReceivedFileStatus.cs`:
```csharp
namespace Shared.Models;
public enum ReceivedFileStatus { Pending, Processing, Processed, Failed }
```

- [ ] **Step 2: Write contract types**

`src/Shared/Contracts/CreatePaymentRequest.cs`:
```csharp
namespace Shared.Contracts;

public record CreatePaymentRequest(
    string RoutingNumber,
    string AccountNumber,
    string AccountHolderName,
    decimal Amount,
    string Type,           // "Credit" or "Debit"
    bool AllowsRepresentment);
```

`src/Shared/Contracts/AddPaymentActivityRequest.cs`:
```csharp
namespace Shared.Contracts;

public record AddPaymentActivityRequest(
    string Type,           // PaymentActivityType name
    decimal? Amount = null,
    string? ReferenceCode = null,
    string? Notes = null);
```

`src/Shared/Contracts/CreateAchEntryRequest.cs`:
```csharp
namespace Shared.Contracts;

public record CreateAchEntryRequest(
    Guid PaymentId,
    int RepresentmentCount = 0);
```

`src/Shared/Contracts/TransferFileRequest.cs`:
```csharp
namespace Shared.Contracts;

public record TransferFileRequest(
    Guid AchFileId,
    string FileName,
    string ContentBase64);
```

`src/Shared/Contracts/BatchDetails.cs`:
```csharp
namespace Shared.Contracts;

public record BatchDetails(Guid AchFileId, bool IsSameDayAch);
```

`src/Shared/Contracts/AchReturnDetails.cs`:
```csharp
namespace Shared.Contracts;

public record AchReturnDetails(Guid PaymentId, string RCode, string? Description = null);
```

`src/Shared/Contracts/AchReturnRecord.cs`:
```csharp
namespace Shared.Contracts;

public record AchReturnRecord(Guid PaymentId, string RCode, string? Description);
```

- [ ] **Step 3: Delete default Class1.cs from Shared**

```powershell
Remove-Item src/Shared/Class1.cs
```

- [ ] **Step 4: Verify build**

```powershell
dotnet build src/Shared
```

Expected: Build succeeded.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat: add shared contracts, models, and enums"
```

---

## Task 3: PaymentApi

**Files:**
- Create: `src/PaymentApi/Data/Payment.cs`, `PaymentActivity.cs`, `PaymentDbContext.cs`, `Program.cs`

- [ ] **Step 1: Write domain entities**

`src/PaymentApi/Data/Payment.cs`:
```csharp
using Shared.Models;

namespace PaymentApi.Data;

public class Payment
{
    public Guid PaymentId { get; set; } = Guid.NewGuid();
    public string RoutingNumber { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountHolderName { get; set; } = "";
    public decimal Amount { get; set; }
    public PaymentType Type { get; set; }
    public bool AllowsRepresentment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<PaymentActivity> Activities { get; set; } = [];

    public string CurrentStatus => Activities.Count == 0
        ? "Pending"
        : Activities[^1].Type.ToString();
}
```

`src/PaymentApi/Data/PaymentActivity.cs`:
```csharp
using Shared.Models;

namespace PaymentApi.Data;

public class PaymentActivity
{
    public Guid ActivityId { get; set; } = Guid.NewGuid();
    public Guid PaymentId { get; set; }
    public PaymentActivityType Type { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public decimal? Amount { get; set; }
    public string? ReferenceCode { get; set; }
    public string? Notes { get; set; }
}
```

`src/PaymentApi/Data/PaymentDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;

namespace PaymentApi.Data;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentActivity> PaymentActivities => Set<PaymentActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>()
            .HasMany(p => p.Activities)
            .WithOne()
            .HasForeignKey(a => a.PaymentId);
    }
}
```

- [ ] **Step 2: Write Program.cs**

`src/PaymentApi/Program.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using PaymentApi.Data;
using Shared.Contracts;
using Shared.Models;
using Temporalio.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<PaymentDbContext>(opt =>
    opt.UseSqlite("Data Source=payments.db"));

// Temporal client — used only to start PaymentWorkflow at payment creation
builder.Services.AddSingleton<ITemporalClient>(_ =>
    TemporalClient.ConnectAsync(new("localhost:7233")).GetAwaiter().GetResult());

var app = builder.Build();

// Ensure DB created
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<PaymentDbContext>().Database.EnsureCreated();

// POST /payments — create payment and start PaymentWorkflow
app.MapPost("/payments", async (CreatePaymentRequest req, PaymentDbContext db, ITemporalClient temporal) =>
{
    if (!Enum.TryParse<PaymentType>(req.Type, out var type))
        return Results.BadRequest("Invalid payment type. Use 'Credit' or 'Debit'.");

    if (req.Amount <= 0 || req.Amount > 99_999_999.99m)
        return Results.BadRequest("Amount must be between 0.01 and 99,999,999.99.");

    if (req.RoutingNumber.Length != 9 || !req.RoutingNumber.All(char.IsDigit))
        return Results.BadRequest("Routing number must be 9 digits.");

    if (req.AccountNumber.Length > 17)
        return Results.BadRequest("Account number must be 17 chars or fewer.");

    if (req.AccountHolderName.Length > 22)
        return Results.BadRequest("Account holder name must be 22 chars or fewer (NACHA limit).");

    var payment = new Payment
    {
        RoutingNumber = req.RoutingNumber,
        AccountNumber = req.AccountNumber,
        AccountHolderName = req.AccountHolderName,
        Amount = req.Amount,
        Type = type,
        AllowsRepresentment = req.AllowsRepresentment
    };
    db.Payments.Add(payment);
    await db.SaveChangesAsync();

    // Start the payment's Temporal workflow
    await temporal.StartWorkflowAsync(
        "PaymentWorkflow",
        new object[] { payment.PaymentId, payment.AllowsRepresentment },
        new WorkflowOptions(id: $"payment-{payment.PaymentId}", taskQueue: "ach-worker"));

    return Results.Created($"/payments/{payment.PaymentId}", new { payment.PaymentId });
});

// GET /payments
app.MapGet("/payments", async (PaymentDbContext db, string? status) =>
{
    var query = db.Payments.Include(p => p.Activities).AsQueryable();
    var payments = await query.OrderByDescending(p => p.CreatedAt).Take(200).ToListAsync();
    if (status != null)
        payments = payments.Where(p => p.CurrentStatus == status).ToList();
    return Results.Ok(payments.Select(p => new {
        p.PaymentId, p.AccountHolderName, p.Amount, p.Type,
        p.AllowsRepresentment, p.CurrentStatus, p.CreatedAt
    }));
});

// GET /payments/{id}
app.MapGet("/payments/{id:guid}", async (Guid id, PaymentDbContext db) =>
{
    var payment = await db.Payments.Include(p => p.Activities)
        .FirstOrDefaultAsync(p => p.PaymentId == id);
    return payment is null ? Results.NotFound() : Results.Ok(payment);
});

// POST /payments/{id}/activities — append activity (called by AchWorker)
app.MapPost("/payments/{id:guid}/activities", async (Guid id, AddPaymentActivityRequest req, PaymentDbContext db) =>
{
    var payment = await db.Payments.FindAsync(id);
    if (payment is null) return Results.NotFound();

    if (!Enum.TryParse<PaymentActivityType>(req.Type, out var actType))
        return Results.BadRequest($"Unknown activity type: {req.Type}");

    var activity = new PaymentActivity
    {
        PaymentId = id,
        Type = actType,
        Amount = req.Amount,
        ReferenceCode = req.ReferenceCode,
        Notes = req.Notes
    };
    db.PaymentActivities.Add(activity);
    await db.SaveChangesAsync();
    return Results.Created($"/payments/{id}/activities/{activity.ActivityId}", activity);
});

app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();

public partial class Program { } // for WebApplicationFactory in tests
```

- [ ] **Step 3: Verify build**

```powershell
dotnet build src/PaymentApi
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "feat: implement PaymentApi with activity ledger and Temporal client"
```

---

## Task 4: AchApi

**Files:**
- Create: `src/AchApi/Data/`, `src/AchApi/Services/NachaFileGenerator.cs`, `src/AchApi/Program.cs`

- [ ] **Step 1: Write domain entities**

`src/AchApi/Data/AchFile.cs`:
```csharp
using Shared.Models;

namespace AchApi.Data;

public class AchFile
{
    public Guid FileId { get; set; } = Guid.NewGuid();
    public string BatchNumber { get; set; } = DateTime.UtcNow.ToString("yyyyMMdd");
    public AchFileStatus Status { get; set; } = AchFileStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinalizedAt { get; set; }
    public string? NachaContent { get; set; }
    public List<AchEntry> Entries { get; set; } = [];
}
```

`src/AchApi/Data/AchEntry.cs`:
```csharp
namespace AchApi.Data;

public class AchEntry
{
    public Guid EntryId { get; set; } = Guid.NewGuid();
    public Guid FileId { get; set; }
    public Guid PaymentId { get; set; }
    public string RoutingNumber { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountHolderName { get; set; } = "";
    public decimal Amount { get; set; }
    public string TransactionCode { get; set; } = "";  // 22=Credit, 27=Debit
    public int RepresentmentCount { get; set; }
}
```

`src/AchApi/Data/AchDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;

namespace AchApi.Data;

public class AchDbContext(DbContextOptions<AchDbContext> options) : DbContext(options)
{
    public DbSet<AchFile> AchFiles => Set<AchFile>();
    public DbSet<AchEntry> AchEntries => Set<AchEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AchFile>()
            .HasMany(f => f.Entries)
            .WithOne()
            .HasForeignKey(e => e.FileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 2: Write NachaFileGenerator**

`src/AchApi/Services/NachaFileGenerator.cs`:
```csharp
using System.Text;
using AchApi.Data;

namespace AchApi.Services;

public static class NachaFileGenerator
{
    private const string OdfiRouting = "07600001";  // sample ODFI
    private const string CompanyId = "1234567890";
    private const string CompanyName = "HERACLES LLC       ";  // 16 chars
    private const string ImmediateDest = " 076000010";         // 10 chars with leading space
    private const string ImmediateOrigin = "1234567890";       // 10 chars

    public static string Generate(AchFile file)
    {
        var sb = new StringBuilder();
        var effectiveDate = DateTime.UtcNow.ToString("yyMMdd");
        var creationDate = DateTime.UtcNow.ToString("yyMMdd");
        var creationTime = DateTime.UtcNow.ToString("HHmm");
        var batchNumber = "0000001";

        // Service class: 200=mixed, 220=credits, 225=debits
        var serviceClass = "200";
        long totalCredits = 0, totalDebits = 0;
        long entryHash = 0;

        // File Header (type 1)
        sb.AppendLine(Pad(
            $"101{ImmediateDest}{ImmediateOrigin}{creationDate}{creationTime}A094101" +
            $"{"DEST BANK".PadRight(23)}{"HERACLES LLC".PadRight(23)}{"        "}", 94));

        // Batch Header (type 5)
        sb.AppendLine(Pad(
            $"5{serviceClass}{CompanyName}{"".PadRight(20)}{CompanyId}PPD" +
            $"{"PAYROLL".PadRight(10)}{effectiveDate}{effectiveDate}   1{OdfiRouting}{batchNumber}", 94));

        // Entry Details (type 6)
        var entries = file.Entries;
        foreach (var entry in entries)
        {
            var routing8 = entry.RoutingNumber[..8];
            var checkDigit = entry.RoutingNumber[8];
            var amountCents = (long)(entry.Amount * 100);
            entryHash += long.Parse(routing8);

            if (entry.TransactionCode == "22") totalCredits += amountCents;
            else totalDebits += amountCents;

            var traceNumber = $"{OdfiRouting}{entry.EntryId.ToString("N")[..7]}";

            sb.AppendLine(Pad(
                $"6{entry.TransactionCode}{routing8}{checkDigit}" +
                $"{entry.AccountNumber.PadRight(17)}" +
                $"{amountCents.ToString().PadLeft(10, '0')}" +
                $"{"".PadRight(15)}" +
                $"{entry.AccountHolderName.PadRight(22)}" +
                $"  0{traceNumber}", 94));
        }

        // Batch Control (type 8)
        var hashMod = (entryHash % 10_000_000_000L).ToString().PadLeft(10, '0');
        sb.AppendLine(Pad(
            $"8{serviceClass}{entries.Count.ToString().PadLeft(6, '0')}{hashMod}" +
            $"{totalDebits.ToString().PadLeft(12, '0')}{totalCredits.ToString().PadLeft(12, '0')}" +
            $"{CompanyId}{"".PadRight(25)}{OdfiRouting}{batchNumber}", 94));

        // File Control (type 9)
        var totalRecords = entries.Count + 4; // header + batch header + batch control + file control
        var blockCount = (int)Math.Ceiling((totalRecords + 1) / 10.0);
        sb.AppendLine(Pad(
            $"9000001{blockCount.ToString().PadLeft(6, '0')}" +
            $"{entries.Count.ToString().PadLeft(8, '0')}{hashMod}" +
            $"{totalDebits.ToString().PadLeft(12, '0')}{totalCredits.ToString().PadLeft(12, '0')}" +
            $"{"".PadRight(39)}", 94));

        // Padding to complete the block (9-filled lines)
        var currentLines = entries.Count + 4;
        var paddingNeeded = (10 - (currentLines % 10)) % 10;
        for (var i = 0; i < paddingNeeded; i++)
            sb.AppendLine(new string('9', 94));

        return sb.ToString();
    }

    private static string Pad(string s, int length) =>
        s.Length >= length ? s[..length] : s.PadRight(length);
}
```

- [ ] **Step 3: Write Program.cs**

`src/AchApi/Program.cs`:
```csharp
using AchApi.Data;
using AchApi.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AchDbContext>(opt =>
    opt.UseSqlite("Data Source=ach.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AchDbContext>().Database.EnsureCreated();

// POST /files — create ACH file, returns FileId
app.MapPost("/files", async (AchDbContext db) =>
{
    var file = new AchFile();
    db.AchFiles.Add(file);
    await db.SaveChangesAsync();
    return Results.Created($"/files/{file.FileId}", new { file.FileId, file.BatchNumber });
});

// POST /files/{id}/entries — add a payment entry
app.MapPost("/files/{id:guid}/entries", async (Guid id, CreateAchEntryRequest req, AchDbContext db) =>
{
    var file = await db.AchFiles.FindAsync(id);
    if (file is null || file.Status != AchFileStatus.Draft) return Results.BadRequest("File not found or not in Draft status.");

    // Fetch payment details from PaymentApi to build the entry
    // (In the worker, this info is passed via the request)
    // We need RoutingNumber, AccountNumber, AccountHolderName, Amount, Type from the caller
    // The CreateAchEntryRequest carries PaymentId; the worker must enrich it.
    // For simplicity, the worker calls GET /payments/{id} before calling this endpoint,
    // and passes the enriched request. See AchActivities.cs for the full flow.
    return Results.BadRequest("Use POST /files/{id}/entries/full instead.");
});

// POST /files/{id}/entries/full — add entry with full payment data
app.MapPost("/files/{id:guid}/entries/full", async (Guid id, AchEntryFullRequest req, AchDbContext db) =>
{
    var file = await db.AchFiles.FindAsync(id);
    if (file is null || file.Status != AchFileStatus.Draft)
        return Results.BadRequest("File not found or not in Draft status.");

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

// POST /files/{id}/finalize — generate NACHA content
app.MapPost("/files/{id:guid}/finalize", async (Guid id, AchDbContext db) =>
{
    var file = await db.AchFiles.Include(f => f.Entries).FirstOrDefaultAsync(f => f.FileId == id);
    if (file is null) return Results.NotFound();
    if (file.Entries.Count == 0) return Results.BadRequest("Cannot finalize empty file.");

    file.NachaContent = NachaFileGenerator.Generate(file);
    file.Status = AchFileStatus.Finalized;
    file.FinalizedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Ok(new { file.FileId, file.Status, ContentBase64 = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(file.NachaContent)) });
});

// GET /files/{id}/content — get NACHA content as base64
app.MapGet("/files/{id:guid}/content", async (Guid id, AchDbContext db) =>
{
    var file = await db.AchFiles.FindAsync(id);
    if (file?.NachaContent is null) return Results.NotFound();
    return Results.Ok(new { ContentBase64 = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(file.NachaContent)) });
});

// DELETE /files/{id} — delete file (compensation)
app.MapDelete("/files/{id:guid}", async (Guid id, AchDbContext db) =>
{
    var file = await db.AchFiles.FindAsync(id);
    if (file is null) return Results.Ok(); // idempotent
    db.AchFiles.Remove(file);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// PATCH /files/{id}/status — revert status (compensation)
app.MapMethods("/files/{id:guid}/status", ["PATCH"], async (Guid id, UpdateStatusRequest req, AchDbContext db) =>
{
    var file = await db.AchFiles.FindAsync(id);
    if (file is null) return Results.NotFound();
    if (Enum.TryParse<AchFileStatus>(req.Status, out var status))
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
```

- [ ] **Step 4: Verify build**

```powershell
dotnet build src/AchApi
```

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat: implement AchApi with NACHA file generation"
```

---

## Task 5: SftpApi

**Files:**
- Create: `src/SftpApi/Data/`, `src/SftpApi/Program.cs`

- [ ] **Step 1: Write domain entities**

`src/SftpApi/Data/TransferredFile.cs`:
```csharp
namespace SftpApi.Data;

public class TransferredFile
{
    public Guid FileId { get; set; } = Guid.NewGuid();
    public Guid AchFileId { get; set; }
    public string FileName { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public string ContentHash { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Received";
}
```

`src/SftpApi/Data/ReceivedFile.cs`:
```csharp
using Shared.Models;

namespace SftpApi.Data;

public class ReceivedFile
{
    public Guid FileId { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = "";
    public string ContentBase64 { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public ReceivedFileStatus Status { get; set; } = ReceivedFileStatus.Pending;
}
```

`src/SftpApi/Data/SftpDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;

namespace SftpApi.Data;

public class SftpDbContext(DbContextOptions<SftpDbContext> options) : DbContext(options)
{
    public DbSet<TransferredFile> TransferredFiles => Set<TransferredFile>();
    public DbSet<ReceivedFile> ReceivedFiles => Set<ReceivedFile>();
}
```

- [ ] **Step 2: Write Program.cs**

`src/SftpApi/Program.cs`:
```csharp
using System.Security.Cryptography;
using System.Text;
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<SftpDbContext>().Database.EnsureCreated();

// POST /files/outbound — receive and store outbound file transfer
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

// DELETE /files/outbound/{id} — compensation
app.MapDelete("/files/outbound/{id:guid}", async (Guid id, SftpDbContext db) =>
{
    var file = await db.TransferredFiles.FindAsync(id);
    if (file is null) return Results.Ok(); // idempotent
    db.TransferredFiles.Remove(file);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// DELETE /files/outbound/by-ach/{achFileId} — compensation by ACH file ID
app.MapDelete("/files/outbound/by-ach/{achFileId:guid}", async (Guid achFileId, SftpDbContext db) =>
{
    var files = db.TransferredFiles.Where(f => f.AchFileId == achFileId);
    db.TransferredFiles.RemoveRange(files);
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

    // Start AchReturnWorkflow in AchWorker
    await temporal.StartWorkflowAsync(
        "AchReturnWorkflow",
        new object[] { file.FileId },
        new WorkflowOptions(
            id: $"ach-return-{file.FileId}",
            taskQueue: "ach-worker"));

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
    if (Enum.TryParse<ReceivedFileStatus>(req.Status, out var status))
        file.Status = status;
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapGet("/health", () => Results.Ok("healthy"));
app.Run();

public record InboundFileRequest(string FileName, string ContentBase64);
public record UpdateInboundStatusRequest(string Status);
public partial class Program { }
```

- [ ] **Step 3: Verify build**

```powershell
dotnet build src/SftpApi
```

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "feat: implement SftpApi with inbound/outbound file handling and Temporal client"
```

---

## Task 6: AchWorker — BankingCalendar + Activities

**Files:**
- Create: `src/AchWorker/Services/BankingCalendar.cs`
- Create: `src/AchWorker/Activities/PaymentActivities.cs`
- Create: `src/AchWorker/Activities/AchActivities.cs`
- Create: `src/AchWorker/Activities/SftpActivities.cs`

- [ ] **Step 1: Write BankingCalendar**

`src/AchWorker/Services/BankingCalendar.cs`:
```csharp
namespace AchWorker.Services;

/// <summary>
/// Computes ACH return windows in banking days (no weekends, no Fed holidays).
/// Used in workflow code — all inputs must be deterministic (use Workflow.UtcNow).
/// </summary>
public static class BankingCalendar
{
    public static TimeSpan GetReturnWindow(DateTime from, bool isSameDayAch)
    {
        var bankingDays = isSameDayAch ? 1 : 2;
        var current = from.Date;
        var added = 0;
        while (added < bankingDays)
        {
            current = current.AddDays(1);
            if (!IsWeekend(current) && !IsFedHoliday(current))
                added++;
        }
        // Window closes at end of that banking day (11:59 PM UTC)
        var windowEnd = current.AddDays(1).AddSeconds(-1);
        return windowEnd - from;
    }

    private static bool IsWeekend(DateTime d) =>
        d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private static bool IsFedHoliday(DateTime d)
    {
        var year = d.Year;
        var holidays = new[]
        {
            ObservedDate(new DateTime(year, 1, 1)),                    // New Year's Day
            NthWeekday(year, 1, DayOfWeek.Monday, 3),                 // MLK Day
            NthWeekday(year, 2, DayOfWeek.Monday, 3),                 // Presidents' Day
            LastWeekday(year, 5, DayOfWeek.Monday),                   // Memorial Day
            ObservedDate(new DateTime(year, 6, 19)),                   // Juneteenth
            ObservedDate(new DateTime(year, 7, 4)),                    // Independence Day
            NthWeekday(year, 9, DayOfWeek.Monday, 1),                 // Labor Day
            NthWeekday(year, 10, DayOfWeek.Monday, 2),                // Columbus Day
            ObservedDate(new DateTime(year, 11, 11)),                  // Veterans Day
            NthWeekday(year, 11, DayOfWeek.Thursday, 4),              // Thanksgiving
            ObservedDate(new DateTime(year, 12, 25)),                  // Christmas
        };
        return holidays.Any(h => h.Date == d.Date);
    }

    private static DateTime ObservedDate(DateTime holiday)
    {
        if (holiday.DayOfWeek == DayOfWeek.Saturday) return holiday.AddDays(-1);
        if (holiday.DayOfWeek == DayOfWeek.Sunday) return holiday.AddDays(1);
        return holiday;
    }

    private static DateTime NthWeekday(int year, int month, DayOfWeek dow, int n)
    {
        var d = new DateTime(year, month, 1);
        while (d.DayOfWeek != dow) d = d.AddDays(1);
        return d.AddDays(7 * (n - 1));
    }

    private static DateTime LastWeekday(int year, int month, DayOfWeek dow)
    {
        var d = new DateTime(year, month + 1, 1).AddDays(-1);
        while (d.DayOfWeek != dow) d = d.AddDays(-1);
        return d;
    }
}
```

- [ ] **Step 2: Write PaymentActivities**

`src/AchWorker/Activities/PaymentActivities.cs`:
```csharp
using Shared.Contracts;
using Temporalio.Activities;
using Temporalio.Client;

namespace AchWorker.Activities;

public class PaymentActivities(IHttpClientFactory httpFactory, ITemporalClient temporalClient)
{
    private HttpClient PaymentClient => httpFactory.CreateClient("PaymentApi");

    [Activity]
    public async Task<List<Guid>> CollectPendingPaymentsAsync()
    {
        var resp = await PaymentClient.GetFromJsonAsync<List<PaymentSummary>>("/payments?status=Pending");
        return resp?.Select(p => p.PaymentId).ToList() ?? [];
    }

    [Activity]
    public async Task HardAuthAsync(Guid paymentId)
    {
        var req = new AddPaymentActivityRequest("HardAuth");
        var resp = await PaymentClient.PostAsJsonAsync($"/payments/{paymentId}/activities", req);
        if (!resp.IsSuccessStatusCode)
            throw new ApplicationException($"HardAuth failed for {paymentId}: {resp.StatusCode}");
    }

    [Activity]
    public async Task VoidPaymentAuthIfExistsAsync(Guid paymentId)
    {
        // Idempotent compensation — only voids if payment exists
        var checkResp = await PaymentClient.GetAsync($"/payments/{paymentId}");
        if (!checkResp.IsSuccessStatusCode) return;

        var req = new AddPaymentActivityRequest("Void");
        await PaymentClient.PostAsJsonAsync($"/payments/{paymentId}/activities", req);
    }

    [Activity]
    public async Task RecordSettlementAsync(Guid paymentId)
    {
        var req = new AddPaymentActivityRequest("Settlement");
        await PaymentClient.PostAsJsonAsync($"/payments/{paymentId}/activities", req);
        var req2 = new AddPaymentActivityRequest("PaidOut");
        await PaymentClient.PostAsJsonAsync($"/payments/{paymentId}/activities", req2);
    }

    [Activity]
    public async Task RecordAchReturnAsync(Guid paymentId, AchReturnDetails details)
    {
        var req = new AddPaymentActivityRequest("AchReturn", ReferenceCode: details.RCode, Notes: details.Description);
        var resp = await PaymentClient.PostAsJsonAsync($"/payments/{paymentId}/activities", req);
        if (!resp.IsSuccessStatusCode)
            throw new ApplicationException($"RecordAchReturn failed: {resp.StatusCode}");
    }

    [Activity]
    public async Task RecordRepresentmentAsync(Guid paymentId, int representmentCount)
    {
        var req = new AddPaymentActivityRequest("Representment", Notes: $"Attempt {representmentCount}");
        await PaymentClient.PostAsJsonAsync($"/payments/{paymentId}/activities", req);
        // Reset payment to Pending so next batch picks it up
        var pendingReq = new AddPaymentActivityRequest("SoftAuth", Notes: "Re-queued for representment");
        await PaymentClient.PostAsJsonAsync($"/payments/{paymentId}/activities", pendingReq);
    }

    [Activity]
    public async Task SignalPaymentAddedToBatchAsync(Guid paymentId, Guid achFileId, bool isSameDayAch)
    {
        var handle = temporalClient.GetWorkflowHandle($"payment-{paymentId}");
        await handle.SignalAsync("AddedToBatch", [new BatchDetails(achFileId, isSameDayAch)]);
    }

    private record PaymentSummary(Guid PaymentId, string CurrentStatus);
}
```

- [ ] **Step 3: Write AchActivities**

`src/AchWorker/Activities/AchActivities.cs`:
```csharp
using Temporalio.Activities;

namespace AchWorker.Activities;

public class AchActivities(IHttpClientFactory httpFactory)
{
    private HttpClient AchClient => httpFactory.CreateClient("AchApi");
    private HttpClient PaymentClient => httpFactory.CreateClient("PaymentApi");

    [Activity]
    public async Task<Guid> CreateAchFileAsync()
    {
        var resp = await AchClient.PostAsJsonAsync("/files", new { });
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<FileCreatedResponse>();
        return result!.FileId;
    }

    [Activity]
    public async Task<Guid> AddEntryAsync(Guid fileId, Guid paymentId, int representmentCount = 0)
    {
        // Fetch payment details
        var payment = await PaymentClient.GetFromJsonAsync<PaymentDetail>($"/payments/{paymentId}");
        if (payment is null) throw new ApplicationException($"Payment {paymentId} not found");

        var req = new
        {
            payment.PaymentId,
            payment.RoutingNumber,
            payment.AccountNumber,
            payment.AccountHolderName,
            payment.Amount,
            payment.Type,
            RepresentmentCount = representmentCount
        };
        var resp = await AchClient.PostAsJsonAsync($"/files/{fileId}/entries/full", req);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<EntryCreatedResponse>();
        return result!.EntryId;
    }

    [Activity]
    public async Task FinalizeAchFileAsync(Guid fileId)
    {
        var resp = await AchClient.PostAsJsonAsync($"/files/{fileId}/finalize", new { });
        if (!resp.IsSuccessStatusCode)
            throw new ApplicationException($"Finalize failed: {await resp.Content.ReadAsStringAsync()}");
    }

    [Activity]
    public async Task DeleteAchFileIfExistsAsync(Guid fileId)
    {
        await AchClient.DeleteAsync($"/files/{fileId}"); // 200 or 404 both OK
    }

    [Activity]
    public async Task RevertAchFileToDraftAsync(Guid fileId)
    {
        await AchClient.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/files/{fileId}/status")
        {
            Content = JsonContent.Create(new { Status = "Draft" })
        });
    }

    [Activity]
    public async Task<List<AchReturnRecordDto>> ParseReturnFileAsync(Guid receivedFileId)
    {
        // Fetch raw content from SftpApi
        var sftpClient = httpFactory.CreateClient("SftpApi");
        var content = await sftpClient.GetFromJsonAsync<ContentResponse>($"/files/inbound/{receivedFileId}/content");
        if (content is null) return [];

        var nachaText = System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(content.ContentBase64));
        var returns = new List<AchReturnRecordDto>();

        foreach (var line in nachaText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            // Type 6 = Entry Detail, Type 7 = Addenda (return reason in addenda)
            // For simplicity: parse type-7 addenda records which contain R-codes
            if (line.Length < 94 || line[0] != '7') continue;
            var rCode = line[3..6].Trim();
            // Trace number in positions 79-93 links back to original entry
            // For the sample, we embed PaymentId in the trace number field
            // In production you'd look up by trace number
            if (Guid.TryParse(line[13..49].Trim(), out var paymentId))
                returns.Add(new AchReturnRecordDto(paymentId, rCode));
        }

        return returns;
    }

    private record FileCreatedResponse(Guid FileId);
    private record EntryCreatedResponse(Guid EntryId);
    private record PaymentDetail(Guid PaymentId, string RoutingNumber, string AccountNumber,
        string AccountHolderName, decimal Amount, string Type);
    private record ContentResponse(string ContentBase64);
    public record AchReturnRecordDto(Guid PaymentId, string RCode);
}
```

- [ ] **Step 4: Write SftpActivities**

`src/AchWorker/Activities/SftpActivities.cs`:
```csharp
using Temporalio.Activities;

namespace AchWorker.Activities;

public class SftpActivities(IHttpClientFactory httpFactory)
{
    private HttpClient SftpClient => httpFactory.CreateClient("SftpApi");
    private HttpClient AchClient => httpFactory.CreateClient("AchApi");

    [Activity]
    public async Task<Guid> TransferAchFileAsync(Guid achFileId)
    {
        // Fetch finalized content from AchApi
        var content = await AchClient.GetFromJsonAsync<ContentResponse>($"/files/{achFileId}/content");
        if (content is null) throw new ApplicationException("ACH file content not found");

        var fileName = $"ACH_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
        var req = new
        {
            AchFileId = achFileId,
            FileName = fileName,
            content.ContentBase64
        };

        var resp = await SftpClient.PostAsJsonAsync("/files/outbound", req);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<TransferResponse>();
        return result!.FileId;
    }

    [Activity]
    public async Task DeleteTransferredFileIfExistsAsync(Guid achFileId)
    {
        // Use by-ach endpoint — idempotent
        await SftpClient.DeleteAsync($"/files/outbound/by-ach/{achFileId}");
    }

    [Activity]
    public async Task MarkReceivedFileProcessedAsync(Guid receivedFileId)
    {
        await SftpClient.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/files/inbound/{receivedFileId}/status")
        {
            Content = JsonContent.Create(new { Status = "Processed" })
        });
    }

    private record ContentResponse(string ContentBase64);
    private record TransferResponse(Guid FileId);
}
```

- [ ] **Step 5: Verify build**

```powershell
dotnet build src/AchWorker
```

- [ ] **Step 6: Commit**

```powershell
git add -A
git commit -m "feat: add BankingCalendar and all three activity classes"
```

---

## Task 7: PaymentWorkflow

**Files:**
- Create: `src/AchWorker/Workflows/PaymentWorkflow.workflow.cs`

- [ ] **Step 1: Write the workflow**

`src/AchWorker/Workflows/PaymentWorkflow.workflow.cs`:
```csharp
using AchWorker.Activities;
using AchWorker.Services;
using Shared.Contracts;
using Temporalio.Activities;
using Temporalio.Exceptions;
using Temporalio.Workflows;

namespace AchWorker.Workflows;

[Workflow]
public class PaymentWorkflow
{
    private BatchDetails? _batchDetails;
    private AchReturnDetails? _bankReturn;
    private int _representmentCount;

    [WorkflowInit]
    public PaymentWorkflow(Guid paymentId, bool allowsRepresentment)
    {
        PaymentId = paymentId;
        AllowsRepresentment = allowsRepresentment;
    }

    private Guid PaymentId { get; }
    private bool AllowsRepresentment { get; }

    [WorkflowSignal]
    public async Task AddedToBatchAsync(BatchDetails details)
    {
        _batchDetails = details;
    }

    [WorkflowSignal]
    public async Task BankReturnAsync(AchReturnDetails details)
    {
        _bankReturn = details;
    }

    [WorkflowQuery]
    public string GetStatus()
    {
        if (_batchDetails is null) return "AwaitingBatch";
        if (_bankReturn is null) return "AwaitingSettlement";
        return _representmentCount > 0 ? "Representment" : "Returned";
    }

    [WorkflowRun]
    public async Task RunAsync(Guid paymentId, bool allowsRepresentment)
    {
        var activityOptions = new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(2) };

        // SoftAuth on creation
        await Workflow.ExecuteActivityAsync(
            (PaymentActivities a) => a.HardAuthAsync(paymentId),
            activityOptions);

        // Wait for inclusion in a batch (no timeout — payment waits until processed or cancelled)
        await Workflow.WaitConditionAsync(() => _batchDetails != null);

        while (true)
        {
            _bankReturn = null;

            // Compute return window using Workflow.UtcNow (deterministic)
            var returnWindow = BankingCalendar.GetReturnWindow(Workflow.UtcNow, _batchDetails!.IsSameDayAch);

            var returned = await Workflow.WaitConditionAsync(
                () => _bankReturn != null, returnWindow);

            if (!returned)
            {
                // Timer expired — no return, settle
                await Workflow.ExecuteActivityAsync(
                    (PaymentActivities a) => a.RecordSettlementAsync(paymentId),
                    activityOptions);
                return;
            }

            // Bank return received
            await Workflow.ExecuteActivityAsync(
                (PaymentActivities a) => a.RecordAchReturnAsync(paymentId, _bankReturn!),
                activityOptions);

            var isRepresentable = _bankReturn!.RCode == "R01";

            if (!isRepresentable || !allowsRepresentment || _representmentCount >= 2)
            {
                // Terminal — hard failure or representment limit reached
                Workflow.Logger.LogWarning(
                    "Payment {PaymentId} terminal after return {RCode}, representments: {Count}",
                    paymentId, _bankReturn.RCode, _representmentCount);
                return;
            }

            // Representment allowed — re-queue for next batch
            _representmentCount++;
            await Workflow.ExecuteActivityAsync(
                (PaymentActivities a) => a.RecordRepresentmentAsync(paymentId, _representmentCount),
                activityOptions);

            // Reset and wait for next batch inclusion
            _batchDetails = null;
            await Workflow.WaitConditionAsync(() => _batchDetails != null);
        }
    }
}
```

- [ ] **Step 2: Verify build**

```powershell
dotnet build src/AchWorker
```

- [ ] **Step 3: Commit**

```powershell
git add -A
git commit -m "feat: implement PaymentWorkflow with return window and representment"
```

---

## Task 8: AchBatchWorkflow

**Files:**
- Create: `src/AchWorker/Workflows/AchBatchWorkflow.workflow.cs`

- [ ] **Step 1: Write the workflow**

`src/AchWorker/Workflows/AchBatchWorkflow.workflow.cs`:
```csharp
using AchWorker.Activities;
using Temporalio.Exceptions;
using Temporalio.Workflows;

namespace AchWorker.Workflows;

[Workflow]
public class AchBatchWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        var compensations = new List<Func<Task>>();
        var shortTimeout = new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(2) };
        var longTimeout = new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(10) };

        try
        {
            // 1. Collect pending payments
            var paymentIds = await Workflow.ExecuteActivityAsync(
                (PaymentActivities a) => a.CollectPendingPaymentsAsync(), shortTimeout);

            if (paymentIds.Count == 0)
                throw new ApplicationFailureException("No pending payments to process.");

            // 2. Fan out HardAuth with semaphore (max 50 concurrent)
            var semaphore = new Temporalio.Workflows.Semaphore(50);
            var authResults = await Workflow.WhenAllAsync(
                paymentIds.Select(id => AuthorizeAsync(id, semaphore, compensations)));

            var authorized = authResults.Where(r => r.Success).Select(r => r.Id).ToList();
            if (authorized.Count == 0)
                throw new ApplicationFailureException("All payment authorizations failed.");

            Workflow.Logger.LogInformation("Authorized {Count}/{Total} payments", authorized.Count, paymentIds.Count);

            // 3. Create ACH file
            var fileId = Guid.Empty;
            compensations.Add(() => Workflow.ExecuteActivityAsync(
                (AchActivities a) => a.DeleteAchFileIfExistsAsync(fileId), shortTimeout));
            fileId = await Workflow.ExecuteActivityAsync(
                (AchActivities a) => a.CreateAchFileAsync(), shortTimeout);

            // 4. Fan out AddEntry (all parallel, no semaphore needed for ACH API)
            await Workflow.WhenAllAsync(
                authorized.Select(id => AddEntryAsync(fileId, id, shortTimeout)));

            // 5. Finalize
            compensations.Add(() => Workflow.ExecuteActivityAsync(
                (AchActivities a) => a.RevertAchFileToDraftAsync(fileId), shortTimeout));
            await Workflow.ExecuteActivityAsync(
                (AchActivities a) => a.FinalizeAchFileAsync(fileId), shortTimeout);

            // 6. Transfer to SFTP
            var transferredFileId = Guid.Empty;
            compensations.Add(() => Workflow.ExecuteActivityAsync(
                (SftpActivities a) => a.DeleteTransferredFileIfExistsAsync(fileId), shortTimeout));
            transferredFileId = await Workflow.ExecuteActivityAsync(
                (SftpActivities a) => a.TransferAchFileAsync(fileId), longTimeout);

            Workflow.Logger.LogInformation("ACH file {FileId} transferred as {TransferredId}", fileId, transferredFileId);

            // 7. Signal each payment workflow that it was included in this batch
            var isSameDayAch = false; // standard ACH — extend to config if needed
            await Workflow.WhenAllAsync(
                authorized.Select(id => Workflow.ExecuteActivityAsync(
                    (PaymentActivities a) => a.SignalPaymentAddedToBatchAsync(id, fileId, isSameDayAch),
                    shortTimeout)));
        }
        catch (Exception ex) when (!TemporalException.IsCanceledException(ex))
        {
            Workflow.Logger.LogError(ex, "AchBatchWorkflow failed, running compensation");
            compensations.Reverse();
            foreach (var comp in compensations)
            {
                try { await comp(); }
                catch (Exception ce) { Workflow.Logger.LogError(ce, "Compensation step failed"); }
            }
            throw;
        }
    }

    private async Task<AuthResult> AuthorizeAsync(
        Guid paymentId,
        Temporalio.Workflows.Semaphore semaphore,
        List<Func<Task>> compensations)
    {
        await semaphore.WaitAsync();
        try
        {
            var opts = new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(2) };
            compensations.Add(() => Workflow.ExecuteActivityAsync(
                (PaymentActivities a) => a.VoidPaymentAuthIfExistsAsync(paymentId), opts));
            await Workflow.ExecuteActivityAsync(
                (PaymentActivities a) => a.HardAuthAsync(paymentId), opts);
            return new AuthResult(paymentId, true);
        }
        catch (ActivityFailureException ex)
        {
            Workflow.Logger.LogWarning(ex, "HardAuth failed for payment {PaymentId}", paymentId);
            return new AuthResult(paymentId, false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task AddEntryAsync(Guid fileId, Guid paymentId, ActivityOptions opts)
    {
        await Workflow.ExecuteActivityAsync(
            (AchActivities a) => a.AddEntryAsync(fileId, paymentId, 0), opts);
    }

    private record AuthResult(Guid Id, bool Success);
}
```

- [ ] **Step 2: Verify build**

```powershell
dotnet build src/AchWorker
```

- [ ] **Step 3: Commit**

```powershell
git add -A
git commit -m "feat: implement AchBatchWorkflow with fan-out/fan-in and saga compensation"
```

---

## Task 9: AchReturnWorkflow + AchWorker Program.cs

**Files:**
- Create: `src/AchWorker/Workflows/AchReturnWorkflow.workflow.cs`
- Modify: `src/AchWorker/Program.cs`

- [ ] **Step 1: Write AchReturnWorkflow**

`src/AchWorker/Workflows/AchReturnWorkflow.workflow.cs`:
```csharp
using AchWorker.Activities;
using Shared.Contracts;
using Temporalio.Workflows;

namespace AchWorker.Workflows;

[Workflow]
public class AchReturnWorkflow
{
    [WorkflowRun]
    public async Task RunAsync(Guid receivedFileId)
    {
        var shortTimeout = new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(5) };

        // 1. Parse return file into R-code records
        var returnRecords = await Workflow.ExecuteActivityAsync(
            (AchActivities a) => a.ParseReturnFileAsync(receivedFileId), shortTimeout);

        Workflow.Logger.LogInformation("Processing {Count} ACH return records", returnRecords.Count);

        // 2. Fan out — record return activity + signal each PaymentWorkflow
        await Workflow.WhenAllAsync(returnRecords.Select(record =>
            ProcessReturnAsync(record, shortTimeout)));

        // 3. Mark received file as processed
        await Workflow.ExecuteActivityAsync(
            (SftpActivities a) => a.MarkReceivedFileProcessedAsync(receivedFileId), shortTimeout);
    }

    private async Task ProcessReturnAsync(
        AchActivities.AchReturnRecordDto record,
        ActivityOptions opts)
    {
        // Record return in payment ledger
        var details = new AchReturnDetails(record.PaymentId, record.RCode);

        try
        {
            await Workflow.ExecuteActivityAsync(
                (PaymentActivities a) => a.RecordAchReturnAsync(record.PaymentId, details), opts);
        }
        catch (Exception ex)
        {
            Workflow.Logger.LogWarning(ex, "Failed to record return for payment {Id}", record.PaymentId);
        }

        // Signal the PaymentWorkflow — if it's no longer running, log and continue
        try
        {
            await Workflow.ExecuteActivityAsync(
                (PaymentActivities a) => a.SignalBankReturnAsync(record.PaymentId, details), opts);
        }
        catch (Exception ex)
        {
            Workflow.Logger.LogWarning(ex, "Could not signal PaymentWorkflow for {Id} — may have already completed", record.PaymentId);
        }
    }
}
```

- [ ] **Step 2: Add SignalBankReturnAsync to PaymentActivities**

Add this method to `src/AchWorker/Activities/PaymentActivities.cs`:

```csharp
[Activity]
public async Task SignalBankReturnAsync(Guid paymentId, AchReturnDetails details)
{
    var handle = temporalClient.GetWorkflowHandle($"payment-{paymentId}");
    await handle.SignalAsync("BankReturn", [details]);
}
```

- [ ] **Step 3: Write AchWorker Program.cs**

`src/AchWorker/Program.cs`:
```csharp
using AchWorker.Activities;
using AchWorker.Workflows;
using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using Temporalio.Client.Schedules;

var builder = Host.CreateApplicationBuilder(args);

// HTTP clients for each downstream API
builder.Services.AddHttpClient("PaymentApi", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:PaymentApi"] ?? "http://localhost:5001"));
builder.Services.AddHttpClient("AchApi", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:AchApi"] ?? "http://localhost:5002"));
builder.Services.AddHttpClient("SftpApi", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:SftpApi"] ?? "http://localhost:5003"));

// Temporal client (shared instance for activities that signal other workflows)
builder.Services.AddSingleton<ITemporalClient>(_ =>
    TemporalClient.ConnectAsync(new(
        builder.Configuration["Temporal:Address"] ?? "localhost:7233")).GetAwaiter().GetResult());

// Temporal worker with all workflows and activities
builder.Services
    .AddHostedTemporalWorker(
        clientTargetHost: builder.Configuration["Temporal:Address"] ?? "localhost:7233",
        clientNamespace: "default",
        taskQueue: "ach-worker")
    .AddScopedActivities<PaymentActivities>()
    .AddScopedActivities<AchActivities>()
    .AddScopedActivities<SftpActivities>()
    .AddWorkflow<PaymentWorkflow>()
    .AddWorkflow<AchBatchWorkflow>()
    .AddWorkflow<AchReturnWorkflow>();

// Register schedule at startup
builder.Services.AddHostedService<ScheduleRegistrationService>();

var host = builder.Build();
await host.RunAsync();

// Background service that registers the daily ACH batch schedule idempotently
public class ScheduleRegistrationService(ITemporalClient client, ILogger<ScheduleRegistrationService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await client.CreateScheduleAsync(
                "daily-ach-batch",
                new Schedule(
                    Action: ScheduleActionStartWorkflow.Create(
                        "AchBatchWorkflow",
                        Array.Empty<object>(),
                        new WorkflowOptions(
                            id: $"ach-batch-{DateTime.UtcNow:yyyyMMdd}",
                            taskQueue: "ach-worker")),
                    Spec: new ScheduleSpec
                    {
                        CronExpressions = ["0 17 * * 1-5"],
                        Jitter = TimeSpan.FromMinutes(1)
                    }),
                new CreateScheduleOptions { TriggerImmediatelyIfMissed = false });

            logger.LogInformation("Daily ACH batch schedule registered");
        }
        catch (Temporalio.Exceptions.RpcException ex) when (ex.Code == Temporalio.Exceptions.RpcException.StatusCode.AlreadyExists)
        {
            logger.LogInformation("Daily ACH batch schedule already exists — skipping");
        }
    }
}
```

- [ ] **Step 4: Verify build**

```powershell
dotnet build src/AchWorker
```

- [ ] **Step 5: Verify full solution builds**

```powershell
dotnet build
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```powershell
git add -A
git commit -m "feat: implement AchReturnWorkflow and AchWorker host with schedule registration"
```

---

## Task 10: Activity Unit Tests

**Files:**
- Create: `tests/Heracles.Activities.Tests/PaymentActivitiesTests.cs`
- Create: `tests/Heracles.Activities.Tests/AchActivitiesTests.cs`

- [ ] **Step 1: Write PaymentActivities tests**

`tests/Heracles.Activities.Tests/PaymentActivitiesTests.cs`:
```csharp
using AchWorker.Activities;
using NSubstitute;
using Shared.Contracts;
using Temporalio.Testing;
using Xunit;

namespace Heracles.Activities.Tests;

public class PaymentActivitiesTests
{
    private static (PaymentActivities activities, HttpClient client) CreateActivities(
        HttpResponseMessage response)
    {
        var handler = new FakeHttpHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://payment-api") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("PaymentApi").Returns(httpClient);
        factory.CreateClient("AchApi").Returns(new HttpClient());
        factory.CreateClient("SftpApi").Returns(new HttpClient());

        var temporalClient = Substitute.For<Temporalio.Client.ITemporalClient>();
        var activities = new PaymentActivities(factory, temporalClient);
        return (activities, httpClient);
    }

    [Fact]
    public async Task CollectPendingPayments_ReturnsList()
    {
        var json = """[{"paymentId":"11111111-1111-1111-1111-111111111111","currentStatus":"Pending"}]""";
        var (activities, _) = CreateActivities(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

        var env = new ActivityEnvironment();
        var result = await env.RunAsync(() => activities.CollectPendingPaymentsAsync());

        Assert.Single(result);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), result[0]);
    }

    [Fact]
    public async Task HardAuth_NonSuccessStatus_Throws()
    {
        var (activities, _) = CreateActivities(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        });

        var env = new ActivityEnvironment();
        await Assert.ThrowsAsync<ApplicationException>(() =>
            env.RunAsync(() => activities.HardAuthAsync(Guid.NewGuid())));
    }

    [Fact]
    public async Task VoidPaymentAuthIfExists_PaymentNotFound_DoesNotThrow()
    {
        var (activities, _) = CreateActivities(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        });

        var env = new ActivityEnvironment();
        // Should not throw — idempotent
        await env.RunAsync(() => activities.VoidPaymentAuthIfExistsAsync(Guid.NewGuid()));
    }
}

public class FakeHttpHandler(HttpResponseMessage response) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(response);
}
```

- [ ] **Step 2: Run activity tests**

```powershell
dotnet test tests/Heracles.Activities.Tests -v normal
```

Expected: All tests pass.

- [ ] **Step 3: Commit**

```powershell
git add -A
git commit -m "test: add PaymentActivities unit tests"
```

---

## Task 11: Workflow Unit Tests

**Files:**
- Create: `tests/Heracles.Workflow.Tests/PaymentWorkflowTests.cs`
- Create: `tests/Heracles.Workflow.Tests/AchBatchWorkflowTests.cs`

- [ ] **Step 1: Write PaymentWorkflow tests**

`tests/Heracles.Workflow.Tests/PaymentWorkflowTests.cs`:
```csharp
using AchWorker.Activities;
using AchWorker.Workflows;
using Shared.Contracts;
using Temporalio.Testing;
using Temporalio.Worker;
using Xunit;

namespace Heracles.Workflow.Tests;

public class PaymentWorkflowTests
{
    [Fact]
    public async Task NoReturn_TimerExpires_RecordsSettlement()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();

        var settled = false;

        [Temporalio.Activities.Activity("HardAuth")]
        static Task MockHardAuth(Guid id) => Task.CompletedTask;

        [Temporalio.Activities.Activity("RecordSettlement")]
        Task MockSettle(Guid id) { settled = true; return Task.CompletedTask; }

        [Temporalio.Activities.Activity("RecordAchReturn")]
        static Task MockReturn(Guid id, AchReturnDetails d) => Task.CompletedTask;

        [Temporalio.Activities.Activity("RecordRepresentment")]
        static Task MockRepresentment(Guid id, int count) => Task.CompletedTask;

        [Temporalio.Activities.Activity("SignalPaymentAddedToBatch")]
        static Task MockSignal(Guid id, Guid fileId, bool sameDayAch) => Task.CompletedTask;

        [Temporalio.Activities.Activity("SignalBankReturn")]
        static Task MockSignalReturn(Guid id, AchReturnDetails d) => Task.CompletedTask;

        [Temporalio.Activities.Activity("VoidPaymentAuthIfExists")]
        static Task MockVoid(Guid id) => Task.CompletedTask;

        using var worker = new TemporalWorker(env.Client,
            new TemporalWorkerOptions($"test-{Guid.NewGuid()}")
                .AddWorkflow<PaymentWorkflow>()
                .AddActivity(MockHardAuth)
                .AddActivity((Func<Guid, Task>)MockSettle)
                .AddActivity(MockReturn)
                .AddActivity(MockRepresentment)
                .AddActivity(MockSignal)
                .AddActivity(MockSignalReturn)
                .AddActivity(MockVoid));

        var paymentId = Guid.NewGuid();

        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync(
                (PaymentWorkflow wf) => wf.RunAsync(paymentId, false),
                new(id: $"payment-{paymentId}", taskQueue: worker.Options.TaskQueue!));

            // Signal AddedToBatch (standard ACH — 2 banking day window)
            await handle.SignalAsync(wf => wf.AddedToBatchAsync(
                new BatchDetails(Guid.NewGuid(), IsSameDayAch: false)));

            // Time-skipping env will auto-advance past the 2-day return window
            await handle.GetResultAsync();
        });

        Assert.True(settled);
    }

    [Fact]
    public async Task R01Return_WithRepresentmentAllowed_RecordsRepresentment()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();

        var representmentRecorded = false;

        [Temporalio.Activities.Activity("HardAuth")]
        static Task MockHardAuth(Guid id) => Task.CompletedTask;

        [Temporalio.Activities.Activity("RecordSettlement")]
        static Task MockSettle(Guid id) => Task.CompletedTask;

        [Temporalio.Activities.Activity("RecordAchReturn")]
        static Task MockReturn(Guid id, AchReturnDetails d) => Task.CompletedTask;

        [Temporalio.Activities.Activity("RecordRepresentment")]
        Task MockRepresentment(Guid id, int count) { representmentRecorded = true; return Task.CompletedTask; }

        [Temporalio.Activities.Activity("SignalPaymentAddedToBatch")]
        static Task MockSignal(Guid id, Guid fileId, bool sameDayAch) => Task.CompletedTask;

        [Temporalio.Activities.Activity("SignalBankReturn")]
        static Task MockSignalReturn(Guid id, AchReturnDetails d) => Task.CompletedTask;

        [Temporalio.Activities.Activity("VoidPaymentAuthIfExists")]
        static Task MockVoid(Guid id) => Task.CompletedTask;

        using var worker = new TemporalWorker(env.Client,
            new TemporalWorkerOptions($"test-{Guid.NewGuid()}")
                .AddWorkflow<PaymentWorkflow>()
                .AddActivity(MockHardAuth)
                .AddActivity(MockSettle)
                .AddActivity(MockReturn)
                .AddActivity((Func<Guid, int, Task>)MockRepresentment)
                .AddActivity(MockSignal)
                .AddActivity(MockSignalReturn)
                .AddActivity(MockVoid));

        var paymentId = Guid.NewGuid();

        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync(
                (PaymentWorkflow wf) => wf.RunAsync(paymentId, allowsRepresentment: true),
                new(id: $"payment-{paymentId}", taskQueue: worker.Options.TaskQueue!));

            // Signal batch inclusion
            await handle.SignalAsync(wf => wf.AddedToBatchAsync(
                new BatchDetails(Guid.NewGuid(), IsSameDayAch: false)));

            // Send R01 bank return
            await handle.SignalAsync(wf => wf.BankReturnAsync(
                new AchReturnDetails(paymentId, "R01")));

            // Re-signal batch for representment cycle, then let timer expire to settle
            await Task.Delay(100); // let workflow process signal
            await handle.SignalAsync(wf => wf.AddedToBatchAsync(
                new BatchDetails(Guid.NewGuid(), IsSameDayAch: false)));

            await handle.GetResultAsync();
        });

        Assert.True(representmentRecorded);
    }

    [Fact]
    public async Task R02Return_HardFailure_WorkflowEndsWithoutRepresentment()
    {
        await using var env = await WorkflowEnvironment.StartTimeSkippingAsync();

        var representmentRecorded = false;

        [Temporalio.Activities.Activity("HardAuth")]
        static Task MockHardAuth(Guid id) => Task.CompletedTask;

        [Temporalio.Activities.Activity("RecordSettlement")]
        static Task MockSettle(Guid id) => Task.CompletedTask;

        [Temporalio.Activities.Activity("RecordAchReturn")]
        static Task MockReturn(Guid id, AchReturnDetails d) => Task.CompletedTask;

        [Temporalio.Activities.Activity("RecordRepresentment")]
        Task MockRepresentment(Guid id, int count) { representmentRecorded = true; return Task.CompletedTask; }

        [Temporalio.Activities.Activity("SignalPaymentAddedToBatch")]
        static Task MockSignal(Guid id, Guid fileId, bool sameDayAch) => Task.CompletedTask;

        [Temporalio.Activities.Activity("SignalBankReturn")]
        static Task MockSignalReturn(Guid id, AchReturnDetails d) => Task.CompletedTask;

        [Temporalio.Activities.Activity("VoidPaymentAuthIfExists")]
        static Task MockVoid(Guid id) => Task.CompletedTask;

        using var worker = new TemporalWorker(env.Client,
            new TemporalWorkerOptions($"test-{Guid.NewGuid()}")
                .AddWorkflow<PaymentWorkflow>()
                .AddActivity(MockHardAuth)
                .AddActivity(MockSettle)
                .AddActivity(MockReturn)
                .AddActivity((Func<Guid, int, Task>)MockRepresentment)
                .AddActivity(MockSignal)
                .AddActivity(MockSignalReturn)
                .AddActivity(MockVoid));

        var paymentId = Guid.NewGuid();

        await worker.ExecuteAsync(async () =>
        {
            var handle = await env.Client.StartWorkflowAsync(
                (PaymentWorkflow wf) => wf.RunAsync(paymentId, allowsRepresentment: true),
                new(id: $"payment-{paymentId}", taskQueue: worker.Options.TaskQueue!));

            await handle.SignalAsync(wf => wf.AddedToBatchAsync(
                new BatchDetails(Guid.NewGuid(), IsSameDayAch: false)));

            // R02 = account closed = hard failure, no representment
            await handle.SignalAsync(wf => wf.BankReturnAsync(
                new AchReturnDetails(paymentId, "R02")));

            await handle.GetResultAsync();
        });

        Assert.False(representmentRecorded);
    }
}
```

- [ ] **Step 2: Write AchBatchWorkflow tests**

`tests/Heracles.Workflow.Tests/AchBatchWorkflowTests.cs`:
```csharp
using AchWorker.Activities;
using AchWorker.Workflows;
using Temporalio.Exceptions;
using Temporalio.Testing;
using Temporalio.Worker;
using Xunit;

namespace Heracles.Workflow.Tests;

public class AchBatchWorkflowTests
{
    [Fact]
    public async Task NoPendingPayments_WorkflowFails()
    {
        await using var env = await WorkflowEnvironment.StartLocalAsync();

        [Temporalio.Activities.Activity("CollectPendingPayments")]
        static Task<List<Guid>> MockCollect() => Task.FromResult(new List<Guid>());

        [Temporalio.Activities.Activity("HardAuth")]
        static Task MockHardAuth(Guid id) => Task.CompletedTask;

        [Temporalio.Activities.Activity("VoidPaymentAuthIfExists")]
        static Task MockVoid(Guid id) => Task.CompletedTask;

        [Temporalio.Activities.Activity("CreateAchFile")]
        static Task<Guid> MockCreate() => Task.FromResult(Guid.NewGuid());

        [Temporalio.Activities.Activity("AddEntry")]
        static Task<Guid> MockAddEntry(Guid fileId, Guid paymentId, int count) => Task.FromResult(Guid.NewGuid());

        [Temporalio.Activities.Activity("FinalizeAchFile")]
        static Task MockFinalize(Guid fileId) => Task.CompletedTask;

        [Temporalio.Activities.Activity("TransferAchFile")]
        static Task<Guid> MockTransfer(Guid fileId) => Task.FromResult(Guid.NewGuid());

        [Temporalio.Activities.Activity("DeleteAchFileIfExists")]
        static Task MockDeleteFile(Guid fileId) => Task.CompletedTask;

        [Temporalio.Activities.Activity("RevertAchFileToDraft")]
        static Task MockRevert(Guid fileId) => Task.CompletedTask;

        [Temporalio.Activities.Activity("DeleteTransferredFileIfExists")]
        static Task MockDeleteTransfer(Guid fileId) => Task.CompletedTask;

        [Temporalio.Activities.Activity("SignalPaymentAddedToBatch")]
        static Task MockSignal(Guid id, Guid fileId, bool sameDayAch) => Task.CompletedTask;

        using var worker = new TemporalWorker(env.Client,
            new TemporalWorkerOptions($"test-{Guid.NewGuid()}")
                .AddWorkflow<AchBatchWorkflow>()
                .AddActivity(MockCollect)
                .AddActivity(MockHardAuth)
                .AddActivity(MockVoid)
                .AddActivity(MockCreate)
                .AddActivity(MockAddEntry)
                .AddActivity(MockFinalize)
                .AddActivity(MockTransfer)
                .AddActivity(MockDeleteFile)
                .AddActivity(MockRevert)
                .AddActivity(MockDeleteTransfer)
                .AddActivity(MockSignal));

        await worker.ExecuteAsync(async () =>
        {
            var ex = await Assert.ThrowsAsync<WorkflowFailedException>(() =>
                env.Client.ExecuteWorkflowAsync(
                    (AchBatchWorkflow wf) => wf.RunAsync(),
                    new(id: $"batch-{Guid.NewGuid()}", taskQueue: worker.Options.TaskQueue!)));

            Assert.Contains("No pending payments", ex.Message);
        });
    }

    [Fact]
    public async Task HappyPath_5Payments_TransfersFile()
    {
        await using var env = await WorkflowEnvironment.StartLocalAsync();

        var paymentIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var transferCalled = false;
        var signalCount = 0;

        [Temporalio.Activities.Activity("CollectPendingPayments")]
        Task<List<Guid>> MockCollect() => Task.FromResult(paymentIds);

        [Temporalio.Activities.Activity("HardAuth")]
        static Task MockHardAuth(Guid id) => Task.CompletedTask;

        [Temporalio.Activities.Activity("VoidPaymentAuthIfExists")]
        static Task MockVoid(Guid id) => Task.CompletedTask;

        [Temporalio.Activities.Activity("CreateAchFile")]
        static Task<Guid> MockCreate() => Task.FromResult(Guid.NewGuid());

        [Temporalio.Activities.Activity("AddEntry")]
        static Task<Guid> MockAddEntry(Guid fileId, Guid paymentId, int count) => Task.FromResult(Guid.NewGuid());

        [Temporalio.Activities.Activity("FinalizeAchFile")]
        static Task MockFinalize(Guid fileId) => Task.CompletedTask;

        [Temporalio.Activities.Activity("TransferAchFile")]
        Task<Guid> MockTransfer(Guid fileId) { transferCalled = true; return Task.FromResult(Guid.NewGuid()); }

        [Temporalio.Activities.Activity("DeleteAchFileIfExists")]
        static Task MockDeleteFile(Guid fileId) => Task.CompletedTask;

        [Temporalio.Activities.Activity("RevertAchFileToDraft")]
        static Task MockRevert(Guid fileId) => Task.CompletedTask;

        [Temporalio.Activities.Activity("DeleteTransferredFileIfExists")]
        static Task MockDeleteTransfer(Guid fileId) => Task.CompletedTask;

        [Temporalio.Activities.Activity("SignalPaymentAddedToBatch")]
        Task MockSignal(Guid id, Guid fileId, bool sameDayAch) { signalCount++; return Task.CompletedTask; }

        using var worker = new TemporalWorker(env.Client,
            new TemporalWorkerOptions($"test-{Guid.NewGuid()}")
                .AddWorkflow<AchBatchWorkflow>()
                .AddActivity((Func<Task<List<Guid>>>)MockCollect)
                .AddActivity(MockHardAuth)
                .AddActivity(MockVoid)
                .AddActivity(MockCreate)
                .AddActivity(MockAddEntry)
                .AddActivity(MockFinalize)
                .AddActivity((Func<Guid, Task<Guid>>)MockTransfer)
                .AddActivity(MockDeleteFile)
                .AddActivity(MockRevert)
                .AddActivity(MockDeleteTransfer)
                .AddActivity((Func<Guid, Guid, bool, Task>)MockSignal));

        await worker.ExecuteAsync(async () =>
        {
            await env.Client.ExecuteWorkflowAsync(
                (AchBatchWorkflow wf) => wf.RunAsync(),
                new(id: $"batch-{Guid.NewGuid()}", taskQueue: worker.Options.TaskQueue!));
        });

        Assert.True(transferCalled);
        Assert.Equal(5, signalCount);
    }

    [Fact]
    public async Task SftpFailure_CompensationDeletesAchFile()
    {
        await using var env = await WorkflowEnvironment.StartLocalAsync();

        var paymentIds = new List<Guid> { Guid.NewGuid() };
        var deleteFileCalled = false;
        var voidCalled = false;

        [Temporalio.Activities.Activity("CollectPendingPayments")]
        Task<List<Guid>> MockCollect() => Task.FromResult(paymentIds);

        [Temporalio.Activities.Activity("HardAuth")]
        static Task MockHardAuth(Guid id) => Task.CompletedTask;

        [Temporalio.Activities.Activity("VoidPaymentAuthIfExists")]
        Task MockVoid(Guid id) { voidCalled = true; return Task.CompletedTask; }

        [Temporalio.Activities.Activity("CreateAchFile")]
        static Task<Guid> MockCreate() => Task.FromResult(Guid.NewGuid());

        [Temporalio.Activities.Activity("AddEntry")]
        static Task<Guid> MockAddEntry(Guid fileId, Guid paymentId, int count) => Task.FromResult(Guid.NewGuid());

        [Temporalio.Activities.Activity("FinalizeAchFile")]
        static Task MockFinalize(Guid fileId) => Task.CompletedTask;

        [Temporalio.Activities.Activity("TransferAchFile")]
        static Task<Guid> MockTransfer(Guid fileId) =>
            throw new ApplicationException("SFTP connection refused");

        [Temporalio.Activities.Activity("DeleteAchFileIfExists")]
        Task MockDeleteFile(Guid fileId) { deleteFileCalled = true; return Task.CompletedTask; }

        [Temporalio.Activities.Activity("RevertAchFileToDraft")]
        static Task MockRevert(Guid fileId) => Task.CompletedTask;

        [Temporalio.Activities.Activity("DeleteTransferredFileIfExists")]
        static Task MockDeleteTransfer(Guid fileId) => Task.CompletedTask;

        [Temporalio.Activities.Activity("SignalPaymentAddedToBatch")]
        static Task MockSignal(Guid id, Guid fileId, bool sameDayAch) => Task.CompletedTask;

        using var worker = new TemporalWorker(env.Client,
            new TemporalWorkerOptions($"test-{Guid.NewGuid()}")
            {
                WorkflowFailureExceptionTypes = [typeof(Exception)]
            }
                .AddWorkflow<AchBatchWorkflow>()
                .AddActivity((Func<Task<List<Guid>>>)MockCollect)
                .AddActivity(MockHardAuth)
                .AddActivity((Func<Guid, Task>)MockVoid)
                .AddActivity(MockCreate)
                .AddActivity(MockAddEntry)
                .AddActivity(MockFinalize)
                .AddActivity((Func<Guid, Task<Guid>>)MockTransfer)
                .AddActivity((Func<Guid, Task>)MockDeleteFile)
                .AddActivity(MockRevert)
                .AddActivity(MockDeleteTransfer)
                .AddActivity(MockSignal));

        await worker.ExecuteAsync(async () =>
        {
            await Assert.ThrowsAsync<WorkflowFailedException>(() =>
                env.Client.ExecuteWorkflowAsync(
                    (AchBatchWorkflow wf) => wf.RunAsync(),
                    new(id: $"batch-{Guid.NewGuid()}", taskQueue: worker.Options.TaskQueue!)));
        });

        Assert.True(deleteFileCalled, "ACH file should be deleted during compensation");
        Assert.True(voidCalled, "Payment auth should be voided during compensation");
    }
}
```

- [ ] **Step 3: Run workflow tests**

```powershell
dotnet test tests/Heracles.Workflow.Tests -v normal
```

Expected: All tests pass.

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "test: add workflow unit tests for PaymentWorkflow and AchBatchWorkflow"
```

---

## Task 12: Integration Tests

**Files:**
- Create: `tests/Heracles.Integration.Tests/AchBatchIntegrationTests.cs`
- Create: `tests/Heracles.Integration.Tests/AchReturnIntegrationTests.cs`

- [ ] **Step 1: Write integration test infrastructure**

`tests/Heracles.Integration.Tests/IntegrationTestBase.cs`:
```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Temporalio.Client;
using Temporalio.Testing;
using Temporalio.Worker;
using AchWorker.Activities;
using AchWorker.Workflows;

namespace Heracles.Integration.Tests;

public class IntegrationTestBase : IAsyncLifetime
{
    protected WorkflowEnvironment TemporalEnv { get; private set; } = null!;
    protected HttpClient PaymentClient { get; private set; } = null!;
    protected HttpClient AchClient { get; private set; } = null!;
    protected HttpClient SftpClient { get; private set; } = null!;
    protected TemporalWorker Worker { get; private set; } = null!;

    private WebApplicationFactory<PaymentApi.Program> _paymentFactory = null!;
    private WebApplicationFactory<AchApi.Program> _achFactory = null!;
    private WebApplicationFactory<SftpApi.Program> _sftpFactory = null!;

    public async Task InitializeAsync()
    {
        TemporalEnv = await WorkflowEnvironment.StartLocalAsync();

        _paymentFactory = new WebApplicationFactory<PaymentApi.Program>();
        _achFactory = new WebApplicationFactory<AchApi.Program>();
        _sftpFactory = new WebApplicationFactory<SftpApi.Program>();

        PaymentClient = _paymentFactory.CreateClient();
        AchClient = _achFactory.CreateClient();
        SftpClient = _sftpFactory.CreateClient();

        var httpFactory = new TestHttpClientFactory(PaymentClient, AchClient, SftpClient);

        Worker = new TemporalWorker(TemporalEnv.Client,
            new TemporalWorkerOptions($"ach-worker")
                .AddWorkflow<PaymentWorkflow>()
                .AddWorkflow<AchBatchWorkflow>()
                .AddWorkflow<AchReturnWorkflow>()
                .AddAllActivities(new PaymentActivities(httpFactory, TemporalEnv.Client))
                .AddAllActivities(new AchActivities(httpFactory))
                .AddAllActivities(new SftpActivities(httpFactory)));
    }

    public async Task DisposeAsync()
    {
        Worker.Dispose();
        await TemporalEnv.DisposeAsync();
        PaymentClient.Dispose();
        AchClient.Dispose();
        SftpClient.Dispose();
        await _paymentFactory.DisposeAsync();
        await _achFactory.DisposeAsync();
        await _sftpFactory.DisposeAsync();
    }
}

// Routes named HttpClients to the correct WebApplicationFactory test client
public class TestHttpClientFactory(HttpClient payment, HttpClient ach, HttpClient sftp)
    : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => name switch
    {
        "PaymentApi" => payment,
        "AchApi" => ach,
        "SftpApi" => sftp,
        _ => throw new InvalidOperationException($"Unknown client: {name}")
    };
}
```

- [ ] **Step 2: Write AchBatch integration test**

`tests/Heracles.Integration.Tests/AchBatchIntegrationTests.cs`:
```csharp
using Shared.Contracts;
using System.Net.Http.Json;
using Temporalio.Client;
using Xunit;

namespace Heracles.Integration.Tests;

public class AchBatchIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task FullBatchFlow_5Payments_AllSignaledAddedToBatch()
    {
        await Worker.ExecuteAsync(async () =>
        {
            // 1. Create 5 payments — each starts a PaymentWorkflow
            var paymentIds = new List<Guid>();
            for (var i = 0; i < 5; i++)
            {
                var req = new CreatePaymentRequest(
                    RoutingNumber: "021000021",
                    AccountNumber: $"1234567{i:D2}",
                    AccountHolderName: $"Test User {i}",
                    Amount: 100.00m + i,
                    Type: "Credit",
                    AllowsRepresentment: true);

                var resp = await PaymentClient.PostAsJsonAsync("/payments", req);
                resp.EnsureSuccessStatusCode();
                var result = await resp.Content.ReadFromJsonAsync<PaymentCreatedResponse>();
                paymentIds.Add(result!.PaymentId);
            }

            // Give PaymentWorkflows a moment to start and complete SoftAuth
            await Task.Delay(500);

            // 2. Trigger batch workflow manually
            var batchHandle = await TemporalEnv.Client.StartWorkflowAsync(
                (AchBatchWorkflow wf) => wf.RunAsync(),
                new WorkflowOptions(id: $"ach-batch-test-{Guid.NewGuid()}", taskQueue: "ach-worker"));

            await batchHandle.GetResultAsync();

            // 3. Assert: ACH file exists and is Submitted/Finalized
            var achFiles = await AchClient.GetFromJsonAsync<List<AchFileSummary>>("/files");
            Assert.NotNull(achFiles);
            Assert.NotEmpty(achFiles);

            // 4. Assert: Transferred file exists in SftpApi
            var transfers = await SftpClient.GetFromJsonAsync<List<object>>("/files/outbound");
            Assert.NotNull(transfers);
            Assert.NotEmpty(transfers);

            // 5. Assert: Each payment now has HardAuth activity in ledger
            foreach (var id in paymentIds)
            {
                var payment = await PaymentClient.GetFromJsonAsync<PaymentDetail>($"/payments/{id}");
                Assert.NotNull(payment);
                Assert.Contains(payment.Activities, a => a.Type == "HardAuth");
            }
        });
    }

    private record PaymentCreatedResponse(Guid PaymentId);
    private record AchFileSummary(Guid FileId, string Status);
    private record PaymentDetail(Guid PaymentId, List<ActivitySummary> Activities);
    private record ActivitySummary(string Type);
}
```

- [ ] **Step 3: Write AchReturn integration test**

`tests/Heracles.Integration.Tests/AchReturnIntegrationTests.cs`:
```csharp
using Shared.Contracts;
using System.Net.Http.Json;
using System.Text;
using Temporalio.Client;
using Xunit;

namespace Heracles.Integration.Tests;

public class AchReturnIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task InboundReturnFile_SignalsPaymentWorkflow_RecordsAchReturn()
    {
        await Worker.ExecuteAsync(async () =>
        {
            // 1. Create a payment and start its workflow
            var req = new CreatePaymentRequest("021000021", "99887766", "Jane Doe",
                250.00m, "Credit", AllowsRepresentment: false);
            var resp = await PaymentClient.PostAsJsonAsync("/payments", req);
            resp.EnsureSuccessStatusCode();
            var created = await resp.Content.ReadFromJsonAsync<PaymentCreatedResponse>();
            var paymentId = created!.PaymentId;

            await Task.Delay(300);

            // 2. Run batch to include payment
            var batchHandle = await TemporalEnv.Client.StartWorkflowAsync(
                (AchBatchWorkflow wf) => wf.RunAsync(),
                new WorkflowOptions(id: $"ach-batch-return-test-{Guid.NewGuid()}", taskQueue: "ach-worker"));
            await batchHandle.GetResultAsync();

            await Task.Delay(300);

            // 3. Simulate an inbound NACHA return file with R01 for this payment
            // Type-7 addenda format: 7 + return reason + original trace embedded with paymentId
            var returnLine = $"7R01{paymentId:N}".PadRight(94);
            var returnContent = Convert.ToBase64String(Encoding.ASCII.GetBytes(returnLine + "\n"));

            var inboundResp = await SftpClient.PostAsJsonAsync("/files/inbound", new
            {
                FileName = "return_20260519.txt",
                ContentBase64 = returnContent
            });
            inboundResp.EnsureSuccessStatusCode();

            // Give AchReturnWorkflow time to process
            await Task.Delay(1000);

            // 4. Assert: Payment has AchReturn activity in ledger
            var payment = await PaymentClient.GetFromJsonAsync<PaymentDetail>($"/payments/{paymentId}");
            Assert.NotNull(payment);
            Assert.Contains(payment.Activities, a => a.Type == "AchReturn");
        });
    }

    private record PaymentCreatedResponse(Guid PaymentId);
    private record PaymentDetail(Guid PaymentId, List<ActivitySummary> Activities);
    private record ActivitySummary(string Type, string? ReferenceCode);
}
```

- [ ] **Step 4: Start dev server and run integration tests**

In a separate terminal:
```powershell
temporal server start-dev
```

Then run:
```powershell
dotnet test tests/Heracles.Integration.Tests -v normal
```

Expected: All integration tests pass. You can open http://localhost:8080 to see workflow history in real time while tests run.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "test: add integration tests for full batch flow and ACH return processing"
```

---

## Task 13: docker-compose

**Files:**
- Create: `docker-compose.yml`
- Create: `src/PaymentApi/appsettings.Docker.json`
- Create: `src/AchApi/appsettings.Docker.json`
- Create: `src/SftpApi/appsettings.Docker.json`
- Create: `src/AchWorker/appsettings.Docker.json`

- [ ] **Step 1: Write docker-compose.yml**

`docker-compose.yml`:
```yaml
version: "3.9"

services:
  temporal:
    image: temporalio/auto-setup:latest
    ports:
      - "7233:7233"
    environment:
      - DB=sqlite

  temporal-ui:
    image: temporalio/ui:latest
    ports:
      - "8080:8080"
    environment:
      - TEMPORAL_ADDRESS=temporal:7233
    depends_on:
      - temporal

  payment-api:
    build:
      context: .
      dockerfile: src/PaymentApi/Dockerfile
    ports:
      - "5001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Docker
      - Temporal__Address=temporal:7233
    depends_on:
      - temporal

  ach-api:
    build:
      context: .
      dockerfile: src/AchApi/Dockerfile
    ports:
      - "5002:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Docker
      - Temporal__Address=temporal:7233
    depends_on:
      - temporal

  sftp-api:
    build:
      context: .
      dockerfile: src/SftpApi/Dockerfile
    ports:
      - "5003:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Docker
      - Temporal__Address=temporal:7233
    depends_on:
      - temporal

  ach-worker:
    build:
      context: .
      dockerfile: src/AchWorker/Dockerfile
    environment:
      - DOTNET_ENVIRONMENT=Docker
      - Temporal__Address=temporal:7233
      - Services__PaymentApi=http://payment-api:8080
      - Services__AchApi=http://ach-api:8080
      - Services__SftpApi=http://sftp-api:8080
    depends_on:
      - temporal
      - payment-api
      - ach-api
      - sftp-api
```

- [ ] **Step 2: Add Dockerfiles**

`src/PaymentApi/Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Shared/ src/Shared/
COPY src/PaymentApi/ src/PaymentApi/
RUN dotnet publish src/PaymentApi/PaymentApi.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "PaymentApi.dll"]
```

Create identical Dockerfiles for `src/AchApi/Dockerfile`, `src/SftpApi/Dockerfile`, `src/AchWorker/Dockerfile` — same pattern, different project name.

- [ ] **Step 3: Verify docker-compose**

```powershell
docker compose config
```

Expected: Valid configuration, no errors.

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "feat: add docker-compose with Temporal server, UI, and all services"
```

---

## Running the Full System

**Dev mode (no Docker):**
```powershell
# Terminal 1 — Temporal dev server
temporal server start-dev

# Terminal 2 — PaymentApi
dotnet run --project src/PaymentApi

# Terminal 3 — AchApi
dotnet run --project src/AchApi

# Terminal 4 — SftpApi
dotnet run --project src/SftpApi

# Terminal 5 — AchWorker
dotnet run --project src/AchWorker
```

Open http://localhost:8080 — Temporal Web UI shows all workflows, schedules, and history.

**Trigger a manual batch:**
```powershell
temporal workflow start --type AchBatchWorkflow --task-queue ach-worker --workflow-id ach-batch-manual-1
```

**Submit a test payment:**
```powershell
curl -X POST http://localhost:5001/payments -H "Content-Type: application/json" -d '{
  "routingNumber": "021000021",
  "accountNumber": "123456789",
  "accountHolderName": "Alice Smith",
  "amount": 150.00,
  "type": "Credit",
  "allowsRepresentment": true
}'
```
