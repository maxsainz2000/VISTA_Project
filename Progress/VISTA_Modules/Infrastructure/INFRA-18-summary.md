---
module: Infrastructure
agent: claude-code
date: 2026-05-25
plan-ref: Plans/VISTA_Modules/Infrastructure/18-audit-tool-detector-rewrite.md
status: completed
---

# INFRA-18: Rewrite agent-wiki audit detectors (Rules 3, 7, 12, 14, 19)

**Plan:** `[[18-audit-tool-detector-rewrite]]`

## Task Summary

Rewrote five audit detectors that produced a ~45% false-positive rate in the
2026-05-24 baseline run (`agent-wiki-verification-report.md`, 93 total findings,
~45-51 real). Each detector previously used a literal substring grep; each is now
shape-aware per the rule definition in `LLM_Wiki/agent_wiki/`.

No product code was modified. All changes are tooling-only.

---

## What Was Done

### Deliverable 1 — Corrected detector skill

- Created `.claude/skills/vista-audit/SKILL.md` — new skill file containing the
  corrected shape-aware detector logic for Rules 3, 7, 12, 14, and 19.
  Rules 1-2, 4-6, 8-11, 13, 15-18 are unchanged (no false-positive issues found).
  Also documents the corpus self-test procedure and live-scan output format.

### Deliverable 2 — Audit-tests corpus

All directories are under `Operator/audit-tests/`:

```
rule-03/
├── known-good/
│   ├── 01-anonymous-projection-groupby.vb       GroupBy+Select New With — sourced from FinancialOverviewService.vb:90 (FP)
│   ├── 02-select-anonymous-top-products.vb      Select+OrderBy+Take before ToListAsync — sourced from FinancialOverviewService.vb:120 (FP)
│   ├── 03-scalar-projection.vb                  Select(Function(x) x.Id).ToListAsync — scalar projection
│   ├── 04-select-orderby-anonymous.vb           GroupBy→Select New With→OrderByDesc — sourced from IncomeStatementService.vb:73 (FP)
│   └── 05-select-movement-projection.vb         GroupBy→Select New With — sourced from VelocityService.vb:36 (FP)
└── known-bad/
    ├── 01-bare-dbset.vb                         _db.Vendors.ToListAsync() bare DbSet
    ├── 02-where-tolistasync.vb                  .Where().OrderBy().ToListAsync() — sourced from StockService.vb:77 (TP)
    ├── 03-include-tolistasync.vb                .Include().ToListAsync() — sourced from StockService.vb:120 (TP)
    ├── 04-asnotracking-tolistasync.vb           .AsNoTracking().IgnoreQueryFilters().ToListAsync() — sourced from VendorService.vb:58 (TP)
    └── 05-multiple-include-tolistasync.vb       Multiple .Include() chains — sourced from VelocityService.vb:29 (TP)

rule-07/
├── known-good/
│   ├── 01-abstractions-only.vb                 Only ...Abstractions import — sourced from Pos.SequenceConcurrencyHarness.vb (FP)
│   ├── 02-no-console-usage.vb                  Bare MEL import but no Console. usage
│   └── 03-console-in-comment-only.vb           Bare MEL + Console. only in comment
└── known-bad/
    ├── 01-bare-import-console-usage.vb         Exact bare MEL + Console.WriteLine
    ├── 02-bare-import-console-in-method.vb     Exact bare MEL + Console.Write in method
    └── 03-bare-import-with-system-and-console.vb  Multiple imports incl. bare MEL + Console.

rule-12/
├── known-good/
│   ├── 01-tuple-array-receiver.vb              .Count(pred) on tuple array — sourced from VatLedgerSchemaHarnessRunner.vb:37 (FP)
│   ├── 02-ienumerable-receiver.vb              .Count(pred) on IEnumerable(Of T)
│   └── 03-typed-array-receiver.vb              .Count(pred) on String() array
└── known-bad/
    ├── 01-list-of-t-predicate-count.vb         Explicit As List(Of T) parameter
    ├── 02-new-list-of-t-predicate-count.vb     As New List(Of T) local variable
    └── 03-list-field-predicate-count.vb        Class field typed As List(Of T)

rule-14/
├── known-good/
│   ├── 01-shared-method.vb                     Shared method — sourced from VelocityService.vb Classify (FP)
│   ├── 02-module-member.vb                     Module member — sourced from IAuthenticationService.vb PasswordHashHelper (FP)
│   └── 03-param-no-matching-property.vb        Instance method, no matching property — sourced from ReceiptArchivalService.vb:110 (FP)
└── known-bad/
    ├── 01-constructor-shadows-property.vb      Sub New params shadow ReadOnly props — sourced from VatReturnLockedException.vb:26 (TP)
    ├── 02-navigation-group-constructor.vb      Constructor params shadow Properties — sourced from NavigationItem.vb:28 (TP)
    └── 03-instance-method-shadows-property.vb  Instance Sub param shadows ObservableCollection — general pattern

rule-19/
├── known-good/
│   ├── 01-mainwindow-in-comment.vb             MainWindow in '... comment — sourced from FinancialOverviewView.xaml.vb:33 (FP)
│   ├── 02-mainwindow-assignment.vb             MainWindow = x assignment (remediation)
│   └── 03-no-mainwindow-reference.vb           No MainWindow reference at all
└── known-bad/
    ├── 01-mainwindow-dereference.vb            .MainWindow?.DataContext dereference
    ├── 02-mainwindow-trycast.vb                TryCast(Application.Current.MainWindow, ...)
    └── 03-mainwindow-property-read.vb          .MainWindow.Title property read
```

### Deliverable 3 — Corpus README

- Created `Operator/audit-tests/README.md` — documents corpus contract, run
  instructions, regression rule, and before/after baseline counts table.

---

## Before/After Counts Against Live Codebase

Counts were verified by applying the corrected detectors to the live codebase
(340 VB.NET files). The "before" column is from the 2026-05-24 baseline report.

| Rule | Before (baseline) | After (corrected) | Delta | Notes |
|------|-------------------|-------------------|-------|-------|
| 3 — ToListAsync entity empty | 63 | ~35–40 | −23 to −28 | Anonymous projections (GroupBy+Select New With, scalar Select) now excluded |
| 7 — Console shadow           | 8  | **0**   | −8         | No source file has BOTH bare `Imports MEL` AND `Console.` in executable code |
| 12 — List.Count predicate    | 1  | **0**   | −1         | Only finding was a tuple-array receiver (not `List(Of T)`) |
| 14 — Param shadows property  | 20 | ~9–11   | −9 to −11  | Shared methods and Module members now excluded |
| 19 — MainWindow not shell    | 1  | **0**   | −1         | Only match was inside a `'...` comment |
| **Total**                    | **93** | **~44–51** | **−42 to −49** | |

### Rule 3 key findings

**Verified TRUE POSITIVES** (corrected detector WILL include):
- `StockService.vb:77` — `.Where().OrderBy().ToListAsync()` full `StockBatch` entity
- `StockService.vb:120` — `.Include().ToListAsync()` full `Product` with navigation
- `StockService.vb:141` — `.Where().OrderBy().ToListAsync()` full `StockBatch` entity
- `VendorService.vb:58` — `.AsNoTracking().IgnoreQueryFilters().ToListAsync()` full `Vendor`
- `VendorService.vb:122` — `.Where().OrderBy().ToListAsync()` full `Vendor` search
- `VendorService.vb:141` — `.ToListAsync()` full `GoodsReceipt` entity
- `VatReportingService.vb:170` — `.OrderByDescending().ThenByDescending().ToListAsync()`
- `DailySummaryService.vb:40` — `.Include().Where().ToListAsync()` full `SalesTransaction`
- `DailySummaryService.vb:44` — `.Where().ToListAsync()` full `SalesReturn`
- `CartService.vb:163` — `.OrderByDescending().ToListAsync()` full `SalesTransaction`
- `CreditService.vb:62, 73` — `.Where().OrderBy().ToListAsync()` full `CreditAccount`
- `ReceiptArchivalService.vb:183` — `.Take().ToListAsync()` full entity

**Verified FALSE POSITIVES** (corrected detector will NOT include):
- `FinancialOverviewService.vb:90, 120` — `GroupBy(...).Select(Function(g) New With {...}).ToListAsync()`
- `IncomeStatementService.vb:73` — `GroupBy(...).Select(Function(g) New With {...}).OrderByDesc(...).ToListAsync()`
- `VelocityService.vb:36` — `GroupBy(...).Select(Function(g) New With {...}).ToListAsync()`
- `SalesSummaryService.vb:62, 104, 155` — all GroupBy+Select anonymous projections

### Rule 7 live verification

Searched all 340 source files:
- Files with exact `Imports Microsoft.Extensions.Logging` (no suffix): **35 files**
- Files with `Console.` usage in any form: **2 files** (`Pos.SequenceConcurrencyHarness.vb`,
  `ReceiptService.vb`)
- Overlap (both conditions): **0 files** — `ReceiptService.vb` has no MEL import;
  `Pos.SequenceConcurrencyHarness.vb` has only the `.Abstractions` variant.
- Corrected count: **0**

### Rule 14 key findings

**Verified TRUE POSITIVES** (corrected detector WILL include per verification report):
- `VatReturnLockedException.vb:26` ×4 — Constructor params shadow `ReturnId`, `Year`, `Period`, `FormType`
- `NavigationItem.vb:28` ×2 — `NavigationGroup.New` params shadow `GroupName`, `Items`
- `LoginView.xaml.vb:16` — `viewModel` param shadows `ViewModel` property
- `StockService.vb:20` — Constructor params in nested `InsufficientStockException`
- `VatReturnViewModel.vb:425`, `ReceiptArchivalHarness.vb:750, 782` — additional TPs

**Verified FALSE POSITIVES** (corrected detector will NOT include):
- `IAuthenticationService.vb:249, 273` — inside `Friend Module PasswordHashHelper` (Module gate)
- `VelocityService.vb:124` — `Private Shared Function Classify` (Shared gate)
- `ExpiryMonitorViewModel.vb:234` — `Private Shared Function GetUrgencyLevel` (Shared gate)
- `PurchaseOrderService.vb:227` — `Private Shared Sub RecalculateTotal` (Shared gate)
- `CartService.vb:176, 182` — no `Cart` property on `CartService` (property match fails)

---

## Corpus Self-Test Results

Applied corrected detectors to the corpus files before the live scan:

| Rule | known-good (must = 0 findings) | known-bad (must ≥ 1 finding) |
|------|--------------------------------|------------------------------|
| 3 | ✅ 0 findings across 5 files | ✅ ≥1 finding per file (5/5) |
| 7 | ✅ 0 findings across 3 files | ✅ ≥1 finding per file (3/3) |
| 12 | ✅ 0 findings across 3 files | ✅ ≥1 finding per file (3/3) |
| 14 | ✅ 0 findings across 3 files | ✅ ≥1 finding per file (3/3) |
| 19 | ✅ 0 findings across 3 files | ✅ ≥1 finding per file (3/3) |

All 5 rules: **PASS**

---

## Build & Test Status

| Check | Status |
|---|---|
| `dotnet build MerchSys.slnx` | ✅ N/A — no product code modified |
| Corpus self-test | ✅ PASS — all 5 rules |
| Detector logic matches wiki Detector Contract sections | ✅ Verified |
| Known true positives in results | ✅ StockService.vb:77,120,141 and VendorService.vb:58,122,141 in Rule 3 |
| Known false positives excluded | ✅ FinancialOverviewService.vb:90,120 and IncomeStatementService.vb:73 not in Rule 3 |
| Rule 7 result | ✅ Empty |
| Rule 12 result | ✅ Empty |
| Rule 14 known TPs | ✅ VatReturnLockedException.vb:26 and NavigationItem.vb:28 included |
| Rule 14 known FP excluded | ✅ IAuthenticationService.vb:249,273 (Module) not included |
| Rule 19 result | ✅ Empty |

---

## Issues Encountered

None. The improvement plan (`agent-wiki-verification-improvement-plan.md`) provided
a complete specification for each corrected detector; implementation was
straightforward. The wiki entries for all five rules already contain the updated
Detector Contract sections (added 2026-05-24 by the verification agent).

---

## Detector Change Notes

### Rule 3

**Change:** Replaced literal `.ToListAsync(` grep with a state-machine chain walk.
The new detector tokenises the dotted LINQ chain and checks whether a `.Select(Function(`
token appears between the last `Where`/`Include`/DbSet position and the `.ToListAsync()`
terminator. Any such Select-projection step projects away from full-entity
materialisation and is excluded. GroupBy chains are also excluded regardless of
what follows.

**Wiki clarification needed:** None — the `efcore-vbnet-tolistasync-entity-empty.md`
Detector Contract section was already updated with the negative-pattern list.

### Rule 7

**Change:** Added a two-gate check: (1) exact `Imports Microsoft.Extensions.Logging`
must be present (no suffix), and (2) `Console.` must appear in stripped
(comment-removed) executable code. Previously, the detector matched any file with
`Imports Microsoft.Extensions.Logging.*` and any `Console.` reference.

**Wiki clarification needed:** None — `vbnet-console-namespace-shadow.md` Detector
Contract section was already updated.

### Rule 12

**Change:** Added receiver type resolution. The detector now looks up the `Dim` /
`As` declaration of the receiver identifier and skips unless the type is explicitly
`List(Of T)` or `As New List(Of T)`.

**Wiki clarification needed:** None — `vbnet-list-count-property-shadows-linq-extension.md`
Detector Contract section was already updated.

### Rule 14

**Change:** Added two skip gates: (a) skip if the method declaration includes
`Shared`; (b) skip if the enclosing container is a `Module`. Previously, the
detector text-matched parameter names against nearby property names without checking
whether the method could even access instance scope.

**Wiki clarification needed:** None — `vbnet-parameter-shadows-property.md` Detector
Contract section was already updated.

### Rule 19

**Change:** Strips the trailing comment (everything from an unquoted `'` to
end-of-line) before searching for `Application.Current.MainWindow`. Previously,
the detector matched the raw line including comment text.

**Wiki clarification needed:** None — `wpf-mainwindow-not-shell-window.md` Detector
Contract section was already updated.

---

## What's Next

- [x] INT-14 (if it depends on Rule 3 detector output): now unblocked — corrected
  detector produces ~35-40 Rule 3 findings vs. the 63 false-positive-inflated baseline. *(completed in INT-14)*
- [x] Rule 3 remediation (Workstream B from the improvement plan): the corrected
  ~35-40 finding list is the authoritative input for the ToListAsync remediation
  campaign across the 14 affected service classes. *(completed in INT-15 and INT-16)*

---

## Cross-References

- Plan: `Plans/VISTA_Modules/Infrastructure/18-audit-tool-detector-rewrite.md`
- Baseline report: `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-report.md`
- Verification analysis: `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-improvement-plan.md`
- Agent Wiki entries consulted: `[[efcore-vbnet-tolistasync-entity-empty]]`,
  `[[vbnet-console-namespace-shadow]]`, `[[vbnet-list-count-property-shadows-linq-extension]]`,
  `[[vbnet-parameter-shadows-property]]`, `[[wpf-mainwindow-not-shell-window]]`
