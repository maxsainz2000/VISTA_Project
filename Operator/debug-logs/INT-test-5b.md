---
test-id: INT-Test-5b
checklist: INT-verification-checklist.md
branch: debug/INT-test-5b
started: 2026-05-20T17:35
status: in-progress
---

# Debug Session — INT Test 5b (Event Chain Harness — both chains failing)

## Problem Statement

Running the Event Chain Harness reports FAIL on both chains:

- **GoodsReceived**: `DbUpdateException: An error occurred while saving the entity changes. See the inner exception for details.`
- **SaleCompleted**: `InvalidOperationException: Unable to resolve service for type 'MerchSys.SharedKernel.Persistence.ISyncableRepository\`1[MerchSys.POS.Data.POSDbContext]' while attempting to activate 'MerchSys.POS.Services.PaymentService'.`

Report: `%TEMP%\event-chain-report-20260520-173048.md`

## Starting State
- **Commit:** `498c958`
- **Build status:** clean (0 errors, 0 warnings)
- **Relevant files:**
  - `WPF_Applications/MerchSys/src/MerchSys.App/Debug/EventChainVerificationHarness.vb`
  - `WPF_Applications/MerchSys/src/MerchSys.Purchasing/Data/Configurations/VendorConfiguration.vb`

## Allowed Files
- `WPF_Applications/MerchSys/src/MerchSys.App/Debug/EventChainVerificationHarness.vb` — both fixes apply here

### Off-limits (do NOT touch)
- Entity configs, entity classes, services — the harness is the test fixture, not the application

---

## Attempt Log

### Attempt 1
- **Hypothesis (GoodsReceived):** `VendorConfiguration` marks `Address` as `IsRequired()` (NOT NULL).
  The harness vendor seed does not set `.Address`, so SQLite rejects the insert with a NOT NULL
  constraint violation (wrapped as `DbUpdateException`).
- **Hypothesis (SaleCompleted):** The harness's scratch `ServiceCollection` registers
  `IPaymentService → PaymentService`, but `PaymentService` requires
  `ISyncableRepository(Of POSDbContext)` which is not registered. Adding a no-op stub fixes this;
  the Cash payment path never calls `SaveChangesWithJournalAsync` so the stub is safe.
- **Changed:** `EventChainVerificationHarness.vb` — added `.Address` to vendor seed + added
  `HarnessPosRepository` stub class + registered it in `BuildScratchServices`.
- **Build result:** (pending)
- **Runtime result:** (pending operator run)
- **Verdict:** (pending)
- **Action:** (pending)
