# Heracles — ACH Batch Processing with Temporal.io

Heracles is a demo ACH batch-processing system built on [Temporal.io](https://temporal.io) (.NET SDK). It replaces a traditional Hangfire + MassTransit approach with durable Temporal workflows that survive crashes, handle compensation automatically, and enforce correct ACH lifecycle rules.

## What It Does

A payment created via the Payment API starts a long-running `PaymentWorkflow` that waits for the ACH batch cycle to claim it, then waits out the 2-banking-day return window before settling. A daily `AchBatchWorkflow` collects all pending payments, hard-authorises them, generates a NACHA file, and transfers it to an SFTP endpoint. If an ACH return file arrives, `AchReturnWorkflow` parses it and signals the relevant payment workflows with the R-code.

```
POST /payments  →  PaymentWorkflow (long-running, waits for batch + return window)
                         ↑ AddedToBatch signal
AchBatchWorkflow ────────┤
  CollectPendingPayments │
  HardAuth (×N, ≤50 concurrent)
  CreateAchFile → AchApi
  AddEntries → AchApi
  FinalizeAchFile → AchApi
  TransferAchFile → SftpApi
  SignalPayments ──────────┘

POST /files/inbound → AchReturnWorkflow
  ParseReturnFile → SftpApi
  RecordAchReturn → PaymentApi (per R-code)
  SignalBankReturn → PaymentWorkflow (per payment)
```

## Architecture

| Component | Role |
|-----------|------|
| `PaymentApi` | REST API (`:8081`) — creates payments, records activities, stores state in SQLite |
| `AchApi` | REST API (`:8082`) — manages ACH file lifecycle (Draft → Finalized), generates NACHA content |
| `SftpApi` | REST API (`:8083`) — accepts outbound ACH files and inbound return files, starts `AchReturnWorkflow` |
| `AchWorker` | Temporal worker — hosts all three workflows and their activities |
| `Shared` | Contracts and enums shared across projects |
| Temporal | Durable execution cluster — dev server runs in k8s |

### Workflows

**`PaymentWorkflow`** (one per payment, long-lived)
- Waits for the `AddedToBatch` signal from `AchBatchWorkflow`
- Then waits for the return window (`BankingCalendar.GetReturnWindow`) — 2 banking days for standard ACH, 1 for same-day
- If no return arrives: calls `RecordSettlement` and completes
- If a `BankReturn` signal arrives with R01 and the payment allows representment (≤2 attempts): re-queues and loops
- Otherwise: records the return and completes

**`AchBatchWorkflow`** (triggered daily at 17:00 EST on weekdays via Temporal schedule)
- Fan-out HardAuth with a semaphore (max 50 concurrent)
- Creates ACH file, adds entries, finalizes, transfers to SFTP
- Signals all authorized payment workflows
- Full saga compensation: if any step after file creation fails, deletions and status reverts run in reverse order

**`AchReturnWorkflow`** (triggered per inbound file via `SftpApi`)
- Parses NACHA return file, extracts payment IDs and R-codes from addenda records
- Records `AchReturn` activity on each payment
- Signals `PaymentWorkflow` with return details

### Payment Status (derived from `PaymentActivity` log)

| Status | Meaning |
|--------|---------|
| `Pending` | Created, no activity yet |
| `HardAuth` | Authorised by batch workflow |
| `Submitted` | Included in transferred ACH file |
| `Settled` | Return window elapsed with no return |
| `Returned` | ACH return received |
| `Representment` | Re-submitted after R01 return |

`GET /payments?status=...` filters by the derived status name case-insensitively and ignores surrounding whitespace.

### Payment Request Validation

`POST /payments` rejects invalid ACH payment input before starting a workflow. Amounts must be between `0.01` and `99,999,999.99`, routing numbers are required and must be exactly 9 digits, account numbers are required and limited to 17 characters, and account holder names are required and limited to 22 characters for NACHA output.

### NACHA File Generation

`AchApi` generates NACHA files via `NachaFileGenerator`. Transaction codes: `22` = debit, `27` = credit. The `AchWorker` parses return files by finding addenda lines (record type `7`), extracting the R-code from positions 3–5 and the payment GUID from positions 13–48.

## Project Structure

```
src/
  Shared/          # Contracts (CreatePaymentRequest, BatchDetails, …) + enums
  PaymentApi/      # ASP.NET Minimal API + EF Core SQLite
  AchApi/          # ASP.NET Minimal API + EF Core SQLite + NACHA generator
  SftpApi/         # ASP.NET Minimal API + EF Core SQLite
  AchWorker/       # Generic Host + Temporal worker + BankingCalendar

tests/
  Heracles.Activities.Tests/   # Unit tests for activity HTTP logic
  Heracles.Workflow.Tests/     # Unit tests using Temporal's test environment
  Heracles.Integration.Tests/  # Integration tests (in-process, real EF Core)
  Heracles.E2E.Tests/          # E2E tests against live k8s stack

k8s/
  build-and-deploy.ps1   # One-shot build + push + deploy script
  temporal.dockerfile    # Dev Temporal server image (CLI binary)
  *.yaml                 # Kubernetes manifests (namespace, configmap, per-service)
```

## Running Locally

### Prerequisites

- Docker Desktop with Kubernetes enabled
- .NET 8 SDK

### Deploy to Local Kubernetes

```powershell
.\k8s\build-and-deploy.ps1
```

This:
1. Starts a local Docker registry at `localhost:5000` (if not already running)
2. Builds and pushes all five images
3. Applies all manifests to the `heracles` namespace
4. Waits for rollouts

**Endpoints after deploy:**

| Service | URL |
|---------|-----|
| PaymentApi | http://localhost:8081 |
| AchApi | http://localhost:8082 |
| SftpApi | http://localhost:8083 |
| Temporal UI | http://localhost:8233 |
| Temporal gRPC | localhost:7233 |

### Running Tests

```bash
# Unit + integration tests (no cluster required)
dotnet test tests/Heracles.Activities.Tests
dotnet test tests/Heracles.Workflow.Tests
dotnet test tests/Heracles.Integration.Tests

# E2E tests (requires live k8s stack)
dotnet test tests/Heracles.E2E.Tests
```

## Key Design Decisions

**Temporal schedule for daily batch** — `AchWorker` registers a `daily-ach-batch` schedule on startup (idempotent) that fires at 17:00 EST weekdays with up to 1 minute of jitter. A manual batch can be started by launching `AchBatchWorkflow` directly via the Temporal UI or CLI.

**SQLite with EF Core** — each service owns its own database file mounted on an `emptyDir` volume. This is intentional for the demo; replace with a persistent volume or external DB for production.

**HardAuth semaphore** — the batch workflow uses a Temporal `Semaphore` (capacity 50) so large batches don't flood the bank's auth endpoint.

**Compensation pattern** — `AchBatchWorkflow` accumulates compensation callbacks as it progresses. On failure the list is reversed and each step is attempted; individual compensation failures are logged and ignored so the overall cleanup continues.

**BankingCalendar** — computes the return window deadline accounting for Fed holidays (all 11 US Federal Reserve observed holidays) and weekends. Runs inside the workflow using `Workflow.UtcNow` for determinism.
