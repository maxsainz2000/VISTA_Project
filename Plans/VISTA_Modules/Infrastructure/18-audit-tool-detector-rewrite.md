---
module: MerchSys.Infrastructure
plan-id: INFRA-18
title: "Rewrite agent-wiki audit detectors (Rules 3, 7, 12, 14, 19)"
depends-on: []
estimated-files: 7
priority: high
---

# INFRA-18: Rewrite agent-wiki audit detectors (Rules 3, 7, 12, 14, 19)

## Context

The audit that produced `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-report.md` (2026-05-24) flagged 93 violations across 5 failing rules. A line-by-line verification against the original wiki rule definitions — recorded in `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-improvement-plan.md` — found that roughly **half of those findings are false positives**: ~45–51 are real, ~42–48 are not.

The cause is the same in every case: each detector greps a surface pattern instead of testing the **necessary conditions** the rule itself defines.

| Rule | Reported | True | Failure mode |
|---|---|---|---|
| 3 — `ToListAsync` entity empty | 63 | ~35–40 | flags anonymous & scalar projections that the wiki explicitly excludes |
| 7 — `Console` shadow | 8 | 0 | flags files that import only `Microsoft.Extensions.Logging.Abstractions` |
| 12 — `List.Count` predicate | 1 | 0 | flags `.Count(pred)` on tuple arrays (only `List(Of T)` is affected) |
| 14 — Param shadows property | 20 | ~9–11 | flags `Shared` methods and `Module` members which cannot shadow instance state |
| 19 — `MainWindow` not shell | 1 | 0 | matches text inside `'...` comments |

Until the detectors are corrected, every future audit run will require a manual triage pass to separate real defects from tool noise. INT-14 depends on this plan because it consumes detector output as its input list.

## Prerequisites

- None. This is tooling-only and does not touch product code.

## Wiki References

- `agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md` — defines the bug shape (full-entity materialisation only)
- `agent_wiki/antipatterns/vbnet-console-namespace-shadow.md` — defines the shadow trigger (`Imports Microsoft.Extensions.Logging`)
- `agent_wiki/antipatterns/vbnet-list-count-property-shadows-linq-extension.md` — defines receiver type (`List(Of T)`)
- `agent_wiki/antipatterns/vbnet-parameter-shadows-property.md` — defines scope (instance methods, not `Shared` or `Module`)
- `agent_wiki/patterns/wpf-mainwindow-not-shell-window.md` — defines the antipattern (a call, not a comment)
- `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-improvement-plan.md` — the source of this plan; section 5.A is the detector spec

## Deliverables

```
.claude/skills/vista-audit/                              ' Modified — corrected detector logic for Rules 3, 7, 12, 14, 19
Operator/audit-tests/rule-03/known-good/*.vb            ' New — ~5 snippets the detector must NOT flag
Operator/audit-tests/rule-03/known-bad/*.vb             ' New — ~5 snippets the detector MUST flag
Operator/audit-tests/rule-07/known-good|known-bad/*.vb  ' New
Operator/audit-tests/rule-12/known-good|known-bad/*.vb  ' New
Operator/audit-tests/rule-14/known-good|known-bad/*.vb  ' New
Operator/audit-tests/rule-19/known-good|known-bad/*.vb  ' New
Operator/audit-tests/README.md                          ' New — corpus contract + run instructions
```

If the audit logic lives somewhere other than `.claude/skills/vista-audit/`, locate it first and route the detector edits there. Do not introduce a parallel implementation.

## Specification

### Rule 3 detector — `ToListAsync` full-entity materialisation

Replace literal `\.ToListAsync\(` grep with shape-aware detection:

- **Required**: the chain terminates in `.ToListAsync(...)`.
- **Required (positive)**: the immediate parent (last call before `.ToListAsync`) is one of:
  - bare `DbSet` reference (`_db.Vendors.ToListAsync()`)
  - `.Where(...)`, `.OrderBy(...)`, `.OrderByDescending(...)`, `.ThenBy(...)`, `.ThenByDescending(...)`
  - `.Include(...)`, `.ThenInclude(...)`
  - `.AsNoTracking()`, `.IgnoreQueryFilters()`
  - `.Take(...)`, `.Skip(...)`
- **Required (negative)**: the chain must NOT contain a `.Select(Function(...) ...)` step between the last `.Where`/`.Include`/`DbSet` and the `.ToListAsync` call, because that step projects away from full-entity materialisation.
- **Required (negative)**: the chain must NOT contain `.GroupBy(...)`.

Implementation note: a simple state machine over the dotted chain (tokenise by `.` after stripping line-continuation `_`) is sufficient. A real parser is overkill.

### Rule 7 detector — `Console` namespace shadow

- **Required**: the file contains at least one `Console\.` reference outside comments.
- **Required**: the file contains exactly the import `Imports Microsoft.Extensions.Logging` (no suffix).
  - **Do NOT** match `Imports Microsoft.Extensions.Logging.Abstractions`.
  - **Do NOT** match `Imports Microsoft.Extensions.Logging.Console`.
- If both conditions hold, flag every `Console\.` site in that file. Otherwise, flag none.

### Rule 12 detector — `List.Count` predicate

- **Required**: a call shaped `<receiver>.Count(Function(...) ...)` (predicate form).
- **Required**: the receiver is statically declared `As List(Of T)` or `As New List(Of T)` in the same scope.
- **Skip**: receivers declared as arrays (`{ ... }` literals, `As T()`, `As Array`).
- **Skip**: receivers declared `As IEnumerable(Of T)`, `As IQueryable(Of T)`, `As ICollection(Of T)`, tuple arrays.

If the receiver type cannot be resolved from local scope (e.g., it is a method return), treat as **unknown** and skip rather than flag.

### Rule 14 detector — parameter shadows property

- **Required**: a `Sub New`, `Sub`, or `Function` parameter list inside a `Class` or `Structure`.
- **Skip**: the method is declared `Shared`. Shared methods have no instance scope.
- **Skip**: the enclosing type is a `Module`. Modules have no instance properties.
- **Required**: the enclosing class/structure declares an instance `Property` or `Field` whose name matches a parameter name case-insensitively.
- The detector must walk up to the enclosing type, not just text-match the surrounding lines.

### Rule 19 detector — `MainWindow` not shell

- Strip every line's trailing comment (everything from an unquoted `'` to end-of-line) before pattern matching.
- Match `Application\.Current\.MainWindow` in the stripped text only.

### Audit-tests corpus

`Operator/audit-tests/<rule-id>/known-good/` contains snippets the corrected detector MUST NOT flag.
`Operator/audit-tests/<rule-id>/known-bad/` contains snippets the corrected detector MUST flag.

Each rule needs ≥3 known-good and ≥3 known-bad samples. Pull at least one of each from the verification report's worked examples so the corpus traces back to a real finding.

`Operator/audit-tests/README.md` documents:
- the corpus contract (file naming, expected detector behaviour)
- how to run the audit against the corpus (one command)
- the regression rule: a detector change that misclassifies any corpus file is rejected

## Implementation Notes

- This plan does not modify product code. If a detector change appears to require source edits to make a test pass, that is a sign the detector is still wrong — fix the detector, not the source.
- Re-run the audit against the live codebase after each detector rewrite. Compare counts against the 2026-05-24 baseline (`agent-wiki-verification-report.md`) and record the delta. Expect Rule 3 to drop from 63 → ~35–40, Rule 7 to drop from 8 → 0, Rule 12 to drop from 1 → 0, Rule 14 to drop from 20 → ~9–11, Rule 19 to drop from 1 → 0.
- Do not delete the original `agent-wiki-verification-report.md` — it is the baseline.

## Acceptance Criteria

1. All five detectors implement the rules in Specification above.
2. `Operator/audit-tests/` contains ≥3 known-good and ≥3 known-bad samples per rule.
3. Running the audit against the corpus produces zero misclassifications.
4. Running the audit against the live codebase produces counts within ±10% of the verified-true numbers in `agent-wiki-verification-improvement-plan.md` section 3.
5. The Rule 3 result must include `StockService.vb:77, 120, 141` and `VendorService.vb:58, 122, 141` (known true positives) and must NOT include `FinancialOverviewService.vb:90, 120` or `IncomeStatementService.vb:73` (known false positives).
6. The Rule 7 result must be empty.
7. The Rule 12 result must be empty.
8. The Rule 14 result must include `VatReturnLockedException.vb:26` and `NavigationItem.vb:28` (known true positives) and must NOT include `IAuthenticationService.vb:249, 273` (module — known false positive).
9. The Rule 19 result must be empty.
10. `dotnet build MerchSys.slnx` remains 0 errors, 0 warnings (no product code was touched).

## Output Requirements

Create progress report at `Progress/VISTA_Modules/Infrastructure/INFRA-18-summary.md` using `Progress/_template.md`. Include:

- The corpus directory tree and a one-line description per sample.
- Per-rule before/after counts against the live codebase.
- A short note on any detector change that required a wiki clarification — and a pointer to the wiki PR (handled under Workstream D of the improvement plan, not this plan).
