---
module: MerchSys.Integration
plan-id: INT-12
title: "Runtime Event Chain Verification"
depends-on: [INT-08, INT-10, INT-11]
estimated-files: 3
---

# Runtime Event Chain Verification

## Context

INT-10 was the runtime verification gate. It validated 13 of 16 views interactively; three POS views failed because `IEventBus` was not registered, which INT-11 then fixed. The 2026-05-11 Integration audit confirms `SalesCartView` and `CreditManagementView` now pass, but `TransactionHistoryView` is still pending re-test (XAML fix applied in INT-11 — `FieldLabel` style on `<Run>` element — needs operator confirmation).

The audit also flags two genuinely unverified runtime paths:

- **Live GoodsReceived chain**: create a PO → receive goods → verify `Inv_StockMovements` row appears with `Type=Receipt`. INT-10 verified this via synthetic SQL only; the real end-to-end UI flow has never been executed.
- **Live SaleCompleted chain**: complete a sale → verify `Inv_StockMovements` row with `Type=Sale`.

These are the final pre-production validations: the events are published (INT-08 wrote the producers), the handlers are wired (INT-03), the data layer is finalised (INT-04), but no human has watched the chain fire end-to-end.

This plan delivers a deterministic, repeatable verification harness for both chains plus a checklist that captures the `TransactionHistoryView` re-test outcome. It also flips INT-10's interactive-navigation checkbox once all 16 views pass.

## Prerequisites

- **INT-08** (StockMovement Log Writes) — write paths for Sale, Receipt, Shrinkage, Return
- **INT-10** (Runtime Verification & Smoke Testing) — the 13-of-16 interactive baseline
- **INT-11** (IEventBus DI Registration Gap) — `IEventBus` now resolvable; `TransactionHistoryView` XAML fix in place

## Wiki References

- `concepts/modular-monolith.md` — Cross-module events are the only allowed integration point
- `analysis/cross-module-data-flow.md` — `GoodsReceivedEvent` and `SaleCompletedEvent` flow diagrams
- `concepts/fifo-costing.md` — `StockMovement` rows are the audit-trail for FIFO valuation

## Deliverables

```
MerchSys.App/Debug/EventChainVerificationHarness.vb     ' New — orchestrates both chains
MerchSys.App/Debug/EventChainReport.vb                  ' New — Markdown report writer
Progress/VISTA_Modules/Integration/INT-12-checklist.md  ' Output artefact created by this plan
```

The checklist file lives under `Progress/` because it is the artefact, not the plan. Once the operator executes the harness and re-tests `TransactionHistoryView`, this file captures the result.

## Specification

### EventChainVerificationHarness

```
Public Class EventChainVerificationHarness

    Public Sub New(host As IHost)
        ' Resolve module DbContext factories and the IEventBus
    End Sub

    Public Async Function VerifyGoodsReceivedChainAsync() As Task(Of ChainVerificationResult)
    Public Async Function VerifySaleCompletedChainAsync() As Task(Of ChainVerificationResult)

End Class

Public Class ChainVerificationResult
    Public Property ChainName As String
    Public Property Passed As Boolean
    Public Property PublisherFired As Boolean
    Public Property HandlerExecuted As Boolean
    Public Property StockMovementRowFound As Boolean
    Public Property ExpectedMovementType As String
    Public Property ActualMovementType As String
    Public Property DurationMs As Long
    Public Property Detail As String
End Class
```

#### GoodsReceived chain
1. Build a scratch composite DB set (one SQLite file with all four module schemas — same path).
2. Seed: one vendor, one product with starting stock `0`.
3. Issue a `CreateDraftPurchaseOrderAsync` through `IPurchaseOrderService`.
4. Transition to `Issued` then `Received` through the real PO lifecycle service.
5. Subscribe to `GoodsReceivedEvent` on `IEventBus` before step 4; capture firing.
6. After lifecycle completes, query `Inv_StockMovements` for a row matching the receipt with `Type = 'Receipt'`, expected quantity, and non-null audit columns.
7. Populate `ChainVerificationResult` with each step's pass/fail.

#### SaleCompleted chain
1. On the same scratch DB, with the stock now `> 0` from the previous step.
2. Build a cart through `ICartService`, complete payment via `IPaymentService`.
3. Subscribe to `SaleCompletedEvent`; capture firing.
4. After the sale completes, query `Inv_StockMovements` for a row with `Type = 'Sale'`, negative quantity (per INT-08's sign convention), matching transaction reference.
5. Populate the result.

The two chains are run sequentially against the same scratch DB to also exercise the realistic ordering (you cannot sell what you have not received). Each writes to the same `EventChainReport`.

### EventChainReport

A `StringBuilder`-driven Markdown emitter:

```
## GoodsReceived chain
- Publisher fired: ✅
- Handler executed: ✅
- Stock movement row: ✅ (Type=Receipt, Qty=+10)
- Duration: 142 ms

## SaleCompleted chain
- Publisher fired: ✅
- Handler executed: ✅
- Stock movement row: ✅ (Type=Sale, Qty=-3)
- Duration: 98 ms
```

Output path: `%TEMP%\event-chain-report-<timestamp>.md`. The harness surfaces a `Notification.Wpf` toast with the path on completion.

### INT-12-checklist.md content

A short markdown file the operator fills in during execution:

```markdown
---
plan-id: INT-12
generated: <timestamp>
---

# Runtime Event Chain Verification — Checklist

## TransactionHistoryView re-test
- [ ] View navigates without exception
- [ ] All columns render
- [ ] Operator: <name>
- [ ] Result attached: yes/no

## GoodsReceived chain (from harness)
- [ ] Publisher fired
- [ ] Handler executed
- [ ] StockMovement row present with Type=Receipt

## SaleCompleted chain (from harness)
- [ ] Publisher fired
- [ ] Handler executed
- [ ] StockMovement row present with Type=Sale

## INT-10 checkbox flip
- [ ] After all three above pass, edit INT-10 summary: interactive-navigation item `[/]` → `[x]`
```

The harness creates this file on first run; subsequent runs append timestamped sections rather than overwriting.

## Implementation Notes

- The harness must use the **real** service classes, not stubs. The entire point is to prove the wired-up runtime topology works. If a stub is needed for a non-data dependency (e.g., a printer service), gate it behind a `IIntegrationStubFactory` rather than swapping implementations in DI — keep the DI graph identical to production for everything that touches the event chain.
- Subscribe to the events **before** triggering the producer. Subscribing afterwards races with the dispatcher and produces flaky results.
- Use `IDbContextFactory(Of TContext)` for each module to avoid sharing a single context across the harness threads; mirror the pattern from POS-15's concurrency harness.
- Per the feedback memory (`feedback_vbnet_await_catch.md`): no `Await` in `Catch`/`Finally`. Scratch-DB cleanup happens after the `Try` block.
- INT-10's interactive-navigation item flip is **operator action**, not agent action — this plan does not edit the INT-10 summary itself, it just instructs the operator. The agent that runs INT-12 next-steps (which is presumably this same plan's executor) can do the flip as part of the summary write-up.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. `EventChainVerificationHarness.VerifyGoodsReceivedChainAsync` returns a `ChainVerificationResult` with `Passed = True` against a freshly seeded scratch DB.
3. `VerifySaleCompletedChainAsync` returns `Passed = True` after the GoodsReceived chain has run on the same DB.
4. Both methods populate `PublisherFired`, `HandlerExecuted`, and `StockMovementRowFound` independently — a partial pass shows which of the three steps failed.
5. The Markdown report at `%TEMP%\event-chain-report-<timestamp>.md` contains both sections with explicit pass/fail markers.
6. `Progress/VISTA_Modules/Integration/INT-12-checklist.md` is created on first harness run.
7. The harness is excluded from release builds (`#If DEBUG`).
8. The harness leaves no scratch `.db` files behind on a clean run.
9. The implementation summary records the result of the `TransactionHistoryView` re-test as an operator-observed outcome.
10. If all three checklist items pass, INT-10's summary is updated to flip the interactive-navigation checkbox `[/]` → `[x]`; if any fail, INT-10 remains as-is and the failure is documented.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Integration/INT-12-summary.md` using `Progress/_template.md`. Include:

- A copy of the generated event-chain report.
- The filled-in checklist with the operator's `TransactionHistoryView` re-test result.
- A note recording whether INT-10's summary was updated.

### Documentation
- XML doc on `EventChainVerificationHarness` listing the two chains it covers and the prerequisite seed state.
- Inline comment on the event subscription steps explaining why subscription must precede production.
- A short paragraph in the summary listing any non-event side effects observed (e.g., `Notification.Wpf` toasts firing during the harness run) so operators can distinguish noise from signal.
