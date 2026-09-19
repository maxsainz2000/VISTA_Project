---
module: MerchSys.Integration
plan-id: INT-17
title: "Rename parameters that shadow properties (Rule 14 true positives)"
depends-on: [INFRA-18]
estimated-files: 6
priority: high
---

# INT-17: Rename parameters that shadow properties (Rule 14 true positives)

## Context

`agent_wiki/antipatterns/vbnet-parameter-shadows-property.md` documents that a parameter name matching an instance property case-insensitively (VB.NET is case-insensitive) silently routes property assignments to the parameter instead. The verification report (2026-05-24) flagged 20 sites; the follow-up verification confirmed ~9–11 are real, with the rest being false positives on `Shared` methods and `Module` members that cannot shadow instance state (handled by INFRA-18's corrected detector).

Every confirmed true positive is currently **mitigated** by the constructor body using `Me.PropertyName = paramName` explicitly — so no logic bug exists today. The risk is latent: a future edit that drops the `Me.` qualifier silently mutates the parameter instead of the property, with no compiler warning.

This plan does seven mechanical renames in one commit to eliminate the latent footgun.

## Prerequisites

- **INFRA-18** — needed so that the post-rename re-run of Rule 14 confirms the renamed sites are gone (and no new ones surface).

## Wiki References

- `agent_wiki/antipatterns/vbnet-parameter-shadows-property.md` — the canonical antipattern
- `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-improvement-plan.md` — section 5.C is the source of this plan
- `CLAUDE.md` — VB.NET Build Traps table → "Parameter shadows property" row

## Deliverables

```
MerchSys.Accounting/Exceptions/VatReturnLockedException.vb        ' Modified — 4 constructor params
MerchSys.App/Models/NavigationItem.vb                             ' Modified — 2 constructor params on NavigationGroup
MerchSys.App/Views/LoginView.xaml.vb                              ' Modified — 1 constructor param
MerchSys.Inventory/Services/StockService.vb                       ' Modified — 3 params on InsufficientStockException.New
MerchSys.POS/Debug/ReceiptArchivalHarness.vb                      ' Modified — 2 params on EmptyConfigurationSection
MerchSys.Accounting/ViewModels/VatReturnViewModel.vb              ' Modified (only if INFRA-18 confirms — see Implementation Notes)
```

## Specification

### Rename map

Apply each rename verbatim. Use Edit's `replace_all` only inside the body of the affected method — never across the whole file, because the new parameter name may already exist as a property elsewhere.

| File | Current parameter | New parameter |
|---|---|---|
| `VatReturnLockedException.vb:26` | `returnId` | `lockedReturnId` |
| `VatReturnLockedException.vb:26` | `year` | `lockedYear` |
| `VatReturnLockedException.vb:26` | `period` | `lockedPeriod` |
| `VatReturnLockedException.vb:26` | `formType` | `lockedFormType` |
| `NavigationItem.vb:28` (in `NavigationGroup`) | `groupName` | `name` |
| `NavigationItem.vb:28` (in `NavigationGroup`) | `items` | `navigationItems` |
| `LoginView.xaml.vb:16` | `viewModel` | `vm` |
| `StockService.vb:20` (in `InsufficientStockException`) | `productId` | `product` |
| `StockService.vb:20` (in `InsufficientStockException`) | `requestedQty` | `requested` |
| `StockService.vb:20` (in `InsufficientStockException`) | `availableQty` | `available` |
| `ReceiptArchivalHarness.vb:750` (in `EmptyConfigurationSection.New`) | `key` | `sectionKey` |
| `ReceiptArchivalHarness.vb:782` (in `EmptyConfigurationSection.GetSection`) | `key` | `sectionKey` |

### Update every reference inside the same method body

Each rename requires updating:

- the parameter declaration in the method signature;
- every reference to the parameter inside the method body (including string interpolations like `$"... {returnId} ..."`);
- the `MyBase.New($"...")` call inside constructors that pass the parameter to a base exception message.

Do **not** rename:

- the property being mirrored (`ReturnId`, `Year`, `GroupName`, etc.). Those are public API and renaming them ripples into callers and XAML bindings.
- the private backing field if one exists. Backing fields use the `_camelCase` convention already and are not in scope.

### Throw-site check

`VatReturnLockedException` and `InsufficientStockException` are thrown from service code. Grep for the throw sites:

- `Throw New VatReturnLockedException(...)` — any caller. Verify by inspection that callers pass positional arguments (they do; constructors are positional). No caller changes needed.
- `Throw New InsufficientStockException(...)` — `StockService.DeductStockFIFOAsync`. Verify the caller passes positional arguments. No caller change needed.

If a caller uses **named arguments** (`Throw New X(returnId:=42, year:=2026, ...)`), update those names to match the renamed parameters. Grep before assuming none exist.

### XAML binding check

`LoginView.xaml.vb` `New(viewModel As LoginViewModel)` is invoked by DI in `MerchSys.App/App.xaml.vb` (or the equivalent composition root) via positional construction. XAML does not bind constructor parameter names. Confirm by reading the composition root before changing the rename if the search surfaces any named-argument call sites.

### Conditional row — `VatReturnViewModel.PopulateFromReturn(vatReturn As VatReturn)`

This row is flagged in the verification report but the rename target depends on whether `VatReturnViewModel` actually has a `VatReturn` property of its own (the verification report could not confirm without a deeper read). Process:

1. Read `VatReturnViewModel.vb` end to end. Grep for `Public Property VatReturn` and `Private _vatReturn`.
2. If a property `VatReturn` exists on the same class: rename the parameter to `sourceReturn` and update every reference inside `PopulateFromReturn`'s body.
3. If no such property exists: this row is a false positive that should have been filtered by INFRA-18. Do not rename. Record the false-positive confirmation in the summary.

## Implementation Notes

- **One commit.** All renames in a single commit titled exactly: `refactor: rename shadowing parameters per agent-wiki rule 14`. The commit message body lists the file:line pairs touched.
- **Build between each file.** After each file's rename set, run `dotnet build MerchSys.slnx`. A typo in a string interpolation will surface as `BC30451` (name not declared). Catching it per-file is much faster than catching it at the end.
- **No behaviour changes.** Every rename is name-substitution only. If a diff hunk has any line that does more than rename a parameter (e.g., reorders fields, adjusts whitespace beyond what the rename requires), revert it.
- **Comments.** Update any inline comments that reference the old parameter name. Doc comments that reference `<paramref name="..."/>` must be updated to the new name or the build will warn.

## Acceptance Criteria

1. `dotnet build MerchSys.slnx` — 0 errors, 0 warnings.
2. Every rename in the table above is applied.
3. The conditional `VatReturnViewModel` row is either renamed or explicitly recorded as a confirmed false positive.
4. Re-running the INFRA-18 corrected Rule 14 detector produces zero hits for the files listed in this plan's Deliverables.
5. No public API changes — no renamed property, no renamed public method, no renamed event.
6. No XAML binding breakage — every existing `Binding` path resolves at runtime as before.
7. All changes land in one commit with the title specified above.

## Output Requirements

Create progress report at `Progress/VISTA_Modules/Integration/INT-17-summary.md` using `Progress/_template.md`. Include:

- The exact `git diff` per file (or a link to the commit).
- For the conditional `VatReturnViewModel` row: the decision and the evidence.
- The before/after Rule 14 hit counts in the affected files.
- A short note for `CLAUDE.md` reviewers: this commit eliminates the seven known true-positive sites but does not change the antipattern itself — the rule remains active for future code.

## Post-Completion Notes

If INFRA-18's re-run surfaces additional Rule 14 true positives not in this plan (e.g., new code merged after 2026-05-24), open a follow-up rename PR rather than expanding INT-17.
