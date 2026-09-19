---
module: MerchSys.App
plan-id: UX-34
title: "UX Finishing Touches — Password Reveal, Reduced-Motion Listener, Shortcuts-Overlay & Dialog DI"
depends-on: [UX-07, UX-20, UX-25, UX-27]
estimated-files: 6
---

# UX Finishing Touches — Password Reveal, Reduced-Motion Listener, Shortcuts-Overlay & Dialog DI

> Small, self-contained polish items closing `ux_review_report.md` §1.9, §1.10, §2.2, §2.11. Each
> finishes an already-shipped roadmap item (UX-07, UX-25, UX-27, UX-20). Low risk, no shared business
> logic — grouped only because each is too small for its own plan.

## Context

Four loose ends from the review, each independent:

- **§1.9 — Two-state password reveal.** `IconEyeOffGeometry` and `IconChevronLeftGeometry` are defined
  in `Icons.xaml` but never consumed. The `LoginView` reveal button always shows the "eye" icon, so the
  user can't tell from the glyph whether the password is currently shown or hidden.
- **§1.10 — Reduced-motion is read once at startup.** `Application_Startup` checks
  `SystemParameters.ClientAreaAnimation` only at launch; toggling OS animations mid-session has no
  effect until restart. Decide: add a live listener or document "restart required".
- **§2.11 — ConfirmationDialog keyboard behavior absent from the shortcuts overlay.** The UX-27 cheat
  sheet doesn't mention the confirm/cancel/type-to-confirm interaction of the UX-20 dialog.
- **§2.2 — ConfirmationDialog DI/documentation inconsistency.** `DefaultConfirmationPresenter` does
  `New ConfirmationDialog()` directly; it's the only dialog window not represented in `di-registry.md`.

All four are presentation-only. **No business logic, query, or write-path change.**

## Prerequisites

- **UX-07** — `Icons.xaml` (`IconEyeGeometry`, `IconEyeOffGeometry`, `IconChevronLeftGeometry`).
- **UX-25** — the reduced-motion startup check in `Application.xaml.vb`.
- **UX-27** — `ShortcutsOverlay` + `ShortcutsOverlayViewModel` and its grouped-shortcut content model.
- **UX-20** — `ConfirmationDialog` / `DefaultConfirmationPresenter`.
- Existing deps only. **No new NuGet.**

## Scope

### A. Two-state password reveal (review §1.9)
In `LoginView`, drive the reveal button's icon from the reveal state: a `DataTrigger` (or equivalent)
toggles `IconEyeGeometry` ↔ `IconEyeOffGeometry` as the password toggles shown/hidden. Consuming
`IconEyeOffGeometry` also removes one of the two dead-resource warnings.
(`IconChevronLeftGeometry` — if no real consumer is introduced, either wire it to an existing
back/collapse affordance **or** remove it from `Icons.xaml`; decide and record. Don't leave it dead.)

### B. Reduced-motion: decide and implement (review §1.10)
**Option 1 (preferred):** add a `SystemParameters.StaticPropertyChanged` listener that re-reads
`ClientAreaAnimation` and updates the app's motion flag live, so mid-session OS changes take effect
without restart. **Option 2:** keep startup-only and **document** "restart required to pick up an OS
reduced-motion change" (in the agent wiki + the UX-25 behavior note). Pick one in the summary; if
Option 1, ensure the listener is detached cleanly on shutdown (no leak).

### C. ConfirmationDialog group in the shortcuts overlay (review §2.11)
Add a "Confirmation Dialogs" group to the `ShortcutsOverlay` content explaining: **Enter** = confirm
(when enabled), **Esc** = cancel, and that typed-confirmation dialogs require an **exact text match** to
enable the confirm button. Content-only addition to the existing overlay model.

### D. ConfirmationDialog DI consistency (review §2.2)
Resolve the documentation inconsistency: **either** register `ConfirmationDialog` as **Transient** and
have `DefaultConfirmationPresenter` resolve it, **or** keep direct `New` instantiation and document the
"modal dialogs are newed per-invocation, not DI-resolved" convention. Whichever is chosen, the
`di-registry.md` gap is closed (by the codebase-wiki sync) and the rationale is recorded.

> **Out of scope:**
> - Any change to authentication, the confirm/cancel logic, or motion easing curves themselves.
> - New shortcuts beyond documenting existing dialog behavior.

## Specification

### 0. Watch-items

1. **Presentation-only.** No auth, business, query, or write change. The password reveal toggles
   visibility display only — it never logs, stores, or transmits the password.
2. **Icon binding is a brush/geometry swap, not a color prop.** Feed `IconEyeOffGeometry` into the
   `Geometry`/`Data`, never a brush into a `Color` property. Recolors with the theme via tokens.
3. **Listener hygiene (Option B1).** If a `StaticPropertyChanged` listener is added, subscribe once and
   unsubscribe on exit; do not capture a leaking reference to a window/VM.
4. **Overlay content is data, not new key handling.** The §C addition documents existing dialog keys; it
   does not add new global hotkeys.
5. **Tokens + theme.** Any new visual recolors on toggle; no inline hex.
6. **VB traps.** Full `clr-namespace` root prefix (MC3074); `Console` → `System.Console` if logging;
   no `Await` in `Catch`/`Finally`; reserved-keyword-safe names; `<Setter>` targets a DP.

### 1. Constraints

- Reuse existing icons, overlay model, dialog, and presenter; add no parallel infrastructure.
- No new NuGet. Tokens + theme; no inline hex.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check (both themes):** login reveal button shows eye-off when password is visible and
  eye when hidden, and toggles correctly; (Option 1) flipping OS animations mid-session changes motion
  live; the shortcuts overlay shows the Confirmation Dialogs group; a confirmation dialog still confirms
  on Enter / cancels on Esc / requires exact typed match.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. The `LoginView` reveal button shows a two-state icon (eye ↔ eye-off) reflecting reveal state;
   `IconEyeOffGeometry` is consumed; `IconChevronLeftGeometry` is either consumed or removed (no dead
   icon resources remain).
3. Reduced-motion behavior is decided and either reacts live (listener, cleanly detached) or is
   documented as restart-required.
4. The shortcuts overlay includes a Confirmation Dialogs group describing confirm/cancel/type-to-confirm.
5. The `ConfirmationDialog` DI pattern is settled and the registry gap is closed; no business/auth change.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-34-summary.md` (template `Progress/_template.md`). Include: the
reduced-motion decision (and listener-cleanup proof if Option 1), the `IconChevronLeftGeometry`
disposition, the `ConfirmationDialog` DI decision, and both-theme realization. Note `codebase_wiki`
discrepancies (di-registry `ConfirmationDialog` entry/convention; shortcuts-overlay content; password
reveal binding) for Antigravity.

### Documentation
Update the UX-25 motion pattern (or add a short note) with the reduced-motion decision per
`workflow-agent-wiki-update.md`; update `agent_wiki/log.md`. Add the dialog-DI convention note if Option
"keep direct New" is chosen.
