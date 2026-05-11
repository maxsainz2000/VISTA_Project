---
module: MerchSys.Integration
plan-id: INT-13
title: "VatReturnView Navigation Wire-up"
depends-on: [INT-02, ACC-11]
estimated-files: 2
---

# VatReturnView Navigation Wire-up

## Context

INT-02 established the shell navigation pattern and wired 16 views into the dictionary consumed by `MainWindowViewModel.NavigateCommand`. ACC-11 subsequently delivered `VatReturnView` but did not retrofit the navigation dictionary — INT-02 had already shipped. The 2026-05-11 Accounting and Integration audits both flag this as a follow-up requirement: `VatReturnView` exists, is registered for DI, has a routing key in ACC-11, but the shell cannot navigate to it because the key is absent from the dictionary that `NavigateCommand` reads.

ACC-14 (VAT Tile Integration) depends on this plan: the tile's `NavigateToVatReturnRequested` event invokes `NavigateCommand("VatReturn")`, which is a no-op until this plan lands.

This plan is intentionally tiny — one dictionary entry plus a menu/sidebar entry plus a smoke check.

## Prerequisites

- **INT-02** (Shell Navigation & View Wiring) — `MainWindowViewModel.NavigateCommand`, the view-key dictionary, the navigation sidebar markup
- **ACC-11** (VAT Reporting Service & Views) — `VatReturnView`, `VatReturnViewModel`, role-gate enforcement

## Wiki References

- `analysis/cross-module-data-flow.md` — VAT reporting is owner-visible read-only; manager-editable
- `concepts/bir-compliance.md` — VAT Return is a primary BIR-facing surface and must be reachable

## Deliverables

```
MerchSys.App/ViewModels/Shell/MainWindowViewModel.vb    ' Modified — add "VatReturn" key
MerchSys.App/Views/Shell/MainWindow.xaml                ' Modified — add sidebar entry
```

## Specification

### MainWindowViewModel dictionary entry

Locate the existing view-key → view-type dictionary (or equivalent registration mechanism) in `MainWindowViewModel`. Add:

```
{"VatReturn", GetType(VatReturnView)}
```

The exact key string `"VatReturn"` is the contract shared with ACC-14. If the existing dictionary uses a different naming convention (e.g., `"vat-return"` or `"VatReturnView"`), follow that convention and **inform ACC-14** by updating its specification to match. Do not silently diverge; the navigation key is a cross-plan contract.

`VatReturnViewModel` must be resolvable from the DI container — ACC-11 should have done this. Confirm by inspection; if it is missing from DI, the issue belongs in ACC-11 and a separate fix is needed.

### MainWindow.xaml sidebar entry

The existing sidebar likely uses an `ItemsControl` or `ListBox` of navigation entries. Add:

```xml
<navigation:NavigationItem
    Label="VAT Return"
    Icon="DocumentText"
    NavigationKey="VatReturn"
    Role="Manager" />
```

If the sidebar entries are data-driven from a collection in `MainWindowViewModel` rather than declared inline, add to that collection instead. **Read the existing sidebar implementation first** and match its idiom exactly.

The `Role="Manager"` gate (or its data-driven equivalent) hides the entry from Owner. Owner sees the VAT KPI tile on the dashboard but cannot navigate to the editable return view; this matches ACC-11's existing role enforcement.

### Smoke verification

Add a single line to the existing INT-02 smoke test (or run INT-10's harness with the new entry):

- Resolve the navigation dictionary at runtime.
- Assert `"VatReturn"` is a registered key.
- Assert it resolves to a `Type` derived from `VatReturnView`.

The smoke verification is a one-line change to existing harness code, not a new file. It counts within the existing INT-10 verification surface, not as a deliverable here.

## Implementation Notes

- This is the smallest plan in the audit cycle. Its only complexity is **discovering the existing convention** and matching it precisely. Do not introduce a parallel registration mechanism even if you find the current one ugly.
- The audit notes "16 views were wired" by INT-02 and `VatReturnView` is the 17th. Update any docstring or summary in INT-02 that references the "16 views" count, if such a count exists in `Progress/VISTA_Modules/Integration/INT-02-summary.md` — a single-sentence update, not a rewrite.
- ACC-14 depends on this plan and references the key `"VatReturn"`. If the existing convention dictates a different key string, edit ACC-14's specification before implementing ACC-14, do not implement ACC-14 against the wrong key.
- The sidebar role-gate is the only piece that depends on the existing role-aware navigation primitive (whatever INT-02 used). If the existing implementation has no role gate and the four Manager-only views are hidden by other means, follow the existing mechanism.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. The navigation dictionary in `MainWindowViewModel` contains the key `"VatReturn"` (or the matched-convention equivalent, documented in the summary).
3. Resolving the key returns the `VatReturnView` type.
4. The sidebar shows a "VAT Return" entry when logged in as Manager.
5. The sidebar does not show the entry when logged in as Owner.
6. Clicking the entry navigates to `VatReturnView` without exception.
7. `VatReturnViewModel` is resolved from DI on navigation (no `New VatReturnViewModel()` direct construction in any code path).
8. INT-02's "16 views" count (if present in its progress summary) is updated to 17 with a one-sentence note.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Integration/INT-13-summary.md` using `Progress/_template.md`. Include:

- The exact key string used (so ACC-14 can reference it verbatim).
- A note recording the existing dictionary's naming convention (CamelCase / kebab-case / suffix-View / etc.) for future audit clarity.
- Confirmation that `VatReturnViewModel` is registered in DI.
- A small `git diff` of the two modified files.

### Documentation
- Inline comment in `MainWindowViewModel` next to the new dictionary entry citing INT-02 (the originating convention) and ACC-11 (the view source).
- No new XML docs needed; the surface is single-line.
