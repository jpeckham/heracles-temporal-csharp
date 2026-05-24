# SftpApi Clean Architecture Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor `src/SftpApi` from a flat minimal-API to Clean Architecture with Entities, Gateways, UseCases, Presenters, OutputAdapters, and Controllers layers.

**Architecture:** The six layers live as folders inside `src/SftpApi`. Each endpoint becomes one use case with an Interactor (business logic), a Presenter (shapes the HTTP response), an Output Adapter (EF Core data access), and an ASP.NET `[ApiController]`. The `Data/` folder retains only `SftpDbContext`; entity classes move to `Entities/`.

**Tech Stack:** ASP.NET Core 8, EF Core 8 (SQLite), Temporalio 1.14.1, C# 12 records, xUnit (no new tests required by spec — build must pass).

---

## File Map

### New files to create

| File | Responsibility |
|------|---------------|
| `Entities/ReceivedFile.cs` | Entity class, namespace `SftpApi.Entities` |
| `Entities/TransferredFile.cs` | Entity class, namespace `SftpApi.Entities` |
| `Gateways/ISftpFileGateway.cs` | Gateway interface |
| `UseCases/TransferOutboundFile/ITransferOutboundFileInputBoundary.cs` | Input boundary interface |
| `UseCases/TransferOutboundFile/ITransferOutboundFileOutputBoundary.cs` | Output boundary interface |
| `UseCases/TransferOutboundFile/TransferOutboundFileRequestModel.cs` | Request model record |
| `UseCases/TransferOutboundFile/TransferOutboundFileResponseModel.cs` | Response model record |
| `UseCases/TransferOutboundFile/TransferOutboundFileInteractor.cs` | Business logic |
| `UseCases/GetOutboundFiles/IGetOutboundFilesInputBoundary.cs` | Input boundary |
| `UseCases/GetOutboundFiles/IGetOutboundFilesOutputBoundary.cs` | Output boundary |
| `UseCases/GetOutboundFiles/GetOutboundFilesRequestModel.cs` | Request model (empty record) |
| `UseCases/GetOutboundFiles/GetOutboundFilesResponseModel.cs` | Response model |
| `UseCases/GetOutboundFiles/GetOutboundFilesInteractor.cs` | Business logic |
| `UseCases/DeleteTransferredFileByAchFileId/IDeleteTransferredFileByAchFileIdInputBoundary.cs` | Input boundary |
| `UseCases/DeleteTransferredFileByAchFileId/IDeleteTransferredFileByAchFileIdOutputBoundary.cs` | Output boundary |
| `UseCases/DeleteTransferredFileByAchFileId/DeleteTransferredFileByAchFileIdRequestModel.cs` | Request model |
| `UseCases/DeleteTransferredFileByAchFileId/DeleteTransferredFileByAchFileIdResponseModel.cs` | Response model |
| `UseCases/DeleteTransferredFileByAchFileId/DeleteTransferredFileByAchFileIdInteractor.cs` | Business logic |
| `UseCases/DeleteTransferredFile/IDeleteTransferredFileInputBoundary.cs` | Input boundary |
| `UseCases/DeleteTransferredFile/IDeleteTransferredFileOutputBoundary.cs` | Output boundary |
| `UseCases/DeleteTransferredFile/DeleteTransferredFileRequestModel.cs` | Request model |
| `UseCases/DeleteTransferredFile/DeleteTransferredFileResponseModel.cs` | Response model |
| `UseCases/DeleteTransferredFile/DeleteTransferredFileInteractor.cs` | Business logic |
| `UseCases/CreateInboundFile/ICreateInboundFileInputBoundary.cs` | Input boundary |
| `UseCases/CreateInboundFile/ICreateInboundFileOutputBoundary.cs` | Output boundary |
| `UseCases/CreateInboundFile/CreateInboundFileRequestModel.cs` | Request model |
| `UseCases/CreateInboundFile/CreateInboundFileResponseModel.cs` | Response model |
| `UseCases/CreateInboundFile/CreateInboundFileInteractor.cs` | Business logic (starts Temporal workflow) |
| `UseCases/GetInboundFileContent/IGetInboundFileContentInputBoundary.cs` | Input boundary |
| `UseCases/GetInboundFileContent/IGetInboundFileContentOutputBoundary.cs` | Output boundary |
| `UseCases/GetInboundFileContent/GetInboundFileContentRequestModel.cs` | Request model |
| `UseCases/GetInboundFileContent/GetInboundFileContentResponseModel.cs` | Response model |
| `UseCases/GetInboundFileContent/GetInboundFileContentInteractor.cs` | Business logic |
| `UseCases/MarkInboundFileProcessed/IMarkInboundFileProcessedInputBoundary.cs` | Input boundary |
| `UseCases/MarkInboundFileProcessed/IMarkInboundFileProcessedOutputBoundary.cs` | Output boundary |
| `UseCases/MarkInboundFileProcessed/MarkInboundFileProcessedRequestModel.cs` | Request model |
| `UseCases/MarkInboundFileProcessed/MarkInboundFileProcessedResponseModel.cs` | Response model |
| `UseCases/MarkInboundFileProcessed/MarkInboundFileProcessedInteractor.cs` | Business logic |
| `Presenters/TransferOutboundFilePresenter.cs` | Implements output boundary |
| `Presenters/TransferOutboundFileViewModel.cs` | ViewModel record |
| `Presenters/GetOutboundFilesPresenter.cs` | Implements output boundary |
| `Presenters/GetOutboundFilesViewModel.cs` | ViewModel record |
| `Presenters/DeleteTransferredFileByAchFileIdPresenter.cs` | Implements output boundary |
| `Presenters/DeleteTransferredFileByAchFileIdViewModel.cs` | ViewModel record |
| `Presenters/DeleteTransferredFilePresenter.cs` | Implements output boundary |
| `Presenters/DeleteTransferredFileViewModel.cs` | ViewModel record |
| `Presenters/CreateInboundFilePresenter.cs` | Implements output boundary |
| `Presenters/CreateInboundFileViewModel.cs` | ViewModel record |
| `Presenters/GetInboundFileContentPresenter.cs` | Implements output boundary |
| `Presenters/GetInboundFileContentViewModel.cs` | ViewModel record |
| `Presenters/MarkInboundFileProcessedPresenter.cs` | Implements output boundary |
| `Presenters/MarkInboundFileProcessedViewModel.cs` | ViewModel record |
| `OutputAdapters/SftpFileGateway.cs` | EF Core implementation of ISftpFileGateway |
| `Controllers/SftpFilesController.cs` | ASP.NET ApiController |

### Files to modify

| File | Change |
|------|--------|
| `Data/SftpDbContext.cs` | Update namespace references from `SftpApi.Data` entity types to `SftpApi.Entities` |
| `Program.cs` | Replace minimal API with controller + DI setup |

### Files to delete

| File | Reason |
|------|--------|
| `Data/ReceivedFile.cs` | Moved to `Entities/` |
| `Data/TransferredFile.cs` | Moved to `Entities/` |

---

## Task 1: Create Entity Classes

**Files:**
- Create: `src/SftpApi/Entities/ReceivedFile.cs`
- Create: `src/SftpApi/Entities/TransferredFile.cs`
- Delete: `src/SftpApi/Data/ReceivedFile.cs`
- Delete: `src/SftpApi/Data/TransferredFile.cs`

- [ ] **Step 1: Create `Entities/ReceivedFile.cs`**

```csharp
// src/SftpApi/Entities/ReceivedFile.cs
using Shared.Models;

namespace SftpApi.Entities;

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

- [ ] **Step 2: Create `Entities/TransferredFile.cs`**

```csharp
// src/SftpApi/Entities/TransferredFile.cs
namespace SftpApi.Entities;

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

- [ ] **Step 3: Update `Data/SftpDbContext.cs` to use `SftpApi.Entities`**

Replace the content of `src/SftpApi/Data/SftpDbContext.cs` with:

```csharp
using Microsoft.EntityFrameworkCore;
using SftpApi.Entities;

namespace SftpApi.Data;

public class SftpDbContext(DbContextOptions<SftpDbContext> options) : DbContext(options)
{
    public DbSet<TransferredFile> TransferredFiles => Set<TransferredFile>();
    public DbSet<ReceivedFile> ReceivedFiles => Set<ReceivedFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReceivedFile>()
            .HasKey(f => f.FileId);

        modelBuilder.Entity<TransferredFile>()
            .HasKey(f => f.FileId);
    }
}
```

- [ ] **Step 4: Delete old entity files**

Delete `src/SftpApi/Data/ReceivedFile.cs` and `src/SftpApi/Data/TransferredFile.cs`.

- [ ] **Step 5: Quick build check**

```
dotnet build src/SftpApi/SftpApi.csproj
```

Expected: Build succeeds (Program.cs still references `SftpApi.Data` entity types but those are gone — expect errors here, proceed to Task 2 which fixes them).

---

## Task 2: Define the Gateway Interface

**Files:**
- Create: `src/SftpApi/Gateways/ISftpFileGateway.cs`

- [ ] **Step 1: Create `Gateways/ISftpFileGateway.cs`**

```csharp
// src/SftpApi/Gateways/ISftpFileGateway.cs
using SftpApi.Entities;

namespace SftpApi.Gateways;

public interface ISftpFileGateway
{
    Task<TransferredFile> AddTransferredFileAsync(TransferredFile file);
    Task<IReadOnlyList<TransferredFile>> GetAllTransferredFilesAsync();
    Task DeleteTransferredFilesByAchFileIdAsync(Guid achFileId);
    Task<TransferredFile?> FindTransferredFileAsync(Guid fileId);
    Task DeleteTransferredFileAsync(TransferredFile file);

    Task<ReceivedFile> AddReceivedFileAsync(ReceivedFile file);
    Task<ReceivedFile?> FindReceivedFileAsync(Guid fileId);
    Task SaveChangesAsync();
}
```

---

## Task 3: TransferOutboundFile Use Case

**Files:**
- Create: `src/SftpApi/UseCases/TransferOutboundFile/ITransferOutboundFileInputBoundary.cs`
- Create: `src/SftpApi/UseCases/TransferOutboundFile/ITransferOutboundFileOutputBoundary.cs`
- Create: `src/SftpApi/UseCases/TransferOutboundFile/TransferOutboundFileRequestModel.cs`
- Create: `src/SftpApi/UseCases/TransferOutboundFile/TransferOutboundFileResponseModel.cs`
- Create: `src/SftpApi/UseCases/TransferOutboundFile/TransferOutboundFileInteractor.cs`

- [ ] **Step 1: Create request model**

```csharp
// src/SftpApi/UseCases/TransferOutboundFile/TransferOutboundFileRequestModel.cs
namespace SftpApi.UseCases.TransferOutboundFile;

public record TransferOutboundFileRequestModel(
    Guid AchFileId,
    string FileName,
    string ContentBase64);
```

- [ ] **Step 2: Create response model**

```csharp
// src/SftpApi/UseCases/TransferOutboundFile/TransferOutboundFileResponseModel.cs
namespace SftpApi.UseCases.TransferOutboundFile;

public record TransferOutboundFileResponseModel(Guid FileId);
```

- [ ] **Step 3: Create output boundary**

```csharp
// src/SftpApi/UseCases/TransferOutboundFile/ITransferOutboundFileOutputBoundary.cs
namespace SftpApi.UseCases.TransferOutboundFile;

public interface ITransferOutboundFileOutputBoundary
{
    void Present(TransferOutboundFileResponseModel response);
}
```

- [ ] **Step 4: Create input boundary**

```csharp
// src/SftpApi/UseCases/TransferOutboundFile/ITransferOutboundFileInputBoundary.cs
namespace SftpApi.UseCases.TransferOutboundFile;

public interface ITransferOutboundFileInputBoundary
{
    Task TransferOutboundFileAsync(
        ITransferOutboundFileOutputBoundary presenter,
        TransferOutboundFileRequestModel request);
}
```

- [ ] **Step 5: Create interactor**

```csharp
// src/SftpApi/UseCases/TransferOutboundFile/TransferOutboundFileInteractor.cs
using System.Security.Cryptography;
using SftpApi.Entities;
using SftpApi.Gateways;

namespace SftpApi.UseCases.TransferOutboundFile;

public class TransferOutboundFileInteractor(ISftpFileGateway gateway)
    : ITransferOutboundFileInputBoundary
{
    public async Task TransferOutboundFileAsync(
        ITransferOutboundFileOutputBoundary presenter,
        TransferOutboundFileRequestModel request)
    {
        var bytes = Convert.FromBase64String(request.ContentBase64);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));

        var file = new TransferredFile
        {
            AchFileId = request.AchFileId,
            FileName = request.FileName,
            FileSizeBytes = bytes.Length,
            ContentHash = hash
        };

        var saved = await gateway.AddTransferredFileAsync(file);
        presenter.Present(new TransferOutboundFileResponseModel(saved.FileId));
    }
}
```

---

## Task 4: GetOutboundFiles Use Case

**Files:**
- Create: `src/SftpApi/UseCases/GetOutboundFiles/IGetOutboundFilesInputBoundary.cs`
- Create: `src/SftpApi/UseCases/GetOutboundFiles/IGetOutboundFilesOutputBoundary.cs`
- Create: `src/SftpApi/UseCases/GetOutboundFiles/GetOutboundFilesRequestModel.cs`
- Create: `src/SftpApi/UseCases/GetOutboundFiles/GetOutboundFilesResponseModel.cs`
- Create: `src/SftpApi/UseCases/GetOutboundFiles/GetOutboundFilesInteractor.cs`

- [ ] **Step 1: Create request model**

```csharp
// src/SftpApi/UseCases/GetOutboundFiles/GetOutboundFilesRequestModel.cs
namespace SftpApi.UseCases.GetOutboundFiles;

public record GetOutboundFilesRequestModel();
```

- [ ] **Step 2: Create response model**

```csharp
// src/SftpApi/UseCases/GetOutboundFiles/GetOutboundFilesResponseModel.cs
using SftpApi.Entities;

namespace SftpApi.UseCases.GetOutboundFiles;

public record GetOutboundFilesResponseModel(IReadOnlyList<TransferredFile> Files);
```

- [ ] **Step 3: Create output boundary**

```csharp
// src/SftpApi/UseCases/GetOutboundFiles/IGetOutboundFilesOutputBoundary.cs
namespace SftpApi.UseCases.GetOutboundFiles;

public interface IGetOutboundFilesOutputBoundary
{
    void Present(GetOutboundFilesResponseModel response);
}
```

- [ ] **Step 4: Create input boundary**

```csharp
// src/SftpApi/UseCases/GetOutboundFiles/IGetOutboundFilesInputBoundary.cs
namespace SftpApi.UseCases.GetOutboundFiles;

public interface IGetOutboundFilesInputBoundary
{
    Task GetOutboundFilesAsync(
        IGetOutboundFilesOutputBoundary presenter,
        GetOutboundFilesRequestModel request);
}
```

- [ ] **Step 5: Create interactor**

```csharp
// src/SftpApi/UseCases/GetOutboundFiles/GetOutboundFilesInteractor.cs
using SftpApi.Gateways;

namespace SftpApi.UseCases.GetOutboundFiles;

public class GetOutboundFilesInteractor(ISftpFileGateway gateway)
    : IGetOutboundFilesInputBoundary
{
    public async Task GetOutboundFilesAsync(
        IGetOutboundFilesOutputBoundary presenter,
        GetOutboundFilesRequestModel request)
    {
        var files = await gateway.GetAllTransferredFilesAsync();
        presenter.Present(new GetOutboundFilesResponseModel(files));
    }
}
```

---

## Task 5: DeleteTransferredFileByAchFileId Use Case

**Files:**
- Create: `src/SftpApi/UseCases/DeleteTransferredFileByAchFileId/IDeleteTransferredFileByAchFileIdInputBoundary.cs`
- Create: `src/SftpApi/UseCases/DeleteTransferredFileByAchFileId/IDeleteTransferredFileByAchFileIdOutputBoundary.cs`
- Create: `src/SftpApi/UseCases/DeleteTransferredFileByAchFileId/DeleteTransferredFileByAchFileIdRequestModel.cs`
- Create: `src/SftpApi/UseCases/DeleteTransferredFileByAchFileId/DeleteTransferredFileByAchFileIdResponseModel.cs`
- Create: `src/SftpApi/UseCases/DeleteTransferredFileByAchFileId/DeleteTransferredFileByAchFileIdInteractor.cs`

- [ ] **Step 1: Create request model**

```csharp
// src/SftpApi/UseCases/DeleteTransferredFileByAchFileId/DeleteTransferredFileByAchFileIdRequestModel.cs
namespace SftpApi.UseCases.DeleteTransferredFileByAchFileId;

public record DeleteTransferredFileByAchFileIdRequestModel(Guid AchFileId);
```

- [ ] **Step 2: Create response model**

```csharp
// src/SftpApi/UseCases/DeleteTransferredFileByAchFileId/DeleteTransferredFileByAchFileIdResponseModel.cs
namespace SftpApi.UseCases.DeleteTransferredFileByAchFileId;

public record DeleteTransferredFileByAchFileIdResponseModel();
```

- [ ] **Step 3: Create output boundary**

```csharp
// src/SftpApi/UseCases/DeleteTransferredFileByAchFileId/IDeleteTransferredFileByAchFileIdOutputBoundary.cs
namespace SftpApi.UseCases.DeleteTransferredFileByAchFileId;

public interface IDeleteTransferredFileByAchFileIdOutputBoundary
{
    void Present(DeleteTransferredFileByAchFileIdResponseModel response);
}
```

- [ ] **Step 4: Create input boundary**

```csharp
// src/SftpApi/UseCases/DeleteTransferredFileByAchFileId/IDeleteTransferredFileByAchFileIdInputBoundary.cs
namespace SftpApi.UseCases.DeleteTransferredFileByAchFileId;

public interface IDeleteTransferredFileByAchFileIdInputBoundary
{
    Task DeleteTransferredFileByAchFileIdAsync(
        IDeleteTransferredFileByAchFileIdOutputBoundary presenter,
        DeleteTransferredFileByAchFileIdRequestModel request);
}
```

- [ ] **Step 5: Create interactor**

```csharp
// src/SftpApi/UseCases/DeleteTransferredFileByAchFileId/DeleteTransferredFileByAchFileIdInteractor.cs
using SftpApi.Gateways;

namespace SftpApi.UseCases.DeleteTransferredFileByAchFileId;

public class DeleteTransferredFileByAchFileIdInteractor(ISftpFileGateway gateway)
    : IDeleteTransferredFileByAchFileIdInputBoundary
{
    public async Task DeleteTransferredFileByAchFileIdAsync(
        IDeleteTransferredFileByAchFileIdOutputBoundary presenter,
        DeleteTransferredFileByAchFileIdRequestModel request)
    {
        await gateway.DeleteTransferredFilesByAchFileIdAsync(request.AchFileId);
        presenter.Present(new DeleteTransferredFileByAchFileIdResponseModel());
    }
}
```

---

## Task 6: DeleteTransferredFile Use Case

**Files:**
- Create: `src/SftpApi/UseCases/DeleteTransferredFile/IDeleteTransferredFileInputBoundary.cs`
- Create: `src/SftpApi/UseCases/DeleteTransferredFile/IDeleteTransferredFileOutputBoundary.cs`
- Create: `src/SftpApi/UseCases/DeleteTransferredFile/DeleteTransferredFileRequestModel.cs`
- Create: `src/SftpApi/UseCases/DeleteTransferredFile/DeleteTransferredFileResponseModel.cs`
- Create: `src/SftpApi/UseCases/DeleteTransferredFile/DeleteTransferredFileInteractor.cs`

- [ ] **Step 1: Create request model**

```csharp
// src/SftpApi/UseCases/DeleteTransferredFile/DeleteTransferredFileRequestModel.cs
namespace SftpApi.UseCases.DeleteTransferredFile;

public record DeleteTransferredFileRequestModel(Guid FileId);
```

- [ ] **Step 2: Create response model**

```csharp
// src/SftpApi/UseCases/DeleteTransferredFile/DeleteTransferredFileResponseModel.cs
namespace SftpApi.UseCases.DeleteTransferredFile;

public record DeleteTransferredFileResponseModel();
```

- [ ] **Step 3: Create output boundary**

```csharp
// src/SftpApi/UseCases/DeleteTransferredFile/IDeleteTransferredFileOutputBoundary.cs
namespace SftpApi.UseCases.DeleteTransferredFile;

public interface IDeleteTransferredFileOutputBoundary
{
    void Present(DeleteTransferredFileResponseModel response);
}
```

- [ ] **Step 4: Create input boundary**

```csharp
// src/SftpApi/UseCases/DeleteTransferredFile/IDeleteTransferredFileInputBoundary.cs
namespace SftpApi.UseCases.DeleteTransferredFile;

public interface IDeleteTransferredFileInputBoundary
{
    Task DeleteTransferredFileAsync(
        IDeleteTransferredFileOutputBoundary presenter,
        DeleteTransferredFileRequestModel request);
}
```

- [ ] **Step 5: Create interactor**

```csharp
// src/SftpApi/UseCases/DeleteTransferredFile/DeleteTransferredFileInteractor.cs
using SftpApi.Gateways;

namespace SftpApi.UseCases.DeleteTransferredFile;

public class DeleteTransferredFileInteractor(ISftpFileGateway gateway)
    : IDeleteTransferredFileInputBoundary
{
    public async Task DeleteTransferredFileAsync(
        IDeleteTransferredFileOutputBoundary presenter,
        DeleteTransferredFileRequestModel request)
    {
        var file = await gateway.FindTransferredFileAsync(request.FileId);
        if (file is not null)
        {
            await gateway.DeleteTransferredFileAsync(file);
        }
        presenter.Present(new DeleteTransferredFileResponseModel());
    }
}
```

---

## Task 7: CreateInboundFile Use Case

**Files:**
- Create: `src/SftpApi/UseCases/CreateInboundFile/ICreateInboundFileInputBoundary.cs`
- Create: `src/SftpApi/UseCases/CreateInboundFile/ICreateInboundFileOutputBoundary.cs`
- Create: `src/SftpApi/UseCases/CreateInboundFile/CreateInboundFileRequestModel.cs`
- Create: `src/SftpApi/UseCases/CreateInboundFile/CreateInboundFileResponseModel.cs`
- Create: `src/SftpApi/UseCases/CreateInboundFile/CreateInboundFileInteractor.cs`

- [ ] **Step 1: Create request model**

```csharp
// src/SftpApi/UseCases/CreateInboundFile/CreateInboundFileRequestModel.cs
namespace SftpApi.UseCases.CreateInboundFile;

public record CreateInboundFileRequestModel(string FileName, string ContentBase64);
```

- [ ] **Step 2: Create response model**

```csharp
// src/SftpApi/UseCases/CreateInboundFile/CreateInboundFileResponseModel.cs
namespace SftpApi.UseCases.CreateInboundFile;

public record CreateInboundFileResponseModel(Guid FileId);
```

- [ ] **Step 3: Create output boundary**

```csharp
// src/SftpApi/UseCases/CreateInboundFile/ICreateInboundFileOutputBoundary.cs
namespace SftpApi.UseCases.CreateInboundFile;

public interface ICreateInboundFileOutputBoundary
{
    void Present(CreateInboundFileResponseModel response);
}
```

- [ ] **Step 4: Create input boundary**

```csharp
// src/SftpApi/UseCases/CreateInboundFile/ICreateInboundFileInputBoundary.cs
namespace SftpApi.UseCases.CreateInboundFile;

public interface ICreateInboundFileInputBoundary
{
    Task CreateInboundFileAsync(
        ICreateInboundFileOutputBoundary presenter,
        CreateInboundFileRequestModel request);
}
```

- [ ] **Step 5: Create interactor**

```csharp
// src/SftpApi/UseCases/CreateInboundFile/CreateInboundFileInteractor.cs
using System.Security.Cryptography;
using SftpApi.Entities;
using SftpApi.Gateways;
using Temporalio.Client;

namespace SftpApi.UseCases.CreateInboundFile;

public class CreateInboundFileInteractor(ISftpFileGateway gateway, ITemporalClient temporal)
    : ICreateInboundFileInputBoundary
{
    public async Task CreateInboundFileAsync(
        ICreateInboundFileOutputBoundary presenter,
        CreateInboundFileRequestModel request)
    {
        var bytes = Convert.FromBase64String(request.ContentBase64);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));

        var file = new ReceivedFile
        {
            FileName = request.FileName,
            ContentBase64 = request.ContentBase64,
            ContentHash = hash
        };

        var saved = await gateway.AddReceivedFileAsync(file);

        await temporal.StartWorkflowAsync(
            "AchReturnWorkflow",
            new object[] { saved.FileId },
            new WorkflowOptions(id: $"ach-return-{saved.FileId}", taskQueue: "ach-worker"));

        presenter.Present(new CreateInboundFileResponseModel(saved.FileId));
    }
}
```

---

## Task 8: GetInboundFileContent Use Case

**Files:**
- Create: `src/SftpApi/UseCases/GetInboundFileContent/IGetInboundFileContentInputBoundary.cs`
- Create: `src/SftpApi/UseCases/GetInboundFileContent/IGetInboundFileContentOutputBoundary.cs`
- Create: `src/SftpApi/UseCases/GetInboundFileContent/GetInboundFileContentRequestModel.cs`
- Create: `src/SftpApi/UseCases/GetInboundFileContent/GetInboundFileContentResponseModel.cs`
- Create: `src/SftpApi/UseCases/GetInboundFileContent/GetInboundFileContentInteractor.cs`

- [ ] **Step 1: Create request model**

```csharp
// src/SftpApi/UseCases/GetInboundFileContent/GetInboundFileContentRequestModel.cs
namespace SftpApi.UseCases.GetInboundFileContent;

public record GetInboundFileContentRequestModel(Guid FileId);
```

- [ ] **Step 2: Create response model**

```csharp
// src/SftpApi/UseCases/GetInboundFileContent/GetInboundFileContentResponseModel.cs
namespace SftpApi.UseCases.GetInboundFileContent;

public record GetInboundFileContentResponseModel(string ContentBase64);
```

- [ ] **Step 3: Create output boundary**

```csharp
// src/SftpApi/UseCases/GetInboundFileContent/IGetInboundFileContentOutputBoundary.cs
namespace SftpApi.UseCases.GetInboundFileContent;

public interface IGetInboundFileContentOutputBoundary
{
    void Present(GetInboundFileContentResponseModel response);
    void PresentNotFound();
}
```

- [ ] **Step 4: Create input boundary**

```csharp
// src/SftpApi/UseCases/GetInboundFileContent/IGetInboundFileContentInputBoundary.cs
namespace SftpApi.UseCases.GetInboundFileContent;

public interface IGetInboundFileContentInputBoundary
{
    Task GetInboundFileContentAsync(
        IGetInboundFileContentOutputBoundary presenter,
        GetInboundFileContentRequestModel request);
}
```

- [ ] **Step 5: Create interactor**

```csharp
// src/SftpApi/UseCases/GetInboundFileContent/GetInboundFileContentInteractor.cs
using SftpApi.Gateways;

namespace SftpApi.UseCases.GetInboundFileContent;

public class GetInboundFileContentInteractor(ISftpFileGateway gateway)
    : IGetInboundFileContentInputBoundary
{
    public async Task GetInboundFileContentAsync(
        IGetInboundFileContentOutputBoundary presenter,
        GetInboundFileContentRequestModel request)
    {
        var file = await gateway.FindReceivedFileAsync(request.FileId);
        if (file is null)
        {
            presenter.PresentNotFound();
            return;
        }
        presenter.Present(new GetInboundFileContentResponseModel(file.ContentBase64));
    }
}
```

---

## Task 9: MarkInboundFileProcessed Use Case

**Files:**
- Create: `src/SftpApi/UseCases/MarkInboundFileProcessed/IMarkInboundFileProcessedInputBoundary.cs`
- Create: `src/SftpApi/UseCases/MarkInboundFileProcessed/IMarkInboundFileProcessedOutputBoundary.cs`
- Create: `src/SftpApi/UseCases/MarkInboundFileProcessed/MarkInboundFileProcessedRequestModel.cs`
- Create: `src/SftpApi/UseCases/MarkInboundFileProcessed/MarkInboundFileProcessedResponseModel.cs`
- Create: `src/SftpApi/UseCases/MarkInboundFileProcessed/MarkInboundFileProcessedInteractor.cs`

- [ ] **Step 1: Create request model**

```csharp
// src/SftpApi/UseCases/MarkInboundFileProcessed/MarkInboundFileProcessedRequestModel.cs
namespace SftpApi.UseCases.MarkInboundFileProcessed;

public record MarkInboundFileProcessedRequestModel(Guid FileId, string Status);
```

- [ ] **Step 2: Create response model**

```csharp
// src/SftpApi/UseCases/MarkInboundFileProcessed/MarkInboundFileProcessedResponseModel.cs
namespace SftpApi.UseCases.MarkInboundFileProcessed;

public record MarkInboundFileProcessedResponseModel();
```

- [ ] **Step 3: Create output boundary**

```csharp
// src/SftpApi/UseCases/MarkInboundFileProcessed/IMarkInboundFileProcessedOutputBoundary.cs
namespace SftpApi.UseCases.MarkInboundFileProcessed;

public interface IMarkInboundFileProcessedOutputBoundary
{
    void Present(MarkInboundFileProcessedResponseModel response);
    void PresentNotFound();
    void PresentInvalidStatus(string status);
}
```

- [ ] **Step 4: Create input boundary**

```csharp
// src/SftpApi/UseCases/MarkInboundFileProcessed/IMarkInboundFileProcessedInputBoundary.cs
namespace SftpApi.UseCases.MarkInboundFileProcessed;

public interface IMarkInboundFileProcessedInputBoundary
{
    Task MarkInboundFileProcessedAsync(
        IMarkInboundFileProcessedOutputBoundary presenter,
        MarkInboundFileProcessedRequestModel request);
}
```

- [ ] **Step 5: Create interactor**

```csharp
// src/SftpApi/UseCases/MarkInboundFileProcessed/MarkInboundFileProcessedInteractor.cs
using Shared.Models;
using SftpApi.Gateways;

namespace SftpApi.UseCases.MarkInboundFileProcessed;

public class MarkInboundFileProcessedInteractor(ISftpFileGateway gateway)
    : IMarkInboundFileProcessedInputBoundary
{
    public async Task MarkInboundFileProcessedAsync(
        IMarkInboundFileProcessedOutputBoundary presenter,
        MarkInboundFileProcessedRequestModel request)
    {
        var file = await gateway.FindReceivedFileAsync(request.FileId);
        if (file is null)
        {
            presenter.PresentNotFound();
            return;
        }
        if (!Enum.TryParse<ReceivedFileStatus>(request.Status, out var status))
        {
            presenter.PresentInvalidStatus(request.Status);
            return;
        }
        file.Status = status;
        await gateway.SaveChangesAsync();
        presenter.Present(new MarkInboundFileProcessedResponseModel());
    }
}
```

---

## Task 10: Create Presenters and ViewModels

**Files:**
- Create all `Presenters/*.cs` and `Presenters/*ViewModel.cs`

- [ ] **Step 1: TransferOutboundFile presenter and view model**

```csharp
// src/SftpApi/Presenters/TransferOutboundFileViewModel.cs
namespace SftpApi.Presenters;

public record TransferOutboundFileViewModel(Guid FileId);
```

```csharp
// src/SftpApi/Presenters/TransferOutboundFilePresenter.cs
using SftpApi.UseCases.TransferOutboundFile;

namespace SftpApi.Presenters;

public class TransferOutboundFilePresenter : ITransferOutboundFileOutputBoundary
{
    public TransferOutboundFileViewModel? ViewModel { get; private set; }

    public void Present(TransferOutboundFileResponseModel response)
    {
        ViewModel = new TransferOutboundFileViewModel(response.FileId);
    }
}
```

- [ ] **Step 2: GetOutboundFiles presenter and view model**

```csharp
// src/SftpApi/Presenters/GetOutboundFilesViewModel.cs
using SftpApi.Entities;

namespace SftpApi.Presenters;

public record GetOutboundFilesViewModel(IReadOnlyList<TransferredFile> Files);
```

```csharp
// src/SftpApi/Presenters/GetOutboundFilesPresenter.cs
using SftpApi.UseCases.GetOutboundFiles;

namespace SftpApi.Presenters;

public class GetOutboundFilesPresenter : IGetOutboundFilesOutputBoundary
{
    public GetOutboundFilesViewModel? ViewModel { get; private set; }

    public void Present(GetOutboundFilesResponseModel response)
    {
        ViewModel = new GetOutboundFilesViewModel(response.Files);
    }
}
```

- [ ] **Step 3: DeleteTransferredFileByAchFileId presenter and view model**

```csharp
// src/SftpApi/Presenters/DeleteTransferredFileByAchFileIdViewModel.cs
namespace SftpApi.Presenters;

public record DeleteTransferredFileByAchFileIdViewModel();
```

```csharp
// src/SftpApi/Presenters/DeleteTransferredFileByAchFileIdPresenter.cs
using SftpApi.UseCases.DeleteTransferredFileByAchFileId;

namespace SftpApi.Presenters;

public class DeleteTransferredFileByAchFileIdPresenter : IDeleteTransferredFileByAchFileIdOutputBoundary
{
    public DeleteTransferredFileByAchFileIdViewModel? ViewModel { get; private set; }

    public void Present(DeleteTransferredFileByAchFileIdResponseModel response)
    {
        ViewModel = new DeleteTransferredFileByAchFileIdViewModel();
    }
}
```

- [ ] **Step 4: DeleteTransferredFile presenter and view model**

```csharp
// src/SftpApi/Presenters/DeleteTransferredFileViewModel.cs
namespace SftpApi.Presenters;

public record DeleteTransferredFileViewModel();
```

```csharp
// src/SftpApi/Presenters/DeleteTransferredFilePresenter.cs
using SftpApi.UseCases.DeleteTransferredFile;

namespace SftpApi.Presenters;

public class DeleteTransferredFilePresenter : IDeleteTransferredFileOutputBoundary
{
    public DeleteTransferredFileViewModel? ViewModel { get; private set; }

    public void Present(DeleteTransferredFileResponseModel response)
    {
        ViewModel = new DeleteTransferredFileViewModel();
    }
}
```

- [ ] **Step 5: CreateInboundFile presenter and view model**

```csharp
// src/SftpApi/Presenters/CreateInboundFileViewModel.cs
namespace SftpApi.Presenters;

public record CreateInboundFileViewModel(Guid FileId);
```

```csharp
// src/SftpApi/Presenters/CreateInboundFilePresenter.cs
using SftpApi.UseCases.CreateInboundFile;

namespace SftpApi.Presenters;

public class CreateInboundFilePresenter : ICreateInboundFileOutputBoundary
{
    public CreateInboundFileViewModel? ViewModel { get; private set; }

    public void Present(CreateInboundFileResponseModel response)
    {
        ViewModel = new CreateInboundFileViewModel(response.FileId);
    }
}
```

- [ ] **Step 6: GetInboundFileContent presenter and view model**

```csharp
// src/SftpApi/Presenters/GetInboundFileContentViewModel.cs
namespace SftpApi.Presenters;

public record GetInboundFileContentViewModel(string ContentBase64);
```

```csharp
// src/SftpApi/Presenters/GetInboundFileContentPresenter.cs
using SftpApi.UseCases.GetInboundFileContent;

namespace SftpApi.Presenters;

public class GetInboundFileContentPresenter : IGetInboundFileContentOutputBoundary
{
    public GetInboundFileContentViewModel? ViewModel { get; private set; }
    public bool NotFound { get; private set; }

    public void Present(GetInboundFileContentResponseModel response)
    {
        ViewModel = new GetInboundFileContentViewModel(response.ContentBase64);
    }

    public void PresentNotFound()
    {
        NotFound = true;
    }
}
```

- [ ] **Step 7: MarkInboundFileProcessed presenter and view model**

```csharp
// src/SftpApi/Presenters/MarkInboundFileProcessedViewModel.cs
namespace SftpApi.Presenters;

public record MarkInboundFileProcessedViewModel();
```

```csharp
// src/SftpApi/Presenters/MarkInboundFileProcessedPresenter.cs
using SftpApi.UseCases.MarkInboundFileProcessed;

namespace SftpApi.Presenters;

public class MarkInboundFileProcessedPresenter : IMarkInboundFileProcessedOutputBoundary
{
    public MarkInboundFileProcessedViewModel? ViewModel { get; private set; }
    public bool NotFound { get; private set; }
    public string? InvalidStatus { get; private set; }

    public void Present(MarkInboundFileProcessedResponseModel response)
    {
        ViewModel = new MarkInboundFileProcessedViewModel();
    }

    public void PresentNotFound()
    {
        NotFound = true;
    }

    public void PresentInvalidStatus(string status)
    {
        InvalidStatus = status;
    }
}
```

---

## Task 11: Create the Output Adapter (SftpFileGateway)

**Files:**
- Create: `src/SftpApi/OutputAdapters/SftpFileGateway.cs`

- [ ] **Step 1: Create `OutputAdapters/SftpFileGateway.cs`**

```csharp
// src/SftpApi/OutputAdapters/SftpFileGateway.cs
using Microsoft.EntityFrameworkCore;
using SftpApi.Data;
using SftpApi.Entities;
using SftpApi.Gateways;

namespace SftpApi.OutputAdapters;

public class SftpFileGateway(SftpDbContext db) : ISftpFileGateway
{
    public async Task<TransferredFile> AddTransferredFileAsync(TransferredFile file)
    {
        db.TransferredFiles.Add(file);
        await db.SaveChangesAsync();
        return file;
    }

    public async Task<IReadOnlyList<TransferredFile>> GetAllTransferredFilesAsync()
    {
        return await db.TransferredFiles.OrderByDescending(f => f.UploadedAt).ToListAsync();
    }

    public async Task DeleteTransferredFilesByAchFileIdAsync(Guid achFileId)
    {
        var files = db.TransferredFiles.Where(f => f.AchFileId == achFileId);
        db.TransferredFiles.RemoveRange(files);
        await db.SaveChangesAsync();
    }

    public async Task<TransferredFile?> FindTransferredFileAsync(Guid fileId)
    {
        return await db.TransferredFiles.FindAsync(fileId);
    }

    public async Task DeleteTransferredFileAsync(TransferredFile file)
    {
        db.TransferredFiles.Remove(file);
        await db.SaveChangesAsync();
    }

    public async Task<ReceivedFile> AddReceivedFileAsync(ReceivedFile file)
    {
        db.ReceivedFiles.Add(file);
        await db.SaveChangesAsync();
        return file;
    }

    public async Task<ReceivedFile?> FindReceivedFileAsync(Guid fileId)
    {
        return await db.ReceivedFiles.FindAsync(fileId);
    }

    public async Task SaveChangesAsync()
    {
        await db.SaveChangesAsync();
    }
}
```

---

## Task 12: Create the Controller

**Files:**
- Create: `src/SftpApi/Controllers/SftpFilesController.cs`

- [ ] **Step 1: Create `Controllers/SftpFilesController.cs`**

```csharp
// src/SftpApi/Controllers/SftpFilesController.cs
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using SftpApi.Presenters;
using SftpApi.UseCases.CreateInboundFile;
using SftpApi.UseCases.DeleteTransferredFile;
using SftpApi.UseCases.DeleteTransferredFileByAchFileId;
using SftpApi.UseCases.GetInboundFileContent;
using SftpApi.UseCases.GetOutboundFiles;
using SftpApi.UseCases.MarkInboundFileProcessed;
using SftpApi.UseCases.TransferOutboundFile;

namespace SftpApi.Controllers;

[ApiController]
[Route("files")]
public class SftpFilesController(
    ITransferOutboundFileInputBoundary transferOutboundFile,
    IGetOutboundFilesInputBoundary getOutboundFiles,
    IDeleteTransferredFileByAchFileIdInputBoundary deleteTransferredFileByAchFileId,
    IDeleteTransferredFileInputBoundary deleteTransferredFile,
    ICreateInboundFileInputBoundary createInboundFile,
    IGetInboundFileContentInputBoundary getInboundFileContent,
    IMarkInboundFileProcessedInputBoundary markInboundFileProcessed) : ControllerBase
{
    // POST /files/outbound
    [HttpPost("outbound")]
    public async Task<IActionResult> PostOutbound([FromBody] TransferFileRequest req)
    {
        var presenter = new TransferOutboundFilePresenter();
        await transferOutboundFile.TransferOutboundFileAsync(
            presenter,
            new TransferOutboundFileRequestModel(req.AchFileId, req.FileName, req.ContentBase64));
        return Created($"/files/outbound/{presenter.ViewModel!.FileId}", presenter.ViewModel);
    }

    // GET /files/outbound
    [HttpGet("outbound")]
    public async Task<IActionResult> GetOutbound()
    {
        var presenter = new GetOutboundFilesPresenter();
        await getOutboundFiles.GetOutboundFilesAsync(presenter, new GetOutboundFilesRequestModel());
        return Ok(presenter.ViewModel!.Files);
    }

    // DELETE /files/outbound/by-ach/{achFileId}
    [HttpDelete("outbound/by-ach/{achFileId:guid}")]
    public async Task<IActionResult> DeleteOutboundByAchFileId(Guid achFileId)
    {
        var presenter = new DeleteTransferredFileByAchFileIdPresenter();
        await deleteTransferredFileByAchFileId.DeleteTransferredFileByAchFileIdAsync(
            presenter,
            new DeleteTransferredFileByAchFileIdRequestModel(achFileId));
        return Ok();
    }

    // DELETE /files/outbound/{id}
    [HttpDelete("outbound/{id:guid}")]
    public async Task<IActionResult> DeleteOutbound(Guid id)
    {
        var presenter = new DeleteTransferredFilePresenter();
        await deleteTransferredFile.DeleteTransferredFileAsync(
            presenter,
            new DeleteTransferredFileRequestModel(id));
        return Ok();
    }

    // POST /files/inbound
    [HttpPost("inbound")]
    public async Task<IActionResult> PostInbound([FromBody] CreateInboundFileHttpRequest req)
    {
        var presenter = new CreateInboundFilePresenter();
        await createInboundFile.CreateInboundFileAsync(
            presenter,
            new CreateInboundFileRequestModel(req.FileName, req.ContentBase64));
        return Created($"/files/inbound/{presenter.ViewModel!.FileId}", presenter.ViewModel);
    }

    // GET /files/inbound/{id}/content
    [HttpGet("inbound/{id:guid}/content")]
    public async Task<IActionResult> GetInboundContent(Guid id)
    {
        var presenter = new GetInboundFileContentPresenter();
        await getInboundFileContent.GetInboundFileContentAsync(
            presenter,
            new GetInboundFileContentRequestModel(id));
        if (presenter.NotFound) return NotFound();
        return Ok(presenter.ViewModel);
    }

    // PATCH /files/inbound/{id}/status
    [HttpPatch("inbound/{id:guid}/status")]
    public async Task<IActionResult> PatchInboundStatus(Guid id, [FromBody] UpdateInboundStatusHttpRequest req)
    {
        var presenter = new MarkInboundFileProcessedPresenter();
        await markInboundFileProcessed.MarkInboundFileProcessedAsync(
            presenter,
            new MarkInboundFileProcessedRequestModel(id, req.Status));
        if (presenter.NotFound) return NotFound();
        if (presenter.InvalidStatus is not null) return BadRequest($"Invalid status: {presenter.InvalidStatus}");
        return Ok();
    }
}

public record CreateInboundFileHttpRequest(string FileName, string ContentBase64);
public record UpdateInboundStatusHttpRequest(string Status);
```

---

## Task 13: Update Program.cs

**Files:**
- Modify: `src/SftpApi/Program.cs`

- [ ] **Step 1: Replace Program.cs**

```csharp
// src/SftpApi/Program.cs
using Microsoft.EntityFrameworkCore;
using SftpApi.Data;
using SftpApi.Gateways;
using SftpApi.OutputAdapters;
using SftpApi.UseCases.CreateInboundFile;
using SftpApi.UseCases.DeleteTransferredFile;
using SftpApi.UseCases.DeleteTransferredFileByAchFileId;
using SftpApi.UseCases.GetInboundFileContent;
using SftpApi.UseCases.GetOutboundFiles;
using SftpApi.UseCases.MarkInboundFileProcessed;
using SftpApi.UseCases.TransferOutboundFile;
using Temporalio.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SftpDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Sftp") ?? "Data Source=sftp.db"));

builder.Services.AddSingleton<ITemporalClient>(_ =>
    TemporalClient.ConnectAsync(new(
        builder.Configuration["Temporal:Address"] ?? "localhost:7233")).GetAwaiter().GetResult());

builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// Gateway
builder.Services.AddScoped<ISftpFileGateway, SftpFileGateway>();

// Use cases
builder.Services.AddScoped<ITransferOutboundFileInputBoundary, TransferOutboundFileInteractor>();
builder.Services.AddScoped<IGetOutboundFilesInputBoundary, GetOutboundFilesInteractor>();
builder.Services.AddScoped<IDeleteTransferredFileByAchFileIdInputBoundary, DeleteTransferredFileByAchFileIdInteractor>();
builder.Services.AddScoped<IDeleteTransferredFileInputBoundary, DeleteTransferredFileInteractor>();
builder.Services.AddScoped<ICreateInboundFileInputBoundary, CreateInboundFileInteractor>();
builder.Services.AddScoped<IGetInboundFileContentInputBoundary, GetInboundFileContentInteractor>();
builder.Services.AddScoped<IMarkInboundFileProcessedInputBoundary, MarkInboundFileProcessedInteractor>();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<SftpDbContext>().Database.EnsureCreated();

app.MapGet("/health", () => Results.Ok("healthy"));
app.MapControllers();
app.Run();

public partial class Program { }
```

---

## Task 14: Build and Fix

- [ ] **Step 1: Build**

```
dotnet build src/SftpApi/SftpApi.csproj
```

Expected: Build succeeds with 0 errors. Fix any errors before proceeding.

Common errors to watch for:
- Namespace conflicts if any using directive clashes
- Missing `using` statements in any generated file
- `InboundFileRequest` still referenced somewhere — ensure it is gone from Program.cs
- `UpdateInboundStatusRequest` still referenced — ensure it is gone from Program.cs

- [ ] **Step 2: Commit**

```bash
git add src/SftpApi/
git commit -m "refactor: apply Clean Architecture to SftpApi (Entities, Gateways, UseCases, Presenters, OutputAdapters, Controllers)"
```

---

## Self-Review Against Spec

**Spec requirements covered:**

| Requirement | Task |
|-------------|------|
| Entities folder with ReceivedFile, TransferredFile | Task 1 |
| Gateways/ISftpFileGateway | Task 2 |
| TransferOutboundFile use case (POST /files/outbound) | Task 3 |
| GetOutboundFiles (GET /files/outbound) | Task 4 |
| DeleteTransferredFileByAchFileId (DELETE /files/outbound/by-ach/{id}) | Task 5 |
| DeleteTransferredFile (DELETE /files/outbound/{id}) | Task 6 |
| CreateInboundFile (POST /files/inbound) | Task 7 |
| GetInboundFileContent (GET /files/inbound/{id}/content) | Task 8 |
| MarkInboundFileProcessed (PATCH /files/inbound/{id}/status) | Task 9 |
| Presenters and ViewModels for all use cases | Task 10 |
| OutputAdapters/SftpFileGateway | Task 11 |
| Controllers/SftpFilesController | Task 12 |
| Program.cs updated with controllers + DI | Task 13 |
| Build verification | Task 14 |
| Presenter param FIRST in input boundary methods | All tasks 3-9 |
| Data/ keeps only DbContext | Task 1 (deletes entity files) |
| Health endpoint retained | Task 13 |
| EF setup retained | Task 13 |

**Placeholder scan:** None found — all code blocks are complete.

**Type consistency check:**
- `ISftpFileGateway` defined in Task 2; used in interactors Tasks 3-9 and gateway Task 11 — method signatures match.
- ResponseModel field names in interactors match ResponseModel record constructors.
- Presenter `ViewModel` property types match ViewModel records.
- Controller uses correct interactor interface names and presenter types throughout.
