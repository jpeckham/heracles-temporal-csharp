# Clean Architecture Refactor Plan

Apply Clean Architecture (entities, use cases, gateways, controller/input adapter, presenter, output adapter) across all services using folders within each existing project.

## Layer conventions

- **Entities** — core domain objects, no framework dependencies
- **UseCases** — one folder per use case containing: `I{Name}InputBoundary`, `I{Name}OutputBoundary`, `{Name}RequestModel`, `{Name}ResponseModel`, `{Name}Interactor`
- **Gateways** — interfaces shaped by what use cases need, not by storage technology
- **Controllers** — ASP.NET MVC controllers (also the CA controller and input adapter for APIs)
- **InputAdapters** — Temporal activities (for AchWorker); translate Temporal triggers into controller calls
- **Presenters** — one per use case, implements `I{Name}OutputBoundary`, exposes `ViewModel`
- **OutputAdapters** — implement gateway interfaces (EF Core, HttpClient, Temporal client)

### Key patterns

Input boundary method signature: presenter always first
```csharp
void Execute(IOutputBoundary presenter, RequestModel request);
```

MVC controller calls use case then reads presenter:
```csharp
var presenter = new MakePaymentPresenter();
_inputBoundary.MakePayment(presenter, requestModel);
return Ok(presenter.ViewModel);
```

Domain events go through an event gateway, not direct Temporal calls:
```csharp
_paymentEventGateway.PaymentCreated(payment); // OutputAdapter starts Temporal workflow
```

## PaymentApi

**Entities:** `Payment`, `PaymentActivity`

**Use cases:**
- `MakePayment` — validate, persist, emit `PaymentCreated` event
- `GetPayment` — fetch single payment with activities
- `ListPayments` — fetch with optional status filter
- `AddPaymentActivity` — append an activity record to a payment

**Gateways:** `IPaymentGateway` (Save, FindById, FindByStatus), `IPaymentEventGateway` (PaymentCreated)

**OutputAdapters:** `PaymentGateway` (EF Core), `PaymentEventGateway` (starts Temporal PaymentWorkflow)

**Data/** keeps only `PaymentDbContext` — EF config, no logic

## AchApi

**Entities:** `AchFile`, `AchEntry`

**Use cases:**
- `CreateAchFile`
- `AddAchEntry`
- `FinalizeAchFile`
- `DeleteAchFile`
- `RevertAchFileToDraft`
- `GetAchFileContent`
- `ListAchFiles`

**Gateways:** `IAchFileGateway`, `INachaGeneratorGateway`

**OutputAdapters:** `AchFileGateway` (EF Core), `NachaGeneratorGateway` (wraps `NachaFileGenerator`)

## SftpApi

**Entities:** `ReceivedFile`, `TransferredFile`

**Use cases:**
- `TransferOutboundFile`
- `DeleteTransferredFile`
- `GetInboundFileContent`
- `MarkInboundFileProcessed`

**Gateways:** `ISftpFileGateway`

**OutputAdapters:** `SftpFileGateway` (EF Core + file I/O)

## AchWorker

Activities become **InputAdapters** — they receive the Temporal trigger and call a controller method (or use case directly if no HTTP concern). Each activity maps to one use case.

**Use cases:**
- `CollectPendingPayments`
- `HardAuthorizePayment`
- `VoidPaymentAuth`
- `CreateAchFile`
- `AddAchEntry`
- `FinalizeAchFile`
- `TransferAchFile`
- `SignalPaymentAddedToBatch`
- `SignalBankReturn`
- `RecordSettlement`
- `RecordAchReturn`
- `RecordRepresentment`
- `ParseReturnFile`
- `MarkReceivedFileProcessed`

**Gateways:** `IAchFileGateway`, `IPaymentGateway`, `ISftpGateway`, `IPaymentSignalGateway`

**OutputAdapters:** `AchApiGateway`, `PaymentApiGateway`, `SftpApiGateway`, `PaymentSignalGateway` (all HttpClient-based)

**Workflows** stay as-is — they orchestrate activities, not business logic

## Shared project

Move domain enums and value types that are genuinely shared (e.g. `PaymentType`, `AchFileStatus`) into `Shared/Entities` or `Shared/Models`. HTTP contract DTOs (`CreatePaymentRequest` etc.) stay as-is — they are input adapter concerns, not domain.

## What does not change

- Temporal workflow orchestration logic (`AchBatchWorkflow`, `PaymentWorkflow`, etc.)
- EF Core models (renamed/moved to Entities but structurally unchanged)
- `NachaFileGenerator` (moves to an output adapter)
- DI registration in `Program.cs` (gains interface bindings, otherwise same)
