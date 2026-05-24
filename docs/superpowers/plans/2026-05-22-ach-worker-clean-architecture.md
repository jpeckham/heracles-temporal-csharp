# AchWorker Clean Architecture Refactor — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor `src/AchWorker` into Clean Architecture layers (Entities, Gateways, UseCases, InputAdapters, Presenters, OutputAdapters) without changing any Temporal workflow logic or breaking the build.

**Architecture:** Activities become thin Input Adapters that create a presenter, invoke a use case interactor, and return `presenter.ViewModel` to Temporal. Interactors hold all business logic and call gateway interfaces. Output adapters implement those interfaces via HttpClient or ITemporalClient.

**Tech Stack:** .NET 8, Temporalio 1.14.1, Temporalio.Extensions.Hosting, System.Net.Http.Json, Microsoft.Extensions.Hosting DI.

---

## File Map

### Entities (minimal — this worker is a coordinator)
- `Entities/AchReturnRecord.cs` — domain record for a parsed NACHA return line

### Gateways (interfaces)
- `Gateways/IAchFileGateway.cs`
- `Gateways/IPaymentGateway.cs`
- `Gateways/ISftpGateway.cs`
- `Gateways/IPaymentSignalGateway.cs`

### OutputAdapters
- `OutputAdapters/AchApiGateway.cs` — implements `IAchFileGateway`
- `OutputAdapters/PaymentApiGateway.cs` — implements `IPaymentGateway`
- `OutputAdapters/SftpApiGateway.cs` — implements `ISftpGateway`
- `OutputAdapters/PaymentSignalGateway.cs` — implements `IPaymentSignalGateway`

### UseCases (one folder each, 5 files each)
```
UseCases/
  CollectPendingPayments/
  HardAuthorizePayment/
  VoidPaymentAuth/
  CreateAchFile/
  AddAchEntry/
  FinalizeAchFile/
  DeleteAchFile/
  RevertAchFileToDraft/
  DeleteTransferredFile/
  TransferAchFile/
  SignalPaymentAddedToBatch/
  SignalBankReturn/
  RecordSettlement/
  RecordAchReturn/
  RecordRepresentment/
  ParseReturnFile/
  MarkReceivedFileProcessed/
```

### Presenters (one folder each)
- One `{Name}Presenter.cs` + `{Name}ViewModel.cs` per use case

### InputAdapters
- `InputAdapters/AchActivities.cs` — thin wrappers (keeps class name for Temporal)
- `InputAdapters/PaymentActivities.cs`
- `InputAdapters/SftpActivities.cs`

### Modified
- `Program.cs` — add DI registrations for gateways + interactors

### Deleted (after refactor)
- `Activities/AchActivities.cs`
- `Activities/PaymentActivities.cs`
- `Activities/SftpActivities.cs`

---

## Task 1: Create the Entity

**Files:**
- Create: `src/AchWorker/Entities/AchReturnRecord.cs`

- [ ] **Step 1: Create the entity**

```csharp
// src/AchWorker/Entities/AchReturnRecord.cs
namespace AchWorker.Entities;

public record AchReturnRecord(Guid PaymentId, string RCode);
```

- [ ] **Step 2: Verify it compiles**

```powershell
dotnet build src/AchWorker/AchWorker.csproj
```

Expected: Build succeeded (0 errors).

- [ ] **Step 3: Commit**

```bash
git add src/AchWorker/Entities/AchReturnRecord.cs
git commit -m "feat(ach-worker): add AchReturnRecord entity"
```

---

## Task 2: Define Gateway Interfaces

**Files:**
- Create: `src/AchWorker/Gateways/IAchFileGateway.cs`
- Create: `src/AchWorker/Gateways/IPaymentGateway.cs`
- Create: `src/AchWorker/Gateways/ISftpGateway.cs`
- Create: `src/AchWorker/Gateways/IPaymentSignalGateway.cs`

- [ ] **Step 1: Create `IAchFileGateway`**

```csharp
// src/AchWorker/Gateways/IAchFileGateway.cs
namespace AchWorker.Gateways;

public interface IAchFileGateway
{
    Task<Guid> CreateAsync();
    Task<Guid> AddEntryAsync(Guid fileId, Guid paymentId, string routingNumber,
        string accountNumber, string accountHolderName, decimal amount,
        string type, int representmentCount);
    Task FinalizeAsync(Guid fileId);
    Task DeleteIfExistsAsync(Guid fileId);
    Task RevertToDraftAsync(Guid fileId);
    Task<string> GetContentBase64Async(Guid fileId);
}
```

- [ ] **Step 2: Create `IPaymentGateway`**

```csharp
// src/AchWorker/Gateways/IPaymentGateway.cs
using AchWorker.Gateways.Models;

namespace AchWorker.Gateways;

public interface IPaymentGateway
{
    Task<List<Guid>> GetPendingPaymentIdsAsync();
    Task<PaymentDetail> GetDetailAsync(Guid paymentId);
    Task<bool> ExistsAsync(Guid paymentId);
    Task AddActivityAsync(Guid paymentId, string activityType, string? referenceCode = null, string? notes = null);
}
```

- [ ] **Step 3: Create gateway model `PaymentDetail`**

```csharp
// src/AchWorker/Gateways/Models/PaymentDetail.cs
namespace AchWorker.Gateways.Models;

public record PaymentDetail(
    Guid PaymentId,
    string RoutingNumber,
    string AccountNumber,
    string AccountHolderName,
    decimal Amount,
    string Type);
```

- [ ] **Step 4: Create `ISftpGateway`**

```csharp
// src/AchWorker/Gateways/ISftpGateway.cs
namespace AchWorker.Gateways;

public interface ISftpGateway
{
    Task<Guid> TransferFileAsync(Guid achFileId, string contentBase64);
    Task DeleteTransferredIfExistsAsync(Guid achFileId);
    Task<string> GetInboundContentBase64Async(Guid receivedFileId);
    Task MarkInboundProcessedAsync(Guid receivedFileId);
}
```

- [ ] **Step 5: Create `IPaymentSignalGateway`**

```csharp
// src/AchWorker/Gateways/IPaymentSignalGateway.cs
using Shared.Contracts;

namespace AchWorker.Gateways;

public interface IPaymentSignalGateway
{
    Task SignalAddedToBatchAsync(Guid paymentId, Guid achFileId, bool isSameDayAch);
    Task SignalBankReturnAsync(Guid paymentId, AchReturnDetails details);
}
```

- [ ] **Step 6: Verify build**

```powershell
dotnet build src/AchWorker/AchWorker.csproj
```

Expected: Build succeeded (0 errors).

- [ ] **Step 7: Commit**

```bash
git add src/AchWorker/Gateways/
git commit -m "feat(ach-worker): add gateway interfaces and PaymentDetail model"
```

---

## Task 3: Implement Output Adapters

**Files:**
- Create: `src/AchWorker/OutputAdapters/AchApiGateway.cs`
- Create: `src/AchWorker/OutputAdapters/PaymentApiGateway.cs`
- Create: `src/AchWorker/OutputAdapters/SftpApiGateway.cs`
- Create: `src/AchWorker/OutputAdapters/PaymentSignalGateway.cs`

- [ ] **Step 1: Create `AchApiGateway`**

```csharp
// src/AchWorker/OutputAdapters/AchApiGateway.cs
using System.Net.Http.Json;
using AchWorker.Gateways;

namespace AchWorker.OutputAdapters;

public class AchApiGateway(IHttpClientFactory httpFactory) : IAchFileGateway
{
    private HttpClient Client => httpFactory.CreateClient("AchApi");

    private record FileCreatedResponse(Guid FileId);
    private record EntryCreatedResponse(Guid EntryId);
    private record ContentResponse(string ContentBase64);

    public async Task<Guid> CreateAsync()
    {
        var resp = await Client.PostAsJsonAsync("/files", new { });
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<FileCreatedResponse>();
        return result!.FileId;
    }

    public async Task<Guid> AddEntryAsync(Guid fileId, Guid paymentId, string routingNumber,
        string accountNumber, string accountHolderName, decimal amount,
        string type, int representmentCount)
    {
        var req = new
        {
            PaymentId = paymentId,
            RoutingNumber = routingNumber,
            AccountNumber = accountNumber,
            AccountHolderName = accountHolderName,
            Amount = amount,
            Type = type,
            RepresentmentCount = representmentCount
        };
        var resp = await Client.PostAsJsonAsync($"/files/{fileId}/entries/full", req);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<EntryCreatedResponse>();
        return result!.EntryId;
    }

    public async Task FinalizeAsync(Guid fileId)
    {
        var resp = await Client.PostAsJsonAsync($"/files/{fileId}/finalize", new { });
        if (!resp.IsSuccessStatusCode)
            throw new ApplicationException($"Finalize failed: {await resp.Content.ReadAsStringAsync()}");
    }

    public async Task DeleteIfExistsAsync(Guid fileId)
    {
        var resp = await Client.DeleteAsync($"/files/{fileId}");
        if (!resp.IsSuccessStatusCode && resp.StatusCode != System.Net.HttpStatusCode.NotFound)
            throw new ApplicationException($"DeleteAchFile failed: {resp.StatusCode}");
    }

    public async Task RevertToDraftAsync(Guid fileId)
    {
        var resp = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/files/{fileId}/status")
        {
            Content = JsonContent.Create(new { Status = "Draft" })
        });
        if (!resp.IsSuccessStatusCode)
            throw new ApplicationException($"RevertAchFileToDraft failed: {resp.StatusCode}");
    }

    public async Task<string> GetContentBase64Async(Guid fileId)
    {
        var content = await Client.GetFromJsonAsync<ContentResponse>($"/files/{fileId}/content");
        if (content is null) throw new ApplicationException("ACH file content not found");
        return content.ContentBase64;
    }
}
```

- [ ] **Step 2: Create `PaymentApiGateway`**

```csharp
// src/AchWorker/OutputAdapters/PaymentApiGateway.cs
using System.Net.Http.Json;
using AchWorker.Gateways;
using AchWorker.Gateways.Models;
using Shared.Contracts;
using Shared.Models;

namespace AchWorker.OutputAdapters;

public class PaymentApiGateway(IHttpClientFactory httpFactory) : IPaymentGateway
{
    private HttpClient Client => httpFactory.CreateClient("PaymentApi");

    private record PaymentSummary(Guid PaymentId, string CurrentStatus);

    public async Task<List<Guid>> GetPendingPaymentIdsAsync()
    {
        var resp = await Client.GetFromJsonAsync<List<PaymentSummary>>("/payments?status=Pending");
        return resp?.Select(p => p.PaymentId).ToList() ?? [];
    }

    public async Task<PaymentDetail> GetDetailAsync(Guid paymentId)
    {
        var result = await Client.GetFromJsonAsync<PaymentDetail>($"/payments/{paymentId}");
        if (result is null) throw new ApplicationException($"Payment {paymentId} not found");
        return result;
    }

    public async Task<bool> ExistsAsync(Guid paymentId)
    {
        var resp = await Client.GetAsync($"/payments/{paymentId}");
        return resp.IsSuccessStatusCode;
    }

    public async Task AddActivityAsync(Guid paymentId, string activityType, string? referenceCode = null, string? notes = null)
    {
        var type = Enum.Parse<PaymentActivityType>(activityType);
        var req = new AddPaymentActivityRequest(type, ReferenceCode: referenceCode, Notes: notes);
        var resp = await Client.PostAsJsonAsync($"/payments/{paymentId}/activities", req);
        if (!resp.IsSuccessStatusCode)
            throw new ApplicationException($"AddActivity({activityType}) failed for {paymentId}: {resp.StatusCode}");
    }
}
```

- [ ] **Step 3: Create `SftpApiGateway`**

```csharp
// src/AchWorker/OutputAdapters/SftpApiGateway.cs
using System.Net.Http.Json;
using AchWorker.Gateways;

namespace AchWorker.OutputAdapters;

public class SftpApiGateway(IHttpClientFactory httpFactory) : ISftpGateway
{
    private HttpClient Client => httpFactory.CreateClient("SftpApi");

    private record ContentResponse(string ContentBase64);
    private record TransferResponse(Guid FileId);

    public async Task<Guid> TransferFileAsync(Guid achFileId, string contentBase64)
    {
        var fileName = $"ACH_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
        var req = new { AchFileId = achFileId, FileName = fileName, ContentBase64 = contentBase64 };
        var resp = await Client.PostAsJsonAsync("/files/outbound", req);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<TransferResponse>();
        return result!.FileId;
    }

    public async Task DeleteTransferredIfExistsAsync(Guid achFileId)
    {
        await Client.DeleteAsync($"/files/outbound/by-ach/{achFileId}");
    }

    public async Task<string> GetInboundContentBase64Async(Guid receivedFileId)
    {
        var content = await Client.GetFromJsonAsync<ContentResponse>($"/files/inbound/{receivedFileId}/content");
        if (content is null) return string.Empty;
        return content.ContentBase64;
    }

    public async Task MarkInboundProcessedAsync(Guid receivedFileId)
    {
        var resp = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/files/inbound/{receivedFileId}/status")
        {
            Content = JsonContent.Create(new { Status = "Processed" })
        });
        if (!resp.IsSuccessStatusCode)
            throw new ApplicationException($"MarkReceivedFileProcessed failed: {resp.StatusCode}");
    }
}
```

- [ ] **Step 4: Create `PaymentSignalGateway`**

```csharp
// src/AchWorker/OutputAdapters/PaymentSignalGateway.cs
using AchWorker.Gateways;
using Shared.Contracts;
using Temporalio.Client;

namespace AchWorker.OutputAdapters;

public class PaymentSignalGateway(ITemporalClient temporalClient) : IPaymentSignalGateway
{
    public async Task SignalAddedToBatchAsync(Guid paymentId, Guid achFileId, bool isSameDayAch)
    {
        var handle = temporalClient.GetWorkflowHandle($"payment-{paymentId}");
        await handle.SignalAsync("AddedToBatch", [new BatchDetails(achFileId, isSameDayAch)]);
    }

    public async Task SignalBankReturnAsync(Guid paymentId, AchReturnDetails details)
    {
        var handle = temporalClient.GetWorkflowHandle($"payment-{paymentId}");
        await handle.SignalAsync("BankReturn", [details]);
    }
}
```

- [ ] **Step 5: Verify build**

```powershell
dotnet build src/AchWorker/AchWorker.csproj
```

Expected: Build succeeded (0 errors).

- [ ] **Step 6: Commit**

```bash
git add src/AchWorker/OutputAdapters/
git commit -m "feat(ach-worker): add output adapter implementations"
```

---

## Task 4: Use Cases — CollectPendingPayments and HardAuthorizePayment

**Files:**
- Create: `src/AchWorker/UseCases/CollectPendingPayments/ICollectPendingPaymentsInputBoundary.cs`
- Create: `src/AchWorker/UseCases/CollectPendingPayments/ICollectPendingPaymentsOutputBoundary.cs`
- Create: `src/AchWorker/UseCases/CollectPendingPayments/CollectPendingPaymentsRequestModel.cs`
- Create: `src/AchWorker/UseCases/CollectPendingPayments/CollectPendingPaymentsResponseModel.cs`
- Create: `src/AchWorker/UseCases/CollectPendingPayments/CollectPendingPaymentsInteractor.cs`
- Create: `src/AchWorker/UseCases/HardAuthorizePayment/IHardAuthorizePaymentInputBoundary.cs`
- Create: `src/AchWorker/UseCases/HardAuthorizePayment/IHardAuthorizePaymentOutputBoundary.cs`
- Create: `src/AchWorker/UseCases/HardAuthorizePayment/HardAuthorizePaymentRequestModel.cs`
- Create: `src/AchWorker/UseCases/HardAuthorizePayment/HardAuthorizePaymentResponseModel.cs`
- Create: `src/AchWorker/UseCases/HardAuthorizePayment/HardAuthorizePaymentInteractor.cs`

- [ ] **Step 1: CollectPendingPayments — input boundary**

```csharp
// src/AchWorker/UseCases/CollectPendingPayments/ICollectPendingPaymentsInputBoundary.cs
namespace AchWorker.UseCases.CollectPendingPayments;

public interface ICollectPendingPaymentsInputBoundary
{
    Task CollectPendingPaymentsAsync(ICollectPendingPaymentsOutputBoundary presenter, CollectPendingPaymentsRequestModel request);
}
```

- [ ] **Step 2: CollectPendingPayments — output boundary**

```csharp
// src/AchWorker/UseCases/CollectPendingPayments/ICollectPendingPaymentsOutputBoundary.cs
namespace AchWorker.UseCases.CollectPendingPayments;

public interface ICollectPendingPaymentsOutputBoundary
{
    void Present(CollectPendingPaymentsResponseModel response);
}
```

- [ ] **Step 3: CollectPendingPayments — request model**

```csharp
// src/AchWorker/UseCases/CollectPendingPayments/CollectPendingPaymentsRequestModel.cs
namespace AchWorker.UseCases.CollectPendingPayments;

public record CollectPendingPaymentsRequestModel();
```

- [ ] **Step 4: CollectPendingPayments — response model**

```csharp
// src/AchWorker/UseCases/CollectPendingPayments/CollectPendingPaymentsResponseModel.cs
namespace AchWorker.UseCases.CollectPendingPayments;

public record CollectPendingPaymentsResponseModel(List<Guid> PaymentIds);
```

- [ ] **Step 5: CollectPendingPayments — interactor**

```csharp
// src/AchWorker/UseCases/CollectPendingPayments/CollectPendingPaymentsInteractor.cs
using AchWorker.Gateways;

namespace AchWorker.UseCases.CollectPendingPayments;

public class CollectPendingPaymentsInteractor(IPaymentGateway paymentGateway)
    : ICollectPendingPaymentsInputBoundary
{
    public async Task CollectPendingPaymentsAsync(
        ICollectPendingPaymentsOutputBoundary presenter,
        CollectPendingPaymentsRequestModel request)
    {
        var ids = await paymentGateway.GetPendingPaymentIdsAsync();
        presenter.Present(new CollectPendingPaymentsResponseModel(ids));
    }
}
```

- [ ] **Step 6: HardAuthorizePayment — input boundary**

```csharp
// src/AchWorker/UseCases/HardAuthorizePayment/IHardAuthorizePaymentInputBoundary.cs
namespace AchWorker.UseCases.HardAuthorizePayment;

public interface IHardAuthorizePaymentInputBoundary
{
    Task HardAuthorizePaymentAsync(IHardAuthorizePaymentOutputBoundary presenter, HardAuthorizePaymentRequestModel request);
}
```

- [ ] **Step 7: HardAuthorizePayment — output boundary**

```csharp
// src/AchWorker/UseCases/HardAuthorizePayment/IHardAuthorizePaymentOutputBoundary.cs
namespace AchWorker.UseCases.HardAuthorizePayment;

public interface IHardAuthorizePaymentOutputBoundary
{
    void Present(HardAuthorizePaymentResponseModel response);
}
```

- [ ] **Step 8: HardAuthorizePayment — request model**

```csharp
// src/AchWorker/UseCases/HardAuthorizePayment/HardAuthorizePaymentRequestModel.cs
namespace AchWorker.UseCases.HardAuthorizePayment;

public record HardAuthorizePaymentRequestModel(Guid PaymentId);
```

- [ ] **Step 9: HardAuthorizePayment — response model**

```csharp
// src/AchWorker/UseCases/HardAuthorizePayment/HardAuthorizePaymentResponseModel.cs
namespace AchWorker.UseCases.HardAuthorizePayment;

public record HardAuthorizePaymentResponseModel();
```

- [ ] **Step 10: HardAuthorizePayment — interactor**

```csharp
// src/AchWorker/UseCases/HardAuthorizePayment/HardAuthorizePaymentInteractor.cs
using AchWorker.Gateways;

namespace AchWorker.UseCases.HardAuthorizePayment;

public class HardAuthorizePaymentInteractor(IPaymentGateway paymentGateway)
    : IHardAuthorizePaymentInputBoundary
{
    public async Task HardAuthorizePaymentAsync(
        IHardAuthorizePaymentOutputBoundary presenter,
        HardAuthorizePaymentRequestModel request)
    {
        await paymentGateway.AddActivityAsync(request.PaymentId, "HardAuth");
        presenter.Present(new HardAuthorizePaymentResponseModel());
    }
}
```

- [ ] **Step 11: Verify build**

```powershell
dotnet build src/AchWorker/AchWorker.csproj
```

Expected: Build succeeded (0 errors).

- [ ] **Step 12: Commit**

```bash
git add src/AchWorker/UseCases/CollectPendingPayments/ src/AchWorker/UseCases/HardAuthorizePayment/
git commit -m "feat(ach-worker): add CollectPendingPayments and HardAuthorizePayment use cases"
```

---

## Task 5: Use Cases — VoidPaymentAuth and CreateAchFile

**Files:**
- Create: `src/AchWorker/UseCases/VoidPaymentAuth/IVoidPaymentAuthInputBoundary.cs`
- Create: `src/AchWorker/UseCases/VoidPaymentAuth/IVoidPaymentAuthOutputBoundary.cs`
- Create: `src/AchWorker/UseCases/VoidPaymentAuth/VoidPaymentAuthRequestModel.cs`
- Create: `src/AchWorker/UseCases/VoidPaymentAuth/VoidPaymentAuthResponseModel.cs`
- Create: `src/AchWorker/UseCases/VoidPaymentAuth/VoidPaymentAuthInteractor.cs`
- Create: `src/AchWorker/UseCases/CreateAchFile/ICreateAchFileInputBoundary.cs`
- Create: `src/AchWorker/UseCases/CreateAchFile/ICreateAchFileOutputBoundary.cs`
- Create: `src/AchWorker/UseCases/CreateAchFile/CreateAchFileRequestModel.cs`
- Create: `src/AchWorker/UseCases/CreateAchFile/CreateAchFileResponseModel.cs`
- Create: `src/AchWorker/UseCases/CreateAchFile/CreateAchFileInteractor.cs`

- [ ] **Step 1: VoidPaymentAuth — input boundary**

```csharp
// src/AchWorker/UseCases/VoidPaymentAuth/IVoidPaymentAuthInputBoundary.cs
namespace AchWorker.UseCases.VoidPaymentAuth;

public interface IVoidPaymentAuthInputBoundary
{
    Task VoidPaymentAuthAsync(IVoidPaymentAuthOutputBoundary presenter, VoidPaymentAuthRequestModel request);
}
```

- [ ] **Step 2: VoidPaymentAuth — output boundary**

```csharp
// src/AchWorker/UseCases/VoidPaymentAuth/IVoidPaymentAuthOutputBoundary.cs
namespace AchWorker.UseCases.VoidPaymentAuth;

public interface IVoidPaymentAuthOutputBoundary
{
    void Present(VoidPaymentAuthResponseModel response);
}
```

- [ ] **Step 3: VoidPaymentAuth — request model**

```csharp
// src/AchWorker/UseCases/VoidPaymentAuth/VoidPaymentAuthRequestModel.cs
namespace AchWorker.UseCases.VoidPaymentAuth;

public record VoidPaymentAuthRequestModel(Guid PaymentId);
```

- [ ] **Step 4: VoidPaymentAuth — response model**

```csharp
// src/AchWorker/UseCases/VoidPaymentAuth/VoidPaymentAuthResponseModel.cs
namespace AchWorker.UseCases.VoidPaymentAuth;

public record VoidPaymentAuthResponseModel();
```

- [ ] **Step 5: VoidPaymentAuth — interactor**

```csharp
// src/AchWorker/UseCases/VoidPaymentAuth/VoidPaymentAuthInteractor.cs
using AchWorker.Gateways;

namespace AchWorker.UseCases.VoidPaymentAuth;

public class VoidPaymentAuthInteractor(IPaymentGateway paymentGateway)
    : IVoidPaymentAuthInputBoundary
{
    public async Task VoidPaymentAuthAsync(
        IVoidPaymentAuthOutputBoundary presenter,
        VoidPaymentAuthRequestModel request)
    {
        var exists = await paymentGateway.ExistsAsync(request.PaymentId);
        if (exists)
            await paymentGateway.AddActivityAsync(request.PaymentId, "Void");
        presenter.Present(new VoidPaymentAuthResponseModel());
    }
}
```

- [ ] **Step 6: CreateAchFile — input boundary**

```csharp
// src/AchWorker/UseCases/CreateAchFile/ICreateAchFileInputBoundary.cs
namespace AchWorker.UseCases.CreateAchFile;

public interface ICreateAchFileInputBoundary
{
    Task CreateAchFileAsync(ICreateAchFileOutputBoundary presenter, CreateAchFileRequestModel request);
}
```

- [ ] **Step 7: CreateAchFile — output boundary**

```csharp
// src/AchWorker/UseCases/CreateAchFile/ICreateAchFileOutputBoundary.cs
namespace AchWorker.UseCases.CreateAchFile;

public interface ICreateAchFileOutputBoundary
{
    void Present(CreateAchFileResponseModel response);
}
```

- [ ] **Step 8: CreateAchFile — request model**

```csharp
// src/AchWorker/UseCases/CreateAchFile/CreateAchFileRequestModel.cs
namespace AchWorker.UseCases.CreateAchFile;

public record CreateAchFileRequestModel();
```

- [ ] **Step 9: CreateAchFile — response model**

```csharp
// src/AchWorker/UseCases/CreateAchFile/CreateAchFileResponseModel.cs
namespace AchWorker.UseCases.CreateAchFile;

public record CreateAchFileResponseModel(Guid FileId);
```

- [ ] **Step 10: CreateAchFile — interactor**

```csharp
// src/AchWorker/UseCases/CreateAchFile/CreateAchFileInteractor.cs
using AchWorker.Gateways;

namespace AchWorker.UseCases.CreateAchFile;

public class CreateAchFileInteractor(IAchFileGateway achFileGateway)
    : ICreateAchFileInputBoundary
{
    public async Task CreateAchFileAsync(
        ICreateAchFileOutputBoundary presenter,
        CreateAchFileRequestModel request)
    {
        var fileId = await achFileGateway.CreateAsync();
        presenter.Present(new CreateAchFileResponseModel(fileId));
    }
}
```

- [ ] **Step 11: Verify build**

```powershell
dotnet build src/AchWorker/AchWorker.csproj
```

Expected: Build succeeded (0 errors).

- [ ] **Step 12: Commit**

```bash
git add src/AchWorker/UseCases/VoidPaymentAuth/ src/AchWorker/UseCases/CreateAchFile/
git commit -m "feat(ach-worker): add VoidPaymentAuth and CreateAchFile use cases"
```

---

## Task 6: Use Cases — AddAchEntry, FinalizeAchFile, DeleteAchFile, RevertAchFileToDraft

**Files:**
- Create: `src/AchWorker/UseCases/AddAchEntry/{5 files}`
- Create: `src/AchWorker/UseCases/FinalizeAchFile/{5 files}`
- Create: `src/AchWorker/UseCases/DeleteAchFile/{5 files}`
- Create: `src/AchWorker/UseCases/RevertAchFileToDraft/{5 files}`

- [ ] **Step 1: AddAchEntry — all 5 files**

```csharp
// src/AchWorker/UseCases/AddAchEntry/IAddAchEntryInputBoundary.cs
namespace AchWorker.UseCases.AddAchEntry;

public interface IAddAchEntryInputBoundary
{
    Task AddAchEntryAsync(IAddAchEntryOutputBoundary presenter, AddAchEntryRequestModel request);
}
```

```csharp
// src/AchWorker/UseCases/AddAchEntry/IAddAchEntryOutputBoundary.cs
namespace AchWorker.UseCases.AddAchEntry;

public interface IAddAchEntryOutputBoundary
{
    void Present(AddAchEntryResponseModel response);
}
```

```csharp
// src/AchWorker/UseCases/AddAchEntry/AddAchEntryRequestModel.cs
namespace AchWorker.UseCases.AddAchEntry;

public record AddAchEntryRequestModel(Guid FileId, Guid PaymentId, int RepresentmentCount = 0);
```

```csharp
// src/AchWorker/UseCases/AddAchEntry/AddAchEntryResponseModel.cs
namespace AchWorker.UseCases.AddAchEntry;

public record AddAchEntryResponseModel(Guid EntryId);
```

```csharp
// src/AchWorker/UseCases/AddAchEntry/AddAchEntryInteractor.cs
using AchWorker.Gateways;

namespace AchWorker.UseCases.AddAchEntry;

public class AddAchEntryInteractor(IAchFileGateway achFileGateway, IPaymentGateway paymentGateway)
    : IAddAchEntryInputBoundary
{
    public async Task AddAchEntryAsync(
        IAddAchEntryOutputBoundary presenter,
        AddAchEntryRequestModel request)
    {
        var payment = await paymentGateway.GetDetailAsync(request.PaymentId);
        var entryId = await achFileGateway.AddEntryAsync(
            request.FileId,
            payment.PaymentId,
            payment.RoutingNumber,
            payment.AccountNumber,
            payment.AccountHolderName,
            payment.Amount,
            payment.Type,
            request.RepresentmentCount);
        presenter.Present(new AddAchEntryResponseModel(entryId));
    }
}
```

- [ ] **Step 2: FinalizeAchFile — all 5 files**

```csharp
// src/AchWorker/UseCases/FinalizeAchFile/IFinalizeAchFileInputBoundary.cs
namespace AchWorker.UseCases.FinalizeAchFile;

public interface IFinalizeAchFileInputBoundary
{
    Task FinalizeAchFileAsync(IFinalizeAchFileOutputBoundary presenter, FinalizeAchFileRequestModel request);
}
```

```csharp
// src/AchWorker/UseCases/FinalizeAchFile/IFinalizeAchFileOutputBoundary.cs
namespace AchWorker.UseCases.FinalizeAchFile;

public interface IFinalizeAchFileOutputBoundary
{
    void Present(FinalizeAchFileResponseModel response);
}
```

```csharp
// src/AchWorker/UseCases/FinalizeAchFile/FinalizeAchFileRequestModel.cs
namespace AchWorker.UseCases.FinalizeAchFile;

public record FinalizeAchFileRequestModel(Guid FileId);
```

```csharp
// src/AchWorker/UseCases/FinalizeAchFile/FinalizeAchFileResponseModel.cs
namespace AchWorker.UseCases.FinalizeAchFile;

public record FinalizeAchFileResponseModel();
```

```csharp
// src/AchWorker/UseCases/FinalizeAchFile/FinalizeAchFileInteractor.cs
using AchWorker.Gateways;

namespace AchWorker.UseCases.FinalizeAchFile;

public class FinalizeAchFileInteractor(IAchFileGateway achFileGateway)
    : IFinalizeAchFileInputBoundary
{
    public async Task FinalizeAchFileAsync(
        IFinalizeAchFileOutputBoundary presenter,
        FinalizeAchFileRequestModel request)
    {
        await achFileGateway.FinalizeAsync(request.FileId);
        presenter.Present(new FinalizeAchFileResponseModel());
    }
}
```

- [ ] **Step 3: DeleteAchFile — all 5 files**

```csharp
// src/AchWorker/UseCases/DeleteAchFile/IDeleteAchFileInputBoundary.cs
namespace AchWorker.UseCases.DeleteAchFile;

public interface IDeleteAchFileInputBoundary
{
    Task DeleteAchFileAsync(IDeleteAchFileOutputBoundary presenter, DeleteAchFileRequestModel request);
}
```

```csharp
// src/AchWorker/UseCases/DeleteAchFile/IDeleteAchFileOutputBoundary.cs
namespace AchWorker.UseCases.DeleteAchFile;

public interface IDeleteAchFileOutputBoundary
{
    void Present(DeleteAchFileResponseModel response);
}
```

```csharp
// src/AchWorker/UseCases/DeleteAchFile/DeleteAchFileRequestModel.cs
namespace AchWorker.UseCases.DeleteAchFile;

public record DeleteAchFileRequestModel(Guid FileId);
```

```csharp
// src/AchWorker/UseCases/DeleteAchFile/DeleteAchFileResponseModel.cs
namespace AchWorker.UseCases.DeleteAchFile;

public record DeleteAchFileResponseModel();
```

```csharp
// src/AchWorker/UseCases/DeleteAchFile/DeleteAchFileInteractor.cs
using AchWorker.Gateways;

namespace AchWorker.UseCases.DeleteAchFile;

public class DeleteAchFileInteractor(IAchFileGateway achFileGateway)
    : IDeleteAchFileInputBoundary
{
    public async Task DeleteAchFileAsync(
        IDeleteAchFileOutputBoundary presenter,
        DeleteAchFileRequestModel request)
    {
        await achFileGateway.DeleteIfExistsAsync(request.FileId);
        presenter.Present(new DeleteAchFileResponseModel());
    }
}
```

- [ ] **Step 4: RevertAchFileToDraft — all 5 files**

```csharp
// src/AchWorker/UseCases/RevertAchFileToDraft/IRevertAchFileToDraftInputBoundary.cs
namespace AchWorker.UseCases.RevertAchFileToDraft;

public interface IRevertAchFileToDraftInputBoundary
{
    Task RevertAchFileToDraftAsync(IRevertAchFileToDraftOutputBoundary presenter, RevertAchFileToDraftRequestModel request);
}
```

```csharp
// src/AchWorker/UseCases/RevertAchFileToDraft/IRevertAchFileToDraftOutputBoundary.cs
namespace AchWorker.UseCases.RevertAchFileToDraft;

public interface IRevertAchFileToDraftOutputBoundary
{
    void Present(RevertAchFileToDraftResponseModel response);
}
```

```csharp
// src/AchWorker/UseCases/RevertAchFileToDraft/RevertAchFileToDraftRequestModel.cs
namespace AchWorker.UseCases.RevertAchFileToDraft;

public record RevertAchFileToDraftRequestModel(Guid FileId);
```

```csharp
// src/AchWorker/UseCases/RevertAchFileToDraft/RevertAchFileToDraftResponseModel.cs
namespace AchWorker.UseCases.RevertAchFileToDraft;

public record RevertAchFileToDraftResponseModel();
```

```csharp
// src/AchWorker/UseCases/RevertAchFileToDraft/RevertAchFileToDraftInteractor.cs
using AchWorker.Gateways;

namespace AchWorker.UseCases.RevertAchFileToDraft;

public class RevertAchFileToDraftInteractor(IAchFileGateway achFileGateway)
    : IRevertAchFileToDraftInputBoundary
{
    public async Task RevertAchFileToDraftAsync(
        IRevertAchFileToDraftOutputBoundary presenter,
        RevertAchFileToDraftRequestModel request)
    {
        await achFileGateway.RevertToDraftAsync(request.FileId);
        presenter.Present(new RevertAchFileToDraftResponseModel());
    }
}
```

- [ ] **Step 5: Verify build**

```powershell
dotnet build src/AchWorker/AchWorker.csproj
```

Expected: Build succeeded (0 errors).

- [ ] **Step 6: Commit**

```bash
git add src/AchWorker/UseCases/AddAchEntry/ src/AchWorker/UseCases/FinalizeAchFile/ src/AchWorker/UseCases/DeleteAchFile/ src/AchWorker/UseCases/RevertAchFileToDraft/
git commit -m "feat(ach-worker): add AddAchEntry, FinalizeAchFile, DeleteAchFile, RevertAchFileToDraft use cases"
```

---

## Task 7: Use Cases — SFTP operations (DeleteTransferredFile, TransferAchFile, MarkReceivedFileProcessed)

**Files:** (5 files per use case × 3 use cases)

- [ ] **Step 1: DeleteTransferredFile — all 5 files**

```csharp
// src/AchWorker/UseCases/DeleteTransferredFile/IDeleteTransferredFileInputBoundary.cs
namespace AchWorker.UseCases.DeleteTransferredFile;

public interface IDeleteTransferredFileInputBoundary
{
    Task DeleteTransferredFileAsync(IDeleteTransferredFileOutputBoundary presenter, DeleteTransferredFileRequestModel request);
}
```

```csharp
// src/AchWorker/UseCases/DeleteTransferredFile/IDeleteTransferredFileOutputBoundary.cs
namespace AchWorker.UseCases.DeleteTransferredFile;

public interface IDeleteTransferredFileOutputBoundary
{
    void Present(DeleteTransferredFileResponseModel response);
}
```

```csharp
// src/AchWorker/UseCases/DeleteTransferredFile/DeleteTransferredFileRequestModel.cs
namespace AchWorker.UseCases.DeleteTransferredFile;

public record DeleteTransferredFileRequestModel(Guid AchFileId);
```

```csharp
// src/AchWorker/UseCases/DeleteTransferredFile/DeleteTransferredFileResponseModel.cs
namespace AchWorker.UseCases.DeleteTransferredFile;

public record DeleteTransferredFileResponseModel();
```

```csharp
// src/AchWorker/UseCases/DeleteTransferredFile/DeleteTransferredFileInteractor.cs
using AchWorker.Gateways;

namespace AchWorker.UseCases.DeleteTransferredFile;

public class DeleteTransferredFileInteractor(ISftpGateway sftpGateway)
    : IDeleteTransferredFileInputBoundary
{
    public async Task DeleteTransferredFileAsync(
        IDeleteTransferredFileOutputBoundary presenter,
        DeleteTransferredFileRequestModel request)
    {
        await sftpGateway.DeleteTransferredIfExistsAsync(request.AchFileId);
        presenter.Present(new DeleteTransferredFileResponseModel());
    }
}
```

- [ ] **Step 2: TransferAchFile — all 5 files**

```csharp
// src/AchWorker/UseCases/TransferAchFile/ITransferAchFileInputBoundary.cs
namespace AchWorker.UseCases.TransferAchFile;

public interface ITransferAchFileInputBoundary
{
    Task TransferAchFileAsync(ITransferAchFileOutputBoundary presenter, TransferAchFileRequestModel request);
}
```

```csharp
// src/AchWorker/UseCases/TransferAchFile/ITransferAchFileOutputBoundary.cs
namespace AchWorker.UseCases.TransferAchFile;

public interface ITransferAchFileOutputBoundary
{
    void Present(TransferAchFileResponseModel response);
}
```

```csharp
// src/AchWorker/UseCases/TransferAchFile/TransferAchFileRequestModel.cs
namespace AchWorker.UseCases.TransferAchFile;

public record TransferAchFileRequestModel(Guid AchFileId);
```

```csharp
// src/AchWorker/UseCases/TransferAchFile/TransferAchFileResponseModel.cs
namespace AchWorker.UseCases.TransferAchFile;

public record TransferAchFileResponseModel(Guid TransferredFileId);
```

```csharp
// src/AchWorker/UseCases/TransferAchFile/TransferAchFileInteractor.cs
using AchWorker.Gateways;

namespace AchWorker.UseCases.TransferAchFile;

public class TransferAchFileInteractor(IAchFileGateway achFileGateway, ISftpGateway sftpGateway)
    : ITransferAchFileInputBoundary
{
    public async Task TransferAchFileAsync(
        ITransferAchFileOutputBoundary presenter,
        TransferAchFileRequestModel request)
    {
        var contentBase64 = await achFileGateway.GetContentBase64Async(request.AchFileId);
        var transferredFileId = await sftpGateway.TransferFileAsync(request.AchFileId, contentBase64);
        presenter.Present(new TransferAchFileResponseModel(transferredFileId));
    }
}
```

- [ ] **Step 3: MarkReceivedFileProcessed — all 5 files**

```csharp
// src/AchWorker/UseCases/MarkReceivedFileProcessed/IMarkReceivedFileProcessedInputBoundary.cs
namespace AchWorker.UseCases.MarkReceivedFileProcessed;

public interface IMarkReceivedFileProcessedInputBoundary
{
    Task MarkReceivedFileProcessedAsync(IMarkReceivedFileProcessedOutputBoundary presenter, MarkReceivedFileProcessedRequestModel request);
}
```

```csharp
// src/AchWorker/UseCases/MarkReceivedFileProcessed/IMarkReceivedFileProcessedOutputBoundary.cs
namespace AchWorker.UseCases.MarkReceivedFileProcessed;

public interface IMarkReceivedFileProcessedOutputBoundary
{
    void Present(MarkReceivedFileProcessedResponseModel response);
}
```

```csharp
// src/AchWorker/UseCases/MarkReceivedFileProcessed/MarkReceivedFileProcessedRequestModel.cs
namespace AchWorker.UseCases.MarkReceivedFileProcessed;

public record MarkReceivedFileProcessedRequestModel(Guid ReceivedFileId);
```

```csharp
// src/AchWorker/UseCases/MarkReceivedFileProcessed/MarkReceivedFileProcessedResponseModel.cs
namespace AchWorker.UseCases.MarkReceivedFileProcessed;

public record MarkReceivedFileProcessedResponseModel();
```

```csharp
// src/AchWorker/UseCases/MarkReceivedFileProcessed/MarkReceivedFileProcessedInteractor.cs
using AchWorker.Gateways;

namespace AchWorker.UseCases.MarkReceivedFileProcessed;

public class MarkReceivedFileProcessedInteractor(ISftpGateway sftpGateway)
    : IMarkReceivedFileProcessedInputBoundary
{
    public async Task MarkReceivedFileProcessedAsync(
        IMarkReceivedFileProcessedOutputBoundary presenter,
        MarkReceivedFileProcessedRequestModel request)
    {
        await sftpGateway.MarkInboundProcessedAsync(request.ReceivedFileId);
        presenter.Present(new MarkReceivedFileProcessedResponseModel());
    }
}
```

- [ ] **Step 4: Verify build**

```powershell
dotnet build src/AchWorker/AchWorker.csproj
```

Expected: Build succeeded (0 errors).

- [ ] **Step 5: Commit**

```bash
git add src/AchWorker/UseCases/DeleteTransferredFile/ src/AchWorker/UseCases/TransferAchFile/ src/AchWorker/UseCases/MarkReceivedFileProcessed/
git commit -m "feat(ach-worker): add SFTP use cases"
```

---

## Task 8: Use Cases — Signals and Payment recording

**Files:** (5 files × 4 use cases: SignalPaymentAddedToBatch, SignalBankReturn, RecordSettlement, RecordAchReturn)

- [ ] **Step 1: SignalPaymentAddedToBatch — all 5 files**

```csharp
// src/AchWorker/UseCases/SignalPaymentAddedToBatch/ISignalPaymentAddedToBatchInputBoundary.cs
namespace AchWorker.UseCases.SignalPaymentAddedToBatch;

public interface ISignalPaymentAddedToBatchInputBoundary
{
    Task SignalPaymentAddedToBatchAsync(ISignalPaymentAddedToBatchOutputBoundary presenter, SignalPaymentAddedToBatchRequestModel request);
}
```

```csharp
// src/AchWorker/UseCases/SignalPaymentAddedToBatch/ISignalPaymentAddedToBatchOutputBoundary.cs
namespace AchWorker.UseCases.SignalPaymentAddedToBatch;

public interface ISignalPaymentAddedToBatchOutputBoundary
{
    void Present(SignalPaymentAddedToBatchResponseModel response);
}
```

```csharp
// src/AchWorker/UseCases/SignalPaymentAddedToBatch/SignalPaymentAddedToBatchRequestModel.cs
namespace AchWorker.UseCases.SignalPaymentAddedToBatch;

public record SignalPaymentAddedToBatchRequestModel(Guid PaymentId, Guid AchFileId, bool IsSameDayAch);
```

```csharp
// src/AchWorker/UseCases/SignalPaymentAddedToBatch/SignalPaymentAddedToBatchResponseModel.cs
namespace AchWorker.UseCases.SignalPaymentAddedToBatch;

public record SignalPaymentAddedToBatchResponseModel();
```

```csharp
// src/AchWorker/UseCases/SignalPaymentAddedToBatch/SignalPaymentAddedToBatchInteractor.cs
using AchWorker.Gateways;

namespace AchWorker.UseCases.SignalPaymentAddedToBatch;

public class SignalPaymentAddedToBatchInteractor(IPaymentSignalGateway signalGateway)
    : ISignalPaymentAddedToBatchInputBoundary
{
    public async Task SignalPaymentAddedToBatchAsync(
        ISignalPaymentAddedToBatchOutputBoundary presenter,
        SignalPaymentAddedToBatchRequestModel request)
    {
        await signalGateway.SignalAddedToBatchAsync(request.PaymentId, request.AchFileId, request.IsSameDayAch);
        presenter.Present(new SignalPaymentAddedToBatchResponseModel());
    }
}
```

- [ ] **Step 2: SignalBankReturn — all 5 files**

```csharp
// src/AchWorker/UseCases/SignalBankReturn/ISignalBankReturnInputBoundary.cs
namespace AchWorker.UseCases.SignalBankReturn;

public interface ISignalBankReturnInputBoundary
{
    Task SignalBankReturnAsync(ISignalBankReturnOutputBoundary presenter, SignalBankReturnRequestModel request);
}
```

```csharp
// src/AchWorker/UseCases/SignalBankReturn/ISignalBankReturnOutputBoundary.cs
namespace AchWorker.UseCases.SignalBankReturn;

public interface ISignalBankReturnOutputBoundary
{
    void Present(SignalBankReturnResponseModel response);
}
```

```csharp
// src/AchWorker/UseCases/SignalBankReturn/SignalBankReturnRequestModel.cs
using Shared.Contracts;

namespace AchWorker.UseCases.SignalBankReturn;

public record SignalBankReturnRequestModel(Guid PaymentId, AchReturnDetails Details);
```

```csharp
// src/AchWorker/UseCases/SignalBankReturn/SignalBankReturnResponseModel.cs
namespace AchWorker.UseCases.SignalBankReturn;

public record SignalBankReturnResponseModel();
```

```csharp
// src/AchWorker/UseCases/SignalBankReturn/SignalBankReturnInteractor.cs
using AchWorker.Gateways;

namespace AchWorker.UseCases.SignalBankReturn;

public class SignalBankReturnInteractor(IPaymentSignalGateway signalGateway)
    : ISignalBankReturnInputBoundary
{
    public async Task SignalBankReturnAsync(
        ISignalBankReturnOutputBoundary presenter,
        SignalBankReturnRequestModel request)
    {
        await signalGateway.SignalBankReturnAsync(request.PaymentId, request.Details);
        presenter.Present(new SignalBankReturnResponseModel());
    }
}
```

- [ ] **Step 3: RecordSettlement — all 5 files**

```csharp
// src/AchWorker/UseCases/RecordSettlement/IRecordSettlementInputBoundary.cs
namespace AchWorker.UseCases.RecordSettlement;

public interface IRecordSettlementInputBoundary
{
    Task RecordSettlementAsync(IRecordSettlementOutputBoundary presenter, RecordSettlementRequestModel request);
}
```

```csharp
// src/AchWorker/UseCases/RecordSettlement/IRecordSettlementOutputBoundary.cs
namespace AchWorker.UseCases.RecordSettlement;

public interface IRecordSettlementOutputBoundary
{
    void Present(RecordSettlementResponseModel response);
}
```

```csharp
// src/AchWorker/UseCases/RecordSettlement/RecordSettlementRequestModel.cs
namespace AchWorker.UseCases.RecordSettlement;

public record RecordSettlementRequestModel(Guid PaymentId);
```

```csharp
// src/AchWorker/UseCases/RecordSettlement/RecordSettlementResponseModel.cs
namespace AchWorker.UseCases.RecordSettlement;

public record RecordSettlementResponseModel();
```

```csharp
// src/AchWorker/UseCases/RecordSettlement/RecordSettlementInteractor.cs
using AchWorker.Gateways;

namespace AchWorker.UseCases.RecordSettlement;

public class RecordSettlementInteractor(IPaymentGateway paymentGateway)
    : IRecordSettlementInputBoundary
{
    public async Task RecordSettlementAsync(
        IRecordSettlementOutputBoundary presenter,
        RecordSettlementRequestModel request)
    {
        await paymentGateway.AddActivityAsync(request.PaymentId, "Settlement");
        await paymentGateway.AddActivityAsync(request.PaymentId, "PaidOut");
        presenter.Present(new RecordSettlementResponseModel());
    }
}
```

- [ ] **Step 4: RecordAchReturn — all 5 files**

```csharp
// src/AchWorker/UseCases/RecordAchReturn/IRecordAchReturnInputBoundary.cs
namespace AchWorker.UseCases.RecordAchReturn;

public interface IRecordAchReturnInputBoundary
{
    Task RecordAchReturnAsync(IRecordAchReturnOutputBoundary presenter, RecordAchReturnRequestModel request);
}
```

```csharp
// src/AchWorker/UseCases/RecordAchReturn/IRecordAchReturnOutputBoundary.cs
namespace AchWorker.UseCases.RecordAchReturn;

public interface IRecordAchReturnOutputBoundary
{
    void Present(RecordAchReturnResponseModel response);
}
```

```csharp
// src/AchWorker/UseCases/RecordAchReturn/RecordAchReturnRequestModel.cs
using Shared.Contracts;

namespace AchWorker.UseCases.RecordAchReturn;

public record RecordAchReturnRequestModel(Guid PaymentId, AchReturnDetails Details);
```

```csharp
// src/AchWorker/UseCases/RecordAchReturn/RecordAchReturnResponseModel.cs
namespace AchWorker.UseCases.RecordAchReturn;

public record RecordAchReturnResponseModel();
```

```csharp
// src/AchWorker/UseCases/RecordAchReturn/RecordAchReturnInteractor.cs
using AchWorker.Gateways;

namespace AchWorker.UseCases.RecordAchReturn;

public class RecordAchReturnInteractor(IPaymentGateway paymentGateway)
    : IRecordAchReturnInputBoundary
{
    public async Task RecordAchReturnAsync(
        IRecordAchReturnOutputBoundary presenter,
        RecordAchReturnRequestModel request)
    {
        await paymentGateway.AddActivityAsync(
            request.PaymentId,
            "AchReturn",
            referenceCode: request.Details.RCode,
            notes: request.Details.Description);
        presenter.Present(new RecordAchReturnResponseModel());
    }
}
```

- [ ] **Step 5: Verify build**

```powershell
dotnet build src/AchWorker/AchWorker.csproj
```

Expected: Build succeeded (0 errors).

- [ ] **Step 6: Commit**

```bash
git add src/AchWorker/UseCases/SignalPaymentAddedToBatch/ src/AchWorker/UseCases/SignalBankReturn/ src/AchWorker/UseCases/RecordSettlement/ src/AchWorker/UseCases/RecordAchReturn/
git commit -m "feat(ach-worker): add signal and payment-recording use cases"
```

---

## Task 9: Use Cases — RecordRepresentment and ParseReturnFile

**Files:** (5 files × 2 use cases)

- [ ] **Step 1: RecordRepresentment — all 5 files**

```csharp
// src/AchWorker/UseCases/RecordRepresentment/IRecordRepresentmentInputBoundary.cs
namespace AchWorker.UseCases.RecordRepresentment;

public interface IRecordRepresentmentInputBoundary
{
    Task RecordRepresentmentAsync(IRecordRepresentmentOutputBoundary presenter, RecordRepresentmentRequestModel request);
}
```

```csharp
// src/AchWorker/UseCases/RecordRepresentment/IRecordRepresentmentOutputBoundary.cs
namespace AchWorker.UseCases.RecordRepresentment;

public interface IRecordRepresentmentOutputBoundary
{
    void Present(RecordRepresentmentResponseModel response);
}
```

```csharp
// src/AchWorker/UseCases/RecordRepresentment/RecordRepresentmentRequestModel.cs
namespace AchWorker.UseCases.RecordRepresentment;

public record RecordRepresentmentRequestModel(Guid PaymentId, int RepresentmentCount);
```

```csharp
// src/AchWorker/UseCases/RecordRepresentment/RecordRepresentmentResponseModel.cs
namespace AchWorker.UseCases.RecordRepresentment;

public record RecordRepresentmentResponseModel();
```

```csharp
// src/AchWorker/UseCases/RecordRepresentment/RecordRepresentmentInteractor.cs
using AchWorker.Gateways;

namespace AchWorker.UseCases.RecordRepresentment;

public class RecordRepresentmentInteractor(IPaymentGateway paymentGateway)
    : IRecordRepresentmentInputBoundary
{
    public async Task RecordRepresentmentAsync(
        IRecordRepresentmentOutputBoundary presenter,
        RecordRepresentmentRequestModel request)
    {
        await paymentGateway.AddActivityAsync(
            request.PaymentId,
            "Representment",
            notes: $"Attempt {request.RepresentmentCount}");
        await paymentGateway.AddActivityAsync(
            request.PaymentId,
            "SoftAuth",
            notes: "Re-queued for representment");
        presenter.Present(new RecordRepresentmentResponseModel());
    }
}
```

- [ ] **Step 2: ParseReturnFile — all 5 files**

```csharp
// src/AchWorker/UseCases/ParseReturnFile/IParseReturnFileInputBoundary.cs
namespace AchWorker.UseCases.ParseReturnFile;

public interface IParseReturnFileInputBoundary
{
    Task ParseReturnFileAsync(IParseReturnFileOutputBoundary presenter, ParseReturnFileRequestModel request);
}
```

```csharp
// src/AchWorker/UseCases/ParseReturnFile/IParseReturnFileOutputBoundary.cs
namespace AchWorker.UseCases.ParseReturnFile;

public interface IParseReturnFileOutputBoundary
{
    void Present(ParseReturnFileResponseModel response);
}
```

```csharp
// src/AchWorker/UseCases/ParseReturnFile/ParseReturnFileRequestModel.cs
namespace AchWorker.UseCases.ParseReturnFile;

public record ParseReturnFileRequestModel(Guid ReceivedFileId);
```

```csharp
// src/AchWorker/UseCases/ParseReturnFile/ParseReturnFileResponseModel.cs
using AchWorker.Entities;

namespace AchWorker.UseCases.ParseReturnFile;

public record ParseReturnFileResponseModel(List<AchReturnRecord> Records);
```

```csharp
// src/AchWorker/UseCases/ParseReturnFile/ParseReturnFileInteractor.cs
using AchWorker.Entities;
using AchWorker.Gateways;

namespace AchWorker.UseCases.ParseReturnFile;

public class ParseReturnFileInteractor(ISftpGateway sftpGateway)
    : IParseReturnFileInputBoundary
{
    public async Task ParseReturnFileAsync(
        IParseReturnFileOutputBoundary presenter,
        ParseReturnFileRequestModel request)
    {
        var contentBase64 = await sftpGateway.GetInboundContentBase64Async(request.ReceivedFileId);
        var records = new List<AchReturnRecord>();

        if (!string.IsNullOrEmpty(contentBase64))
        {
            var nachaText = System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(contentBase64));
            foreach (var line in nachaText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Length < 94 || line[0] != '7') continue;
                var rCode = line[3..6].Trim();
                if (Guid.TryParse(line[13..49].Trim(), out var paymentId))
                    records.Add(new AchReturnRecord(paymentId, rCode));
            }
        }

        presenter.Present(new ParseReturnFileResponseModel(records));
    }
}
```

- [ ] **Step 3: Verify build**

```powershell
dotnet build src/AchWorker/AchWorker.csproj
```

Expected: Build succeeded (0 errors).

- [ ] **Step 4: Commit**

```bash
git add src/AchWorker/UseCases/RecordRepresentment/ src/AchWorker/UseCases/ParseReturnFile/
git commit -m "feat(ach-worker): add RecordRepresentment and ParseReturnFile use cases"
```

---

## Task 10: Create Presenters

One presenter + view model per use case. Presenter implements the output boundary interface and exposes a nullable `ViewModel` property.

**Files:** (2 files × 17 use cases = 34 files under `Presenters/`)

- [ ] **Step 1: Create all 17 presenters**

Create each presenter file using this pattern. The `Present` method sets `ViewModel` and the input adapter reads it after calling the use case.

```csharp
// src/AchWorker/Presenters/CollectPendingPayments/CollectPendingPaymentsViewModel.cs
namespace AchWorker.Presenters.CollectPendingPayments;

public record CollectPendingPaymentsViewModel(List<Guid> PaymentIds);
```

```csharp
// src/AchWorker/Presenters/CollectPendingPayments/CollectPendingPaymentsPresenter.cs
using AchWorker.UseCases.CollectPendingPayments;

namespace AchWorker.Presenters.CollectPendingPayments;

public class CollectPendingPaymentsPresenter : ICollectPendingPaymentsOutputBoundary
{
    public CollectPendingPaymentsViewModel? ViewModel { get; private set; }

    public void Present(CollectPendingPaymentsResponseModel response)
    {
        ViewModel = new CollectPendingPaymentsViewModel(response.PaymentIds);
    }
}
```

```csharp
// src/AchWorker/Presenters/HardAuthorizePayment/HardAuthorizePaymentViewModel.cs
namespace AchWorker.Presenters.HardAuthorizePayment;

public record HardAuthorizePaymentViewModel();
```

```csharp
// src/AchWorker/Presenters/HardAuthorizePayment/HardAuthorizePaymentPresenter.cs
using AchWorker.UseCases.HardAuthorizePayment;

namespace AchWorker.Presenters.HardAuthorizePayment;

public class HardAuthorizePaymentPresenter : IHardAuthorizePaymentOutputBoundary
{
    public HardAuthorizePaymentViewModel? ViewModel { get; private set; }

    public void Present(HardAuthorizePaymentResponseModel response)
    {
        ViewModel = new HardAuthorizePaymentViewModel();
    }
}
```

```csharp
// src/AchWorker/Presenters/VoidPaymentAuth/VoidPaymentAuthViewModel.cs
namespace AchWorker.Presenters.VoidPaymentAuth;

public record VoidPaymentAuthViewModel();
```

```csharp
// src/AchWorker/Presenters/VoidPaymentAuth/VoidPaymentAuthPresenter.cs
using AchWorker.UseCases.VoidPaymentAuth;

namespace AchWorker.Presenters.VoidPaymentAuth;

public class VoidPaymentAuthPresenter : IVoidPaymentAuthOutputBoundary
{
    public VoidPaymentAuthViewModel? ViewModel { get; private set; }

    public void Present(VoidPaymentAuthResponseModel response)
    {
        ViewModel = new VoidPaymentAuthViewModel();
    }
}
```

```csharp
// src/AchWorker/Presenters/CreateAchFile/CreateAchFileViewModel.cs
namespace AchWorker.Presenters.CreateAchFile;

public record CreateAchFileViewModel(Guid FileId);
```

```csharp
// src/AchWorker/Presenters/CreateAchFile/CreateAchFilePresenter.cs
using AchWorker.UseCases.CreateAchFile;

namespace AchWorker.Presenters.CreateAchFile;

public class CreateAchFilePresenter : ICreateAchFileOutputBoundary
{
    public CreateAchFileViewModel? ViewModel { get; private set; }

    public void Present(CreateAchFileResponseModel response)
    {
        ViewModel = new CreateAchFileViewModel(response.FileId);
    }
}
```

```csharp
// src/AchWorker/Presenters/AddAchEntry/AddAchEntryViewModel.cs
namespace AchWorker.Presenters.AddAchEntry;

public record AddAchEntryViewModel(Guid EntryId);
```

```csharp
// src/AchWorker/Presenters/AddAchEntry/AddAchEntryPresenter.cs
using AchWorker.UseCases.AddAchEntry;

namespace AchWorker.Presenters.AddAchEntry;

public class AddAchEntryPresenter : IAddAchEntryOutputBoundary
{
    public AddAchEntryViewModel? ViewModel { get; private set; }

    public void Present(AddAchEntryResponseModel response)
    {
        ViewModel = new AddAchEntryViewModel(response.EntryId);
    }
}
```

```csharp
// src/AchWorker/Presenters/FinalizeAchFile/FinalizeAchFileViewModel.cs
namespace AchWorker.Presenters.FinalizeAchFile;

public record FinalizeAchFileViewModel();
```

```csharp
// src/AchWorker/Presenters/FinalizeAchFile/FinalizeAchFilePresenter.cs
using AchWorker.UseCases.FinalizeAchFile;

namespace AchWorker.Presenters.FinalizeAchFile;

public class FinalizeAchFilePresenter : IFinalizeAchFileOutputBoundary
{
    public FinalizeAchFileViewModel? ViewModel { get; private set; }

    public void Present(FinalizeAchFileResponseModel response)
    {
        ViewModel = new FinalizeAchFileViewModel();
    }
}
```

```csharp
// src/AchWorker/Presenters/DeleteAchFile/DeleteAchFileViewModel.cs
namespace AchWorker.Presenters.DeleteAchFile;

public record DeleteAchFileViewModel();
```

```csharp
// src/AchWorker/Presenters/DeleteAchFile/DeleteAchFilePresenter.cs
using AchWorker.UseCases.DeleteAchFile;

namespace AchWorker.Presenters.DeleteAchFile;

public class DeleteAchFilePresenter : IDeleteAchFileOutputBoundary
{
    public DeleteAchFileViewModel? ViewModel { get; private set; }

    public void Present(DeleteAchFileResponseModel response)
    {
        ViewModel = new DeleteAchFileViewModel();
    }
}
```

```csharp
// src/AchWorker/Presenters/RevertAchFileToDraft/RevertAchFileToDraftViewModel.cs
namespace AchWorker.Presenters.RevertAchFileToDraft;

public record RevertAchFileToDraftViewModel();
```

```csharp
// src/AchWorker/Presenters/RevertAchFileToDraft/RevertAchFileToDraftPresenter.cs
using AchWorker.UseCases.RevertAchFileToDraft;

namespace AchWorker.Presenters.RevertAchFileToDraft;

public class RevertAchFileToDraftPresenter : IRevertAchFileToDraftOutputBoundary
{
    public RevertAchFileToDraftViewModel? ViewModel { get; private set; }

    public void Present(RevertAchFileToDraftResponseModel response)
    {
        ViewModel = new RevertAchFileToDraftViewModel();
    }
}
```

```csharp
// src/AchWorker/Presenters/DeleteTransferredFile/DeleteTransferredFileViewModel.cs
namespace AchWorker.Presenters.DeleteTransferredFile;

public record DeleteTransferredFileViewModel();
```

```csharp
// src/AchWorker/Presenters/DeleteTransferredFile/DeleteTransferredFilePresenter.cs
using AchWorker.UseCases.DeleteTransferredFile;

namespace AchWorker.Presenters.DeleteTransferredFile;

public class DeleteTransferredFilePresenter : IDeleteTransferredFileOutputBoundary
{
    public DeleteTransferredFileViewModel? ViewModel { get; private set; }

    public void Present(DeleteTransferredFileResponseModel response)
    {
        ViewModel = new DeleteTransferredFileViewModel();
    }
}
```

```csharp
// src/AchWorker/Presenters/TransferAchFile/TransferAchFileViewModel.cs
namespace AchWorker.Presenters.TransferAchFile;

public record TransferAchFileViewModel(Guid TransferredFileId);
```

```csharp
// src/AchWorker/Presenters/TransferAchFile/TransferAchFilePresenter.cs
using AchWorker.UseCases.TransferAchFile;

namespace AchWorker.Presenters.TransferAchFile;

public class TransferAchFilePresenter : ITransferAchFileOutputBoundary
{
    public TransferAchFileViewModel? ViewModel { get; private set; }

    public void Present(TransferAchFileResponseModel response)
    {
        ViewModel = new TransferAchFileViewModel(response.TransferredFileId);
    }
}
```

```csharp
// src/AchWorker/Presenters/MarkReceivedFileProcessed/MarkReceivedFileProcessedViewModel.cs
namespace AchWorker.Presenters.MarkReceivedFileProcessed;

public record MarkReceivedFileProcessedViewModel();
```

```csharp
// src/AchWorker/Presenters/MarkReceivedFileProcessed/MarkReceivedFileProcessedPresenter.cs
using AchWorker.UseCases.MarkReceivedFileProcessed;

namespace AchWorker.Presenters.MarkReceivedFileProcessed;

public class MarkReceivedFileProcessedPresenter : IMarkReceivedFileProcessedOutputBoundary
{
    public MarkReceivedFileProcessedViewModel? ViewModel { get; private set; }

    public void Present(MarkReceivedFileProcessedResponseModel response)
    {
        ViewModel = new MarkReceivedFileProcessedViewModel();
    }
}
```

```csharp
// src/AchWorker/Presenters/SignalPaymentAddedToBatch/SignalPaymentAddedToBatchViewModel.cs
namespace AchWorker.Presenters.SignalPaymentAddedToBatch;

public record SignalPaymentAddedToBatchViewModel();
```

```csharp
// src/AchWorker/Presenters/SignalPaymentAddedToBatch/SignalPaymentAddedToBatchPresenter.cs
using AchWorker.UseCases.SignalPaymentAddedToBatch;

namespace AchWorker.Presenters.SignalPaymentAddedToBatch;

public class SignalPaymentAddedToBatchPresenter : ISignalPaymentAddedToBatchOutputBoundary
{
    public SignalPaymentAddedToBatchViewModel? ViewModel { get; private set; }

    public void Present(SignalPaymentAddedToBatchResponseModel response)
    {
        ViewModel = new SignalPaymentAddedToBatchViewModel();
    }
}
```

```csharp
// src/AchWorker/Presenters/SignalBankReturn/SignalBankReturnViewModel.cs
namespace AchWorker.Presenters.SignalBankReturn;

public record SignalBankReturnViewModel();
```

```csharp
// src/AchWorker/Presenters/SignalBankReturn/SignalBankReturnPresenter.cs
using AchWorker.UseCases.SignalBankReturn;

namespace AchWorker.Presenters.SignalBankReturn;

public class SignalBankReturnPresenter : ISignalBankReturnOutputBoundary
{
    public SignalBankReturnViewModel? ViewModel { get; private set; }

    public void Present(SignalBankReturnResponseModel response)
    {
        ViewModel = new SignalBankReturnViewModel();
    }
}
```

```csharp
// src/AchWorker/Presenters/RecordSettlement/RecordSettlementViewModel.cs
namespace AchWorker.Presenters.RecordSettlement;

public record RecordSettlementViewModel();
```

```csharp
// src/AchWorker/Presenters/RecordSettlement/RecordSettlementPresenter.cs
using AchWorker.UseCases.RecordSettlement;

namespace AchWorker.Presenters.RecordSettlement;

public class RecordSettlementPresenter : IRecordSettlementOutputBoundary
{
    public RecordSettlementViewModel? ViewModel { get; private set; }

    public void Present(RecordSettlementResponseModel response)
    {
        ViewModel = new RecordSettlementViewModel();
    }
}
```

```csharp
// src/AchWorker/Presenters/RecordAchReturn/RecordAchReturnViewModel.cs
namespace AchWorker.Presenters.RecordAchReturn;

public record RecordAchReturnViewModel();
```

```csharp
// src/AchWorker/Presenters/RecordAchReturn/RecordAchReturnPresenter.cs
using AchWorker.UseCases.RecordAchReturn;

namespace AchWorker.Presenters.RecordAchReturn;

public class RecordAchReturnPresenter : IRecordAchReturnOutputBoundary
{
    public RecordAchReturnViewModel? ViewModel { get; private set; }

    public void Present(RecordAchReturnResponseModel response)
    {
        ViewModel = new RecordAchReturnViewModel();
    }
}
```

```csharp
// src/AchWorker/Presenters/RecordRepresentment/RecordRepresentmentViewModel.cs
namespace AchWorker.Presenters.RecordRepresentment;

public record RecordRepresentmentViewModel();
```

```csharp
// src/AchWorker/Presenters/RecordRepresentment/RecordRepresentmentPresenter.cs
using AchWorker.UseCases.RecordRepresentment;

namespace AchWorker.Presenters.RecordRepresentment;

public class RecordRepresentmentPresenter : IRecordRepresentmentOutputBoundary
{
    public RecordRepresentmentViewModel? ViewModel { get; private set; }

    public void Present(RecordRepresentmentResponseModel response)
    {
        ViewModel = new RecordRepresentmentViewModel();
    }
}
```

```csharp
// src/AchWorker/Presenters/ParseReturnFile/ParseReturnFileViewModel.cs
using AchWorker.Entities;

namespace AchWorker.Presenters.ParseReturnFile;

public record ParseReturnFileViewModel(List<AchReturnRecord> Records);
```

```csharp
// src/AchWorker/Presenters/ParseReturnFile/ParseReturnFilePresenter.cs
using AchWorker.Entities;
using AchWorker.UseCases.ParseReturnFile;

namespace AchWorker.Presenters.ParseReturnFile;

public class ParseReturnFilePresenter : IParseReturnFileOutputBoundary
{
    public ParseReturnFileViewModel? ViewModel { get; private set; }

    public void Present(ParseReturnFileResponseModel response)
    {
        ViewModel = new ParseReturnFileViewModel(response.Records);
    }
}
```

- [ ] **Step 2: Verify build**

```powershell
dotnet build src/AchWorker/AchWorker.csproj
```

Expected: Build succeeded (0 errors).

- [ ] **Step 3: Commit**

```bash
git add src/AchWorker/Presenters/
git commit -m "feat(ach-worker): add all presenters and view models"
```

---

## Task 11: Create Input Adapters (Activity Classes)

Replace the three files in `Activities/` with three new files in `InputAdapters/`. Temporal workflow code references these classes by type (e.g. `(AchActivities a) => a.CreateAchFileAsync()`), so keep the same class names and method signatures.

**Note:** `AchReturnRecordDto` is referenced in `AchReturnWorkflow.workflow.cs` as `AchActivities.AchReturnRecordDto`. The new `AchActivities` must keep a compatible public type. Use the `AchReturnRecord` entity and expose it as a type alias, or expose the entity directly. Since the workflow uses `AchActivities.AchReturnRecordDto`, keep a public record with that exact name on the new `AchActivities` class.

**Files:**
- Create: `src/AchWorker/InputAdapters/AchActivities.cs`
- Create: `src/AchWorker/InputAdapters/PaymentActivities.cs`
- Create: `src/AchWorker/InputAdapters/SftpActivities.cs`
- Delete (after creation): `src/AchWorker/Activities/AchActivities.cs`
- Delete (after creation): `src/AchWorker/Activities/PaymentActivities.cs`
- Delete (after creation): `src/AchWorker/Activities/SftpActivities.cs`

- [ ] **Step 1: Create `InputAdapters/AchActivities.cs`**

```csharp
// src/AchWorker/InputAdapters/AchActivities.cs
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

namespace AchWorker.InputAdapters;

public class AchActivities(
    ICreateAchFileInputBoundary createAchFile,
    IAddAchEntryInputBoundary addAchEntry,
    IFinalizeAchFileInputBoundary finalizeAchFile,
    IDeleteAchFileInputBoundary deleteAchFile,
    IRevertAchFileToDraftInputBoundary revertAchFileToDraft,
    IParseReturnFileInputBoundary parseReturnFile)
{
    // Keep this nested type so AchReturnWorkflow can reference AchActivities.AchReturnRecordDto
    public record AchReturnRecordDto(Guid PaymentId, string RCode)
    {
        public static AchReturnRecordDto FromEntity(AchReturnRecord entity) =>
            new(entity.PaymentId, entity.RCode);
    }

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
        return presenter.ViewModel!.Records.Select(AchReturnRecordDto.FromEntity).ToList();
    }
}
```

- [ ] **Step 2: Create `InputAdapters/PaymentActivities.cs`**

```csharp
// src/AchWorker/InputAdapters/PaymentActivities.cs
using AchWorker.Presenters.CollectPendingPayments;
using AchWorker.Presenters.HardAuthorizePayment;
using AchWorker.Presenters.RecordAchReturn;
using AchWorker.Presenters.RecordRepresentment;
using AchWorker.Presenters.RecordSettlement;
using AchWorker.Presenters.SignalBankReturn;
using AchWorker.Presenters.SignalPaymentAddedToBatch;
using AchWorker.Presenters.VoidPaymentAuth;
using AchWorker.UseCases.CollectPendingPayments;
using AchWorker.UseCases.HardAuthorizePayment;
using AchWorker.UseCases.RecordAchReturn;
using AchWorker.UseCases.RecordRepresentment;
using AchWorker.UseCases.RecordSettlement;
using AchWorker.UseCases.SignalBankReturn;
using AchWorker.UseCases.SignalPaymentAddedToBatch;
using AchWorker.UseCases.VoidPaymentAuth;
using Shared.Contracts;
using Temporalio.Activities;

namespace AchWorker.InputAdapters;

public class PaymentActivities(
    ICollectPendingPaymentsInputBoundary collectPendingPayments,
    IHardAuthorizePaymentInputBoundary hardAuthorizePayment,
    IVoidPaymentAuthInputBoundary voidPaymentAuth,
    IRecordSettlementInputBoundary recordSettlement,
    IRecordAchReturnInputBoundary recordAchReturn,
    IRecordRepresentmentInputBoundary recordRepresentment,
    ISignalPaymentAddedToBatchInputBoundary signalPaymentAddedToBatch,
    ISignalBankReturnInputBoundary signalBankReturn)
{
    [Activity]
    public async Task<List<Guid>> CollectPendingPaymentsAsync()
    {
        var presenter = new CollectPendingPaymentsPresenter();
        await collectPendingPayments.CollectPendingPaymentsAsync(presenter, new CollectPendingPaymentsRequestModel());
        return presenter.ViewModel!.PaymentIds;
    }

    [Activity]
    public async Task HardAuthAsync(Guid paymentId)
    {
        var presenter = new HardAuthorizePaymentPresenter();
        await hardAuthorizePayment.HardAuthorizePaymentAsync(presenter, new HardAuthorizePaymentRequestModel(paymentId));
    }

    [Activity]
    public async Task VoidPaymentAuthIfExistsAsync(Guid paymentId)
    {
        var presenter = new VoidPaymentAuthPresenter();
        await voidPaymentAuth.VoidPaymentAuthAsync(presenter, new VoidPaymentAuthRequestModel(paymentId));
    }

    [Activity]
    public async Task RecordSettlementAsync(Guid paymentId)
    {
        var presenter = new RecordSettlementPresenter();
        await recordSettlement.RecordSettlementAsync(presenter, new RecordSettlementRequestModel(paymentId));
    }

    [Activity]
    public async Task RecordAchReturnAsync(Guid paymentId, AchReturnDetails details)
    {
        var presenter = new RecordAchReturnPresenter();
        await recordAchReturn.RecordAchReturnAsync(presenter, new RecordAchReturnRequestModel(paymentId, details));
    }

    [Activity]
    public async Task RecordRepresentmentAsync(Guid paymentId, int representmentCount)
    {
        var presenter = new RecordRepresentmentPresenter();
        await recordRepresentment.RecordRepresentmentAsync(presenter, new RecordRepresentmentRequestModel(paymentId, representmentCount));
    }

    [Activity]
    public async Task SignalPaymentAddedToBatchAsync(Guid paymentId, Guid achFileId, bool isSameDayAch)
    {
        var presenter = new SignalPaymentAddedToBatchPresenter();
        await signalPaymentAddedToBatch.SignalPaymentAddedToBatchAsync(presenter,
            new SignalPaymentAddedToBatchRequestModel(paymentId, achFileId, isSameDayAch));
    }

    [Activity]
    public async Task SignalBankReturnAsync(Guid paymentId, AchReturnDetails details)
    {
        var presenter = new SignalBankReturnPresenter();
        await signalBankReturn.SignalBankReturnAsync(presenter, new SignalBankReturnRequestModel(paymentId, details));
    }
}
```

- [ ] **Step 3: Create `InputAdapters/SftpActivities.cs`**

```csharp
// src/AchWorker/InputAdapters/SftpActivities.cs
using AchWorker.Presenters.DeleteTransferredFile;
using AchWorker.Presenters.MarkReceivedFileProcessed;
using AchWorker.Presenters.TransferAchFile;
using AchWorker.UseCases.DeleteTransferredFile;
using AchWorker.UseCases.MarkReceivedFileProcessed;
using AchWorker.UseCases.TransferAchFile;
using Temporalio.Activities;

namespace AchWorker.InputAdapters;

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
```

- [ ] **Step 4: Delete old activity files**

```powershell
Remove-Item "src/AchWorker/Activities/AchActivities.cs"
Remove-Item "src/AchWorker/Activities/PaymentActivities.cs"
Remove-Item "src/AchWorker/Activities/SftpActivities.cs"
```

Then verify the `Activities/` folder is empty and remove it:

```powershell
Remove-Item "src/AchWorker/Activities" -Recurse
```

- [ ] **Step 5: Update workflow `using` directives**

The workflows import `AchWorker.Activities`. Change to `AchWorker.InputAdapters` in all three workflow files.

In `src/AchWorker/Workflows/AchBatchWorkflow.workflow.cs`:
- Change `using AchWorker.Activities;` → `using AchWorker.InputAdapters;`

In `src/AchWorker/Workflows/AchReturnWorkflow.workflow.cs`:
- Change `using AchWorker.Activities;` → `using AchWorker.InputAdapters;`

In `src/AchWorker/Workflows/PaymentWorkflow.workflow.cs`:
- Change `using AchWorker.Activities;` → `using AchWorker.InputAdapters;`

- [ ] **Step 6: Update `Program.cs` `using` directive**

In `src/AchWorker/Program.cs`:
- Change `using AchWorker.Activities;` → `using AchWorker.InputAdapters;`

- [ ] **Step 7: Verify build**

```powershell
dotnet build src/AchWorker/AchWorker.csproj
```

Expected: Build succeeded (0 errors). If there are namespace errors, check that workflow files reference `AchWorker.InputAdapters` and that `AchActivities.AchReturnRecordDto` is accessible from the workflow.

- [ ] **Step 8: Commit**

```bash
git add src/AchWorker/InputAdapters/ src/AchWorker/Workflows/ src/AchWorker/Program.cs
git commit -m "feat(ach-worker): add input adapters and remove old Activities folder"
```

---

## Task 12: Wire DI in Program.cs

Register all gateway interfaces → output adapters, and all input boundary interfaces → interactors.

**Files:**
- Modify: `src/AchWorker/Program.cs`

- [ ] **Step 1: Add `using` directives and DI registrations**

Replace the existing `Program.cs` with the following (all other code stays the same, only the service registrations change):

```csharp
// src/AchWorker/Program.cs
using AchWorker.Gateways;
using AchWorker.InputAdapters;
using AchWorker.OutputAdapters;
using AchWorker.UseCases.AddAchEntry;
using AchWorker.UseCases.CollectPendingPayments;
using AchWorker.UseCases.CreateAchFile;
using AchWorker.UseCases.DeleteAchFile;
using AchWorker.UseCases.DeleteTransferredFile;
using AchWorker.UseCases.FinalizeAchFile;
using AchWorker.UseCases.HardAuthorizePayment;
using AchWorker.UseCases.MarkReceivedFileProcessed;
using AchWorker.UseCases.ParseReturnFile;
using AchWorker.UseCases.RecordAchReturn;
using AchWorker.UseCases.RecordRepresentment;
using AchWorker.UseCases.RecordSettlement;
using AchWorker.UseCases.RevertAchFileToDraft;
using AchWorker.UseCases.SignalBankReturn;
using AchWorker.UseCases.SignalPaymentAddedToBatch;
using AchWorker.UseCases.TransferAchFile;
using AchWorker.UseCases.VoidPaymentAuth;
using AchWorker.Workflows;
using Temporalio.Client;
using Temporalio.Client.Schedules;
using Temporalio.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient("PaymentApi", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:PaymentApi"] ?? "http://localhost:5001"));
builder.Services.AddHttpClient("AchApi", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:AchApi"] ?? "http://localhost:5002"));
builder.Services.AddHttpClient("SftpApi", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:SftpApi"] ?? "http://localhost:5003"));

builder.Services.AddSingleton<ITemporalClient>(_ =>
    TemporalClient.ConnectAsync(new(
        builder.Configuration["Temporal:Address"] ?? "localhost:7233")).GetAwaiter().GetResult());

// Output Adapters (Gateways)
builder.Services.AddScoped<IAchFileGateway, AchApiGateway>();
builder.Services.AddScoped<IPaymentGateway, PaymentApiGateway>();
builder.Services.AddScoped<ISftpGateway, SftpApiGateway>();
builder.Services.AddScoped<IPaymentSignalGateway, PaymentSignalGateway>();

// Use Case Interactors
builder.Services.AddScoped<ICollectPendingPaymentsInputBoundary, CollectPendingPaymentsInteractor>();
builder.Services.AddScoped<IHardAuthorizePaymentInputBoundary, HardAuthorizePaymentInteractor>();
builder.Services.AddScoped<IVoidPaymentAuthInputBoundary, VoidPaymentAuthInteractor>();
builder.Services.AddScoped<ICreateAchFileInputBoundary, CreateAchFileInteractor>();
builder.Services.AddScoped<IAddAchEntryInputBoundary, AddAchEntryInteractor>();
builder.Services.AddScoped<IFinalizeAchFileInputBoundary, FinalizeAchFileInteractor>();
builder.Services.AddScoped<IDeleteAchFileInputBoundary, DeleteAchFileInteractor>();
builder.Services.AddScoped<IRevertAchFileToDraftInputBoundary, RevertAchFileToDraftInteractor>();
builder.Services.AddScoped<IDeleteTransferredFileInputBoundary, DeleteTransferredFileInteractor>();
builder.Services.AddScoped<ITransferAchFileInputBoundary, TransferAchFileInteractor>();
builder.Services.AddScoped<ISignalPaymentAddedToBatchInputBoundary, SignalPaymentAddedToBatchInteractor>();
builder.Services.AddScoped<ISignalBankReturnInputBoundary, SignalBankReturnInteractor>();
builder.Services.AddScoped<IRecordSettlementInputBoundary, RecordSettlementInteractor>();
builder.Services.AddScoped<IRecordAchReturnInputBoundary, RecordAchReturnInteractor>();
builder.Services.AddScoped<IRecordRepresentmentInputBoundary, RecordRepresentmentInteractor>();
builder.Services.AddScoped<IParseReturnFileInputBoundary, ParseReturnFileInteractor>();
builder.Services.AddScoped<IMarkReceivedFileProcessedInputBoundary, MarkReceivedFileProcessedInteractor>();

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

builder.Services.AddHostedService<ScheduleRegistrationService>();

var host = builder.Build();
await host.RunAsync();

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
                new ScheduleOptions { TriggerImmediately = false });

            logger.LogInformation("Daily ACH batch schedule registered");
        }
        catch (Exception ex) when (
            ex is Temporalio.Exceptions.ScheduleAlreadyRunningException ||
            (ex is Temporalio.Exceptions.RpcException rpc &&
             rpc.Code == Temporalio.Exceptions.RpcException.StatusCode.AlreadyExists))
        {
            logger.LogInformation("Daily ACH batch schedule already exists — skipping");
        }
    }
}
```

- [ ] **Step 2: Final build**

```powershell
dotnet build src/AchWorker/AchWorker.csproj
```

Expected: Build succeeded (0 errors).

- [ ] **Step 3: Commit**

```bash
git add src/AchWorker/Program.cs
git commit -m "feat(ach-worker): wire DI for all gateways and interactors"
```

---

## Self-Review Checklist

**Spec coverage:**
- [x] CollectPendingPayments — Task 4
- [x] HardAuthorizePayment — Task 4
- [x] VoidPaymentAuth — Task 5
- [x] CreateAchFile — Task 5
- [x] AddAchEntry — Task 6
- [x] FinalizeAchFile — Task 6
- [x] DeleteAchFile — Task 6
- [x] RevertAchFileToDraft — Task 6
- [x] DeleteTransferredFile — Task 7
- [x] TransferAchFile — Task 7
- [x] MarkReceivedFileProcessed — Task 7
- [x] SignalPaymentAddedToBatch — Task 8
- [x] SignalBankReturn — Task 8
- [x] RecordSettlement — Task 8
- [x] RecordAchReturn — Task 8
- [x] RecordRepresentment — Task 9
- [x] ParseReturnFile — Task 9
- [x] Entities folder — Task 1
- [x] Gateway interfaces — Task 2
- [x] Output adapters — Task 3
- [x] Presenters — Task 10
- [x] Input adapters (keep class names) — Task 11
- [x] Workflows untouched (only using-directive changed) — Task 11
- [x] DI wiring — Task 12
- [x] Build verification at every task

**Type consistency check:**
- `AchReturnRecord` entity used in `ParseReturnFileResponseModel`, `ParseReturnFileViewModel`, and `AchActivities.AchReturnRecordDto.FromEntity` — consistent.
- `IPaymentGateway.AddActivityAsync` takes `string activityType` and `PaymentApiGateway` calls `Enum.Parse<PaymentActivityType>(activityType)` — consistent, all call sites pass string enum names.
- All input boundary method names match: `{Name}Async`, presenter parameter first — verified across Tasks 4-9.
- `AchActivities.AchReturnRecordDto` is `public record` — accessible from `AchReturnWorkflow` which references `AchActivities.AchReturnRecordDto` — consistent.
