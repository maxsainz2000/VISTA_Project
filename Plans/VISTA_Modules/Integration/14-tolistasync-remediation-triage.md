---
module: MerchSys.Integration
plan-id: INT-14
title: "ToListAsync remediation triage — produce per-method checklist"
depends-on: [INFRA-18]
estimated-files: 1
priority: high
---

# INT-14: ToListAsync remediation triage — produce per-method checklist

## Context

The agent-wiki verification report (2026-05-24) flagged 63 sites for the `ToListAsync` silent-empty-list bug. The follow-up verification (`Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-improvement-plan.md`) confirmed that only ~35–40 of those are real — full-entity materialisations subject to the EF Core 10 + VB.NET bug documented in `agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md`. The rest are anonymous-type or scalar projections that the wiki itself excludes.

INT-15 and INT-16 will apply the raw-`SqliteConnection` workaround to the real ones. Before either of those plans can execute, the scope must be pinned down: **exactly which methods need the fix, what entity type each one returns, and which ones load `Include` graphs that need manual JOIN SQL.**

This plan produces that scope document. It writes no product code.

## Prerequisites

- **INFRA-18** — the corrected Rule 3 detector. Triage runs against its output, not against the original 2026-05-24 report.

## Wiki References

- `agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md` — the canonical fix shape
- `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-improvement-plan.md` — section 5.B is the source of this plan

## Deliverables

```
Operator/debug-logs/tolistasync-remediation-checklist.md   ' New
```

## Specification

### Re-run the audit

Re-run the agent-wiki audit (with the corrected detectors from INFRA-18) and capture the Rule 3 result list. This is the authoritative input. Do not use the 2026-05-24 numbers directly.

### One row per flagged call site

For each call site in the corrected Rule 3 result, fill one row in `tolistasync-remediation-checklist.md` with:

| Column | Source |
|---|---|
| `Module` | top-level project folder (`Inventory`, `POS`, `Purchasing`, `Accounting`) |
| `File` | path relative to `WPF_Applications/MerchSys/src/` |
| `Line` | line number of the `.ToListAsync` call |
| `Method` | enclosing method/function name |
| `Entity` | element type of the returned `List(Of T)` |
| `Has Include` | yes/no — whether the chain contains `.Include` or `.ThenInclude` |
| `UI surface` | which screen(s) consume this method (one sentence) |
| `Fix complexity` | `simple` (single table, no Include) / `joined` (single Include) / `graph` (multiple Includes / nested ThenInclude) |
| `Batch` | `INT-15` or `INT-16` (see section below) |
| `Fix applied` | empty (filled by INT-15/16) |
| `Verified` | empty (filled by INT-15/16) |

The `UI surface` field is the most important for the sequencing decision and is the only one that requires reading caller code, not just the service file. Grep for callers of each flagged method and trace at least one path to a `ViewModel`. Methods invoked only by background jobs (e.g., archival sweeps) are explicitly noted as such.

### Batch assignment

- **INT-15 (Inventory + POS)**: any flagged method in `MerchSys.Inventory.*` or `MerchSys.POS.*`.
- **INT-16 (Purchasing + Accounting)**: any flagged method in `MerchSys.Purchasing.*` or `MerchSys.Accounting.*`.

If INFRA-18's corrected detector surfaces a true positive in a module not listed above, append it to whichever of INT-15/16 has the lower row count.

### Sequence within each batch

After the table, append a short ordered list per batch, ranked by user visibility. The wiki rule and INFRA-18's acceptance criteria already imply this ordering for the known true positives:

- Visible list / dashboard screens first.
- Search and detail screens next.
- Background-job-only methods last.

The ordering belongs in this triage document, not inside INT-15/16's plan files — those plans consume the order, they do not redefine it.

### Highlight Include-graph methods

At the end of the document, list every row with `Fix complexity = graph` separately. These require manual JOIN SQL plus per-row entity reassembly and are riskier than the bare-entity `SELECT ... FROM table` shape used in the existing Vendor fix. Flagging them up front tells INT-15/16 to budget extra time for those methods.

## Implementation Notes

- This is a **read-only** plan. No `.vb` files are edited. The single deliverable is the markdown checklist.
- Use the existing Vendor fix in production code as the "simple" reference shape. If you cannot tell whether a method is `simple` or `joined` by reading it, mark it `joined` and let INT-15/16 confirm.
- Do not pre-decide which methods are "not worth fixing." Every true positive lands in the checklist. INT-15/16 may defer specific rows with explicit justification, but the deferral is recorded there, not silently dropped here.

## Acceptance Criteria

1. `Operator/debug-logs/tolistasync-remediation-checklist.md` exists.
2. Every row in the corrected Rule 3 detector result has exactly one row in the checklist.
3. Every row has every column populated except `Fix applied` and `Verified`.
4. Every row is assigned to `INT-15` or `INT-16`.
5. The document ends with an ordered sequence list per batch and an Include-graph section.
6. The Vendor fix already applied (per INFRA-test-X) is recorded as `Fix applied = yes` with a pointer to the relevant `Operator/debug-logs/` entry.

## Output Requirements

Create progress report at `Progress/VISTA_Modules/Integration/INT-14-summary.md` using `Progress/_template.md`. Include:

- The total row count and the per-batch row counts.
- A short note on any disagreement between the INFRA-18 detector output and the 2026-05-24 baseline (e.g., a previously-flagged site that the corrected detector cleared).
- The total count of `Fix complexity = graph` rows — these set the risk profile for INT-15/16.
