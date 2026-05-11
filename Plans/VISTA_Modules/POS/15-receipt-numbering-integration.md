---
module: MerchSys.POS
plan-id: POS-15
title: "Receipt Numbering Integration & Concurrency Validation"
depends-on: [POS-06, POS-13, POS-14]
estimated-files: 3
---

# Receipt Numbering Integration & Concurrency Validation

## Context

POS-06 implemented the original `ReceiptService.GenerateReceiptAsync` using a naïve `Max(ReceiptNumber) + 1` pattern over `Pos_OfficialReceipts`. POS-13 then introduced `IReceiptIntegrityService` with two critical methods:

- `GetNextReceiptNumberAsync()` — atomic, gap-free, monotonic receipt-number reservation backed by the `Pos_ReceiptSequence` table and a sequence-row lock.
- `ComputeAndPersistAsync()` — writes the tamper-evidence row to `Pos_ReceiptIntegrity` (BIR requirement).

POS-14 layered VAT calculation on top via `VatAwareReceiptService`, which calls `ComputeAndPersistAsync` — but **does not** call `GetNextReceiptNumberAsync`. The 2026-05-11 POS audit confirms this: the original `ReceiptService` still uses `Max(ReceiptNumber) + 1`, which is **not** safe under concurrent transactions and **is** the very vulnerability POS-13 was created to eliminate.

The audit also flagged two related gaps:

- The DI registration of `IReceiptIntegrityService → ReceiptIntegrityService` in `MerchSys.App` is recorded in POS-14's wiring block but was never independently verified end-to-end (the audit phrases it as "partially stale — verify final state").
- The debug harness `Pos_SequenceConcurrencyHarness.RunAsync` exists but has never been executed against a scratch database, so the concurrency guarantees of `GetNextReceiptNumberAsync` are unproven in this codebase.

This plan closes all three gaps as a single coherent unit because they are causally linked: there is no point running the harness if the production receipt path bypasses the sequence service, and DI must be confirmed before either matters.

## Prerequisites

- **POS-06** (Receipt Generation) — original `ReceiptService.GenerateReceiptAsync`
- **POS-13** (BIR Tamper-Proof Receipt Retention & Sequence) — `IReceiptIntegrityService`, `GetNextReceiptNumberAsync`, `ComputeAndPersistAsync`, `Pos_SequenceConcurrencyHarness`
- **POS-14** (VAT Configuration & Three-Bucket Calculation) — `VatAwareReceiptService`, current `ComputeAndPersistAsync` call site
- **INFRA-04** (MediatR Event Bus) — `SaleCompletedEvent` publishing pipeline still routes through this

## Wiki References

- `concepts/bir-compliance.md` — Receipt numbers must be gap-free and monotonic
- `concepts/utang-credit-system.md` — credit sales still produce receipts and must follow the same numbering path
- `analysis/cross-module-data-flow.md` — receipt issuance is a leaf operation; downstream events do not depend on the numbering implementation

## Deliverables

```
MerchSys.POS/Services/
└── ReceiptService.vb                          ' Modified — swap Max+1 for sequence service

MerchSys.POS/Services/
└── VatAwareReceiptService.vb                  ' Modified — confirm numbering goes through inner service

MerchSys.App/Startup/
└── PosServiceRegistration.vb                  ' Modified — assert / re-verify IReceiptIntegrityService registration

MerchSys.POS/Debug/
└── ReceiptSequenceHarnessReport.vb            ' New — harness runner that writes a Markdown report
```

Note the modification of three existing files is explicit: this plan is a corrective integration, and the rule prohibiting edits to ACC-03/ACC-07 in ACC-12 does **not** generalise to POS-06 — POS-13 was always intended to supersede the Max+1 path; the codebase simply never finished the swap.

## Specification

### ReceiptService.vb modification

The current `GenerateReceiptAsync` flow (paraphrased):

```
Dim nextNumber = Await _context.OfficialReceipts _
    .Where(Function(r) r.IssuedAt.Year = year) _
    .MaxAsync(Function(r) r.ReceiptNumber, defaultIfEmpty:=0) + 1
```

Replace with:

```
Dim nextNumber = Await _receiptIntegrity.GetNextReceiptNumberAsync(year)
```

Where `_receiptIntegrity` is a new constructor-injected `IReceiptIntegrityService` dependency. The Max+1 query and the unused `defaultIfEmpty` import are deleted, not commented out.

The hash computation continues to be invoked **after** the OR row is saved, via the existing `ComputeAndPersistAsync(receiptId)` call — that part of POS-13 is already wired correctly by POS-14 and does not move.

### VatAwareReceiptService.vb modification

`VatAwareReceiptService` decorates `ReceiptService`. Confirm it still **delegates** numbering by calling the inner service rather than re-implementing it. The plan asserts the decorator stays a decorator: it computes VAT buckets, then calls `inner.GenerateReceiptAsync(...)`, then layers the VAT data via the existing pattern. No numbering logic lives in the decorator.

If inspection finds the decorator currently bypasses `inner` and writes its own receipt row directly, fold it back to the decorator pattern in this plan.

### PosServiceRegistration.vb modification

The audit notes the registration of `IReceiptIntegrityService` is "recorded in POS-14's DI wiring block in `Application.xaml.vb`" — verify and consolidate. If the registration is in `Application.xaml.vb` rather than `PosServiceRegistration.vb`, **move it** to the module-local registration file to match the pattern established by every other POS service. The composition root should remain a list of `services.AddPosModule()` style extension calls, not a soup of per-service registrations.

After the move, ensure:

```
services.AddScoped(Of IReceiptIntegrityService, ReceiptIntegrityService)
services.AddScoped(Of IReceiptService, ReceiptService)
services.Decorate(Of IReceiptService, VatAwareReceiptService)()  ' or manual factory
```

If `Scrutor` is unavailable, use the manual factory pattern (see ACC-12 for an example).

### ReceiptSequenceHarnessReport.vb

A new debug-only runner for `Pos_SequenceConcurrencyHarness`:

```
Public Module ReceiptSequenceHarnessReport

    Public Async Function RunAndReportAsync(
        host As IHost,
        Optional concurrency As Integer = 16,
        Optional iterationsPerWorker As Integer = 50
    ) As Task

End Module
```

Behaviour:

1. Build a scratch `PosDbContext` against `%TEMP%\vista-receipt-seq-harness-<guid>.db`.
2. Migrate schema; seed `Pos_ReceiptSequence` with a base row.
3. Spin up `concurrency` parallel tasks, each calling `GetNextReceiptNumberAsync` `iterationsPerWorker` times.
4. Collect all returned numbers; assert: no duplicates, no gaps, monotonic per-year.
5. Write a Markdown report to `%TEMP%\receipt-sequence-harness-report-<timestamp>.md` listing concurrency, total reservations, duplicates count (expected `0`), gap count (expected `0`), and elapsed milliseconds.
6. Surface a `Notification.Wpf` toast with pass/fail and the report path.

Gate the entry point with `#If DEBUG`. Release builds must not ship a one-click "stress my receipt sequence" button.

## Implementation Notes

- The swap from `Max+1` to `GetNextReceiptNumberAsync` is the single biggest behavioural change. Verify by `git diff` that the Max+1 query and any related `EF.Functions.Year` imports are removed, not just bypassed.
- `GetNextReceiptNumberAsync` from POS-13 takes a year (or period) parameter — confirm the exact signature when reading the file and pass the correct value. Do **not** invent a new overload; if the signature is wrong, the bug is in POS-13 and a separate plan is needed.
- The DI registration check is intentionally a code change (moving the registration to the module-local file) and not just a verification — if it is already in the module-local file, this becomes a no-op and the implementation summary should record that explicitly with a `git diff` snippet showing no change.
- The harness must use `IDbContextFactory(Of PosDbContext)` so each worker task gets its own context. Sharing a single context across threads is the classic EF Core concurrency footgun and would mask, not test, the sequence service's locking behaviour.
- Per project policy (CLAUDE.md): no test project. The harness is a debug runner, not xUnit.
- Per the feedback memory (`feedback_vbnet_await_catch.md`): `Await` is not allowed in `Catch`/`Finally` blocks in VB.NET (BC36943). The scratch-file cleanup path in the harness must capture any exception state and perform any async cleanup after the `Try` block, not inside `Finally`.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. `ReceiptService.GenerateReceiptAsync` no longer references `MaxAsync` over `Pos_OfficialReceipts.ReceiptNumber`; `git grep -n "MaxAsync" MerchSys.POS/Services/ReceiptService.vb` returns no matches.
3. `ReceiptService` constructor accepts `IReceiptIntegrityService` and the field is used in the receipt path.
4. `VatAwareReceiptService` still delegates numbering to its inner `IReceiptService` (decorator preserved).
5. `IReceiptIntegrityService` is registered in `PosServiceRegistration.vb` (or whichever module-local registration extension exists), not in `Application.xaml.vb`.
6. The composition root in `Application.xaml.vb` calls a single `services.AddPosModule()` (or equivalent) for POS service wiring — no individual `services.AddScoped(Of IReceiptIntegrityService, ...)` line at the root.
7. `ReceiptSequenceHarnessReport.RunAndReportAsync` writes a Markdown report; under default parameters (`concurrency = 16`, `iterationsPerWorker = 50`) the report shows `Duplicates = 0`, `Gaps = 0`, and `TotalReservations = 800`.
8. The harness leaves no scratch `.db` files behind on a clean run.
9. The harness is excluded from release builds (`#If DEBUG`).
10. No edits to POS-13 source files (`IReceiptIntegrityService.vb`, `ReceiptIntegrityService.vb`, `Pos_SequenceConcurrencyHarness.vb`) — this plan consumes them, it does not modify them.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-15-summary.md` using `Progress/_template.md`. Include:

- The exact `git diff` of `ReceiptService.GenerateReceiptAsync` showing the Max+1 query removed and the sequence-service call introduced.
- The pre- and post-state of where `IReceiptIntegrityService` is registered (`Application.xaml.vb` vs `PosServiceRegistration.vb`).
- A sample harness report from a clean run, with the four key metrics (duplicates, gaps, total reservations, elapsed ms).

### Documentation
- XML doc comment on the modified `ReceiptService.GenerateReceiptAsync` describing the dependency on `IReceiptIntegrityService` and citing POS-13.
- Inline comment in `ReceiptSequenceHarnessReport` documenting why each worker gets its own `DbContext` instance (EF Core thread-safety constraint).
- A short note in the summary listing any discrepancies discovered between the audit's account of the codebase and the actual current state — this is feedback for future audit accuracy.
