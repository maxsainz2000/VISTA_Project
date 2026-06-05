---
module: MerchSys.App
plan-id: UX-30
title: "POS Keyboard-First Fast Path — Mouse-Free Cart, Scan-Hot Focus, Tender Hotkeys"
depends-on: [UX-17]
estimated-files: 5
---

# POS Keyboard-First Fast Path — Mouse-Free Cart, Scan-Hot Focus, Tender Hotkeys

> **Level 2 (Advanced) — item A7** of [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md).
> Final item of the Advanced tier.

## Context

The POS cart is the highest-frequency screen — `SalesCartView` graded **84** (top score) in the UX-08
rubric. Cashier speed compounds across every transaction, and reaching for the mouse per line item is
the dominant friction. This plan makes the cart **fully driveable from the keyboard/keypad**: focus
discipline that keeps the SKU/scan field hot, hotkeys for add-item, set-quantity, apply-discount,
tender, and hold/recall, and **Enter-to-commit** on the tender step.

This is an **invocation-only** change — the behaviour of the underlying sale command is unchanged; only
*how* it is invoked changes. It builds on UX-17 (B1) focus/keyboard foundation rather than reinventing
it.

Read `UX-17` (B1 focus visuals, `TabIndex`, `IsDefault`/`IsCancel`, mnemonics), the shell input-binding
pattern in `MainWindow.xaml`, and `Views/POS/SalesCartView.xaml` + `SalesCartViewModel.vb` first.

## Prerequisites

- **UX-17 (B1)** — the keyboard & focus foundation (focus visuals, tab order, default/cancel keys,
  mnemonics) this extends onto the cart's hot path.
- `Views/POS/SalesCartView.xaml` + `MerchSys.POS/ViewModels/SalesCartViewModel.vb` — the target view
  and VM (existing `IsBusy`, `AmountTendered`, the sale/tender commands).
- Existing deps only. **No new NuGet.**

## Scope

- **Scan-hot focus discipline**: the SKU/scan field is focused on view load and **refocused after each
  line is added** (and after quantity/discount edits commit), so the cashier can scan/type continuously
  without touching the mouse.
- **Cart hotkeys** (via `InputBindings`/`KeyBindings` on the cart scope, not global): add-item (Enter
  in the scan field), set-quantity, apply-discount, hold/recall, and go-to-tender. Use mnemonics
  (`_Tender`, `_Discount`, `_Hold`) consistent with B1.
- **Enter-to-commit on tender**: with a valid `AmountTendered`, Enter completes the sale (the tender
  control's `IsDefault`/key binding), mirroring B1's dialog-confirm convention.
- A short **documented keypad flow** (the canonical mouse-free sequence) in the summary/pattern.

> **Out of scope:**
> - Barcode-scanner hardware integration/config — scanners type into the focused field; this assumes
>   keyboard-wedge behaviour, not a device SDK.
> - Changing pricing/discount/tax/credit (utang) **logic** — only how those existing commands are
>   triggered.
> - The other POS screens (history, credit management) — this is the cart fast path specifically.

## Specification

### 0. Watch-items

1. **Keep focus on the scan field — the core of the feature.** After every add-line and after a
   quantity/discount edit commits, programmatically return focus to the SKU/scan field. Verify focus
   isn't stolen by a toast (UX-06/UX-29), a grid selection, or a validation popup. This is the single
   most important behaviour — get it right.
2. **Cart-scoped bindings, not global.** Hotkeys live on the cart view scope so they don't collide with
   the shell's global `Ctrl+1..4`/`Ctrl+K` or fire when the cart isn't active. Avoid hijacking keys the
   scan field needs for normal entry (digits, Enter-to-add). Distinguish "Enter in scan field = add
   line" from "Enter on tender = commit".
3. **Don't change the sale command — only its invocation.** The add-line, discount, hold/recall,
   tender, and complete commands already exist on `SalesCartViewModel`; bind keys to them. No change to
   FIFO decrement, tax, credit, or receipt logic. The `SELECT ... FOR UPDATE` / concurrency behaviour
   of the underlying sale is untouched.
4. **Guard against invalid commits.** Enter-to-tender must respect `CanExecute` (valid amount, non-empty
   cart, not mid-`IsBusy`); it must not double-fire on key repeat or complete a sale twice.
5. **Focus visuals from B1 apply.** The hot field and actionable controls show the B1 theme-aware focus
   adorner so the keyboard user always sees where they are, in both themes.
6. **Owner N/A.** Owner has no POS write access and never reaches the cart write path — no role logic
   to add here, just don't assume Manager-only UI needs gating beyond what already exists.
7. **VB traps** — code-behind focus calls use reserved-keyword-safe names; `.` continuation at
   line-end; no `Await` in `Catch`/`Finally` (BC36943); lambda params not shadowing locals (BC36641).

### 1. Focus discipline

On `Loaded`, focus the scan field. After `AddLineCommand` (and after quantity/discount commit), refocus
it — via a small code-behind helper or an attached behaviour driven off the VM (e.g. an event/flag the
view observes). Ensure toasts/selection don't steal it.

### 2. Cart hotkeys + tender commit

Wire cart-scoped `KeyBinding`s/mnemonics to the existing commands (add, set-qty, discount, hold/recall,
go-to-tender). On the tender step, bind Enter (respecting `CanExecute`) to complete the sale.

### 3. Constraints

- **Invocation-only / additive** — no change to sale, FIFO, tax, credit, or receipt logic.
- **Cart-scoped bindings** — no collision with global shortcuts; scan-field keys preserved.
- **Respect `CanExecute`** — no invalid or double commits.
- **No new NuGet.**
- **B1 focus visuals + theme** — visible focus in both themes; no inline hex.
- **VB traps** — no `Await` in `Catch`/`Finally`; no lambda/local shadowing.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — ring a full sale mouse-free: scan/type SKU → Enter adds line and focus
  returns to scan; adjust a quantity via keyboard; apply a discount via hotkey; hold then recall;
  go-to-tender, type amount, Enter completes; confirm focus returns to scan for the next customer;
  confirm Enter doesn't double-complete and is blocked on an invalid amount; focus adorner visible in
  both themes.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. A full sale (add lines → set quantity → discount → tender → complete) can be rung up without the
   mouse; focus returns to the scan field after each line and after completion.
3. Cart hotkeys are cart-scoped (no collision with global shortcuts) and the scan field still accepts
   normal entry; Enter adds a line in the scan field and commits on tender.
4. Tender commit respects `CanExecute` (valid amount, non-empty cart, not mid-load) and cannot
   double-fire; the underlying sale/FIFO/tax/credit logic is unchanged.
5. B1 focus visuals are visible in both themes; Owner is unaffected (no POS write access).

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-30-summary.md` (template `Progress/_template.md`). Include: the
refocus mechanism (code-behind vs attached behaviour) and how focus-theft is prevented, the cart-scoped
hotkey map, the Enter-to-tender `CanExecute`/double-fire guarding, the documented mouse-free keypad
flow, and the realization results. Note `codebase_wiki` discrepancies (cart view/VM binding additions,
any new attached behaviour).

### Documentation
Add `patterns/wpf-vista-pos-fast-path.md` (the scan-hot refocus rule, cart-scoped-vs-global binding
rule, Enter-add-vs-Enter-commit disambiguation, `CanExecute`/double-fire guarding, the canonical keypad
flow) per `workflow-agent-wiki-update.md`; update `agent_wiki/index.md` + `log.md`.
