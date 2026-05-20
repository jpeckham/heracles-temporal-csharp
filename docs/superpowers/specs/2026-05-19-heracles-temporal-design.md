# Heracles Temporal — Design Spec
**Date:** 2026-05-19  
**Status:** Approved  
**Replaces:** heracles (Hangfire + MassTransit version)

---

## Overview

A reimplementation of the Heracles ACH batch processing sample app using Temporal.io as the sole orchestration and scheduling layer, replacing Hangfire (scheduling) and MassTransit routing slips (distributed workflows + compensation). The same business capabilities are preserved — scheduled daily ACH batch, cross-API workflows, distributed transactions with compensating actions, and batch processing — while the architecture is simplified and several domain model improvements are made.

---

## Bounded Context Design

Three services with clear, non-overlapping responsibilities:

| Service | Role | Temporal involvement |
|---|---|---|
| **PaymentApi** | Payment entity lifecycle + append-only activity ledger | None — pure HTTP API, no Temporal dependency |
| **AchApi** | ACH file management endpoints (NACHA generation) | Client only — manual batch trigger endpoint starts `AchBatchWorkflow` |
| **AchWorker** | Temporal worker host — owns all workflows, activities, and schedule registration | Worker + Schedule |
| **SftpApi** | Generic file transport (outbound delivery + inbound file detection) | Client only — starts `AchReturnWorkflow` when inbound return file detected |

**Key principle:** SftpApi is transport infrastructure, not ACH infrastructure. It can deliver zip files to an eforms vendor, or any other file type, without ACH logic bleeding in.

---

## Solution Structure

```
Heracles.sln
├── src/
│   ├── PaymentApi/
│   │   └── ...ASP.NET minimal API
│   ├── AchApi/
│   │   └── ...ASP.NET minimal API
│   ├── AchWorker/
│   │   ├── Workflows/
│   │   │   ├── PaymentWorkflow.workflow.cs
│   │   │   ├── AchBatchWorkflow.workflow.cs
│   │   │   └── AchReturnWorkflow.workflow.cs
│   │   ├── Activities/
│   │   │   ├── PaymentActivities.cs       # HTTP → PaymentApi
│   │   │   ├── AchActivities.cs           # HTTP → AchApi
│   │   │   └── SftpActivities.cs          # HTTP → SftpApi
│   │   └── Program.cs                     # Generic Host + DI + worker + schedule registration
│   ├── SftpApi/
│   │   └── ...ASP.NET minimal API
│   └── Shared/
│       └── ...contracts, models, validators (no business logic)
├── tests/
│   ├── Heracles.Activities.Tests/
│   ├── Heracles.Workflow.Tests/
│   └── Heracles.Integration.Tests/
├── .editorconfig                          # includes *.workflow.cs analyzer overrides
└── docker-compose.yml                     # Temporal dev server + all services
```

**AchWorker** is a .NET Generic Host (no HTTP listener). It registers the Temporal worker, registers the daily schedule idempotently at startup, and makes outbound HTTP calls to the three APIs from within activities.

---

## Domain Model

### PaymentApi

```
Payment
├── PaymentId: Guid
├── RoutingNumber: string           (9 digits, ABA validated)
├── AccountNumber: string           (max 17 chars)
├── AccountHolderName: string       (max 22 chars — NACHA limit)
├── Amount: decimal
├── Type: PaymentType               (Credit | Debit)
├── AllowsRepresentment: bool       (payer consent required — NACHA rule)
├── CurrentStatus: PaymentStatus    (derived from tail of Activities list)
└── Activities: List<PaymentActivity>  (append-only ledger)

PaymentActivity
├── ActivityId: Guid
├── PaymentId: Guid
├── Type: PaymentActivityType
│     SoftAuth | HardAuth | Capture | Settlement
│     AchSubmitted | AchReturn | Representment
│     Void | Dispute | DisputeReversed | Refund | PaidOut
├── OccurredAt: DateTime
├── Amount: decimal?                (nullable — voids/disputes may not carry amount)
├── ReferenceCode: string?          (R-code for returns, batch number for submissions)
└── Notes: string?
```

`CurrentStatus` is always derived from the latest `PaymentActivity.Type` — never set directly. This gives a complete audit trail and makes ACH return + representment tracking natural.

### AchApi

```
AchFile
├── FileId: Guid
├── BatchNumber: string             (yyyyMMdd)
├── Status: AchFileStatus
│     Draft | Finalized | Submitted | ReturnReceived | Reconciled
├── Entries: List<AchEntry>
└── ...existing fields

AchEntry
└── ...existing fields
    + RepresentmentCount: int       (max 2 per NACHA rules)
```

### SftpApi

```
TransferredFile     (existing — outbound files sent)
└── ...existing fields

ReceivedFile        (new — inbound files detected: return files, vendor responses)
├── FileId: Guid
├── FileName: string
├── ReceivedAt: DateTime
├── FileType: ReceivedFileType      (AchReturn | Other)
├── ContentHash: string             (SHA-256)
└── Status: ReceivedFileStatus      (Pending | Processing | Processed | Failed)
```

---

## Workflows

### `PaymentWorkflow`

One instance per payment. Workflow ID: `payment-{paymentId}` (deterministic — no storage needed).

Runs from payment creation through settlement or terminal return state.

```
Created
  → SoftAuth       (validate routing/account number format)
  → [signal: AddedToBatch(isSameDayAch: bool)]  ← sent by AchBatchWorkflow when payment included in file
  → WaitConditionAsync(returnWindow, () => _returnReceived)
      return window = 1 banking day  (same-day ACH — signal carries isSameDayAch=true)
                    = 2 banking days (standard ACH — signal carries isSameDayAch=false)
      window computed as actual TimeSpan at signal time, skipping weekends + Federal Reserve holidays

  ← BankReturn signal arrives:
      → record AchReturn activity (with R-code)
      → R-code is hard failure (R02 closed, R04 invalid, etc.) → terminal
      → R-code is soft failure (R01 insufficient funds):
          AllowsRepresentment=true AND RepresentmentCount < 2
            → record Representment activity
            → re-enter WaitConditionAsync for next return window
          otherwise → terminal

  ← timer expires (no return):
      → record Settlement activity
      → record PaidOut activity
      → workflow completes
```

**Return window is measured in banking days** (no weekends, no Federal Reserve holidays). A utility computes the actual `TimeSpan` at the moment of ACH submission.

**Representment limit:** NACHA rules allow a maximum of 2 representments after the original submission. `RepresentmentCount` on `AchEntry` tracks this.

### `AchBatchWorkflow`

Triggered by Temporal Schedule: `0 17 * * 1-5` (5:00 PM EST, weekdays). Also supports manual trigger via AchApi HTTP endpoint.

Workflow ID: `ach-batch-{yyyyMMdd}` (one per business day, idempotent re-trigger).

```
1. CollectPendingPayments      GET PaymentApi/payments?status=Pending
2. Fan out (semaphore: 50 concurrent):
     HardAuth × N             PUT PaymentApi/payments/{id}/activities  { type: HardAuth }
3. Fan in — collect results, filter auth failures (log but continue with successes)
4. Guard: if zero authorized payments → fail workflow with ApplicationFailureException
5. CreateAchFile               POST AchApi/files
6. Fan out (parallel, ungated):
     AddEntry × N             POST AchApi/files/{fileId}/entries
7. Fan in
8. FinalizeFile                POST AchApi/files/{fileId}/finalize   (generates NACHA content)
9. TransferFile                POST SftpApi/files
10. Signal each PaymentWorkflow(payment-{id}).AddedToBatchAsync(batchDetails)
```

**Compensation** (saga pattern — registered before each step, executed in reverse on failure):
- `TransferFile` compensates → DELETE SftpApi/files/{transferredFileId}
- `FinalizeFile` compensates → PATCH AchApi/files/{fileId}/status { status: Draft }
- `AddEntry` compensates → DELETE AchApi/files/{fileId}/entries/{entryId}
- `CreateAchFile` compensates → DELETE AchApi/files/{fileId}
- `HardAuth` compensates → PUT PaymentApi/payments/{id}/activities { type: Void }

Steps 1 and 10 (read-only / signal-only) have no compensation.

### `AchReturnWorkflow`

Started by SftpApi when an inbound return file is received. SftpApi uses a Temporal client to start this workflow — it needs no worker of its own.

```
1. FetchReturnFileContent      GET SftpApi/files/{receivedFileId}/content   (activity)
2. ParseReturnFile             parse NACHA content → List<AchReturnRecord>  (activity)
3. Fan out (parallel):
     For each R-code record:
       → POST PaymentApi/payments/{id}/activities  { type: AchReturn, referenceCode: R-code }
       → Signal PaymentWorkflow(payment-{id}).BankReturnAsync(returnDetails)
4. PATCH SftpApi/files/{receivedFileId}/status  { status: Processed }
```

---

## Schedule Registration

`AchWorker/Program.cs` registers the schedule idempotently at startup. If the schedule already exists, `CreateScheduleAsync` is a no-op.

```csharp
await client.CreateScheduleAsync(
    "daily-ach-batch",
    new Schedule(
        Action: ScheduleActionStartWorkflow.Create(
            (AchBatchWorkflow wf) => wf.RunAsync(),
            new(id: $"ach-batch-{DateTime.UtcNow:yyyyMMdd}", taskQueue: "ach-worker")),
        Spec: new ScheduleSpec {
            CronExpressions = new[] { "0 17 * * 1-5" },
            Jitter = TimeSpan.FromMinutes(1),
        }),
    new CreateScheduleOptions { TriggerImmediatelyIfMissed = false });
```

**No Scheduler project.** The Temporal cluster is the durable schedule store. All services can inspect schedules via the Temporal Web UI or `temporal schedule list`.

---

## Service Communication

All cross-service calls are plain HTTP. No message broker. `AchWorker` activities use typed `HttpClient` instances injected via DI.

```
AchWorker (activities)
  → PaymentApi   :5001   (payment CRUD + activity ledger)
  → AchApi       :5002   (file + entry management)
  → SftpApi      :5003   (outbound transfer, inbound status patch)

SftpApi
  → Temporal cluster :7233  (client only — starts AchReturnWorkflow)

AchApi
  → Temporal cluster :7233  (client only — manual batch trigger endpoint)
```

**Retry:** Temporal activity retry policies handle transient HTTP failures. No Polly needed in activities — Temporal retries the whole activity on failure with exponential backoff.

---

## Error Handling

| Scenario | Behavior |
|---|---|
| Transient HTTP failure in activity | Temporal retries activity automatically (default: unlimited with backoff, capped by `ScheduleToCloseTimeout`) |
| Permanent failure (400 Bad Request, validation) | Activity throws `ApplicationFailureException(nonRetryable: true)` → workflow catches, runs compensation |
| Zero payments in batch | Workflow throws `ApplicationFailureException("No pending payments")` → workflow fails cleanly, no compensation needed |
| SftpApi transfer failure | Compensation runs: delete file from SftpApi → revert AchFile to Draft → remove HardAuth activity entries |
| PaymentWorkflow missing when return arrives | `AchReturnWorkflow` logs warning, continues with remaining payments — one missing workflow does not block the return file processing |
| Representment limit exceeded | PaymentWorkflow transitions to terminal state, logs R-code and count |

---

## Testing Strategy

### `Heracles.Activities.Tests`
- Framework: xUnit + `ActivityEnvironment`
- Tests each activity class in isolation with mocked `HttpClient`
- Covers: happy path, transient HTTP failure, non-retryable errors, heartbeat behavior

### `Heracles.Workflow.Tests`
- Framework: xUnit + `WorkflowEnvironment.StartLocalAsync()` (standard) and `StartTimeSkippingAsync()` (timer tests)
- Real workflow code, mock activity implementations
- Key scenarios:
  - `PaymentWorkflow`: no-return path (timer expires → settled), R01 with representment, R01 without representment, hard R-code terminal
  - `AchBatchWorkflow`: happy path, partial auth failure, SftpApi failure + compensation, 100-payment fan-out
  - `AchReturnWorkflow`: fan-out signals to multiple PaymentWorkflows, missing workflow graceful skip

### `Heracles.Integration.Tests`
- Framework: xUnit + `WorkflowEnvironment.StartLocalAsync()` + `WebApplicationFactory<Program>` per API
- All in-process, no Docker, but every HTTP call is real
- Key scenarios:
  - **Full happy path:** create payments → batch workflow → ACH file → SFTP transfer → time-skip → all payments settled
  - **Full return path:** batch → simulate SftpApi inbound return file → AchReturnWorkflow → PaymentWorkflows signaled → representment submitted
  - **Compensation path:** batch with SftpApi failure → assert AchFile reverted + payment activities rolled back

During integration tests the Temporal Web UI is accessible at `localhost:8080` — workflow history is visible in real time.

---

## Improvements Over Original Heracles

| Area | Original | This version |
|---|---|---|
| Scheduling | Dedicated Hangfire service + SQL Server storage | No scheduler service — Temporal Schedule on the cluster |
| Orchestration | MassTransit routing slips (parent + nested child slips) | Native Temporal fan-out/fan-in with `WhenAllAsync` |
| Message broker | RabbitMQ required | Eliminated — Temporal is the coordination layer |
| Payment model | Single `Status` enum | Append-only `Activities` ledger — full audit trail |
| ACH returns | Not modeled | First-class: R-code handling, representment with consent check, return window timer |
| Cross-service calls | MassTransit request/response over RabbitMQ | Plain HTTP — simpler, easier to trace |
| Compensation | MassTransit `IActivity<TArguments, TLog>` interface | Temporal saga pattern — explicit compensation list in workflow code |
| Concurrency control | RabbitMQ consumer concurrency config | `Temporalio.Workflows.Semaphore` — declared in workflow code |
| Observability | Hangfire Dashboard | Temporal Web UI — workflow history, signals, timers, retries all visible |
| Infrastructure | RabbitMQ + Redis + Hangfire DB + 6 services | Temporal cluster + 4 services (or Temporal Cloud) |
