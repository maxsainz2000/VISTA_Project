---
module: MerchSys.App
plan-id: ROADMAP-L2
title: "Road to UI/UX Perfection — Level 2: Advanced (Interaction Quality)"
depends-on: [ROADMAP, ROADMAP-L1]
estimated-files: 0
---

# Level 2 — Advanced (interaction quality & considerate polish)

> Master map & governance: **[ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md)**.
> Level 2 begins once the **[Basic floor](ROADMAP-L1-basic.md)** is cleared. These items move the app
> from *correct* to *feels good*: responsiveness, motion, discoverability, reversibility, and speed
> for the highest-frequency workflow. Several lean directly on Basic primitives (a cheat-sheet needs
> the keyboard layer; the POS fast path needs the focus foundation).

Each entry: **What · Why · Research · Codebase grounding · Scope · Owner · Done-when.**

---

## A1 — Data Freshness & Manual Refresh
*Proposed slot: UX-24 · Depends-on: UX-14, UX-15*

- **What.** A small "**Updated 3m ago · ↻ Refresh**" chip in each data view's header, driven off the
  load timestamp. Past a staleness threshold (e.g. > 5 min) the chip softens to a `WarningBrush`
  tone. Refresh re-runs the existing load command.
- **Why.** This is the missing half of the concurrency story. UX-14 handles the **write-collision
  moment** ("Data changed elsewhere — refresh and retry"); nothing yet tells a user their data is
  stale **before** they act. On 4 laptops against one MariaDB, the list on screen can be minutes old
  because another client changed it.
- **Research.** NN/g heuristic #1 *Visibility of system status* — keep users informed about what's
  current. Directly complements the optimistic-concurrency mandate in `CLAUDE.md`.
- **Codebase grounding.** UX-15's three-state model (`Views/Shell/ErrorStatePanel`, the `IsBusy/
  IsError/IsEmpty` flags and `LoadDataAsync`/`RetryCommand` paths standardised across the module
  ViewModels) already owns load timing — this rides on it, adding only a `LastLoadedAt` property and
  a header chip. No new data plumbing.
- **Scope.** A `FreshnessChip` component + a `LastLoadedAt` (read-only) on the shared load surface;
  bind `↻` to the existing refresh/load command. Read-only.
- **Owner.** Same chip — arguably more valuable for the read-only Owner watching live KPIs.
- **Done-when.** Every data view shows when it last loaded and offers a one-click refresh; staleness
  is visually signalled.

---

## A2 — Motion & Micro-interactions
*Proposed slot: UX-25 · Depends-on: UX-02..05*

- **What.** Tasteful, restrained motion: a short fade/slide on content-view changes, eased hover and
  pressed states on buttons and list rows, a gentle selection transition, and expand/collapse easing.
  No bounce, no spectacle.
- **Why.** Motion is the clearest "premium" signal after typography. UX-08's non-goals explicitly
  **deferred** animation polish ("Motion/animation polish beyond what already exists in the shell") —
  this is the slot that picks it up.
- **Research.** Apple HIG *Motion* (motion should clarify, not decorate); Material motion-duration
  guidance (short, ~150–250ms, eased); perceived-quality research linking subtle motion to perceived
  polish.
- **Codebase grounding.** The shell already has gentle hover/selection (UX-02); content swaps via
  `ContentControl Content="{Binding CurrentView}"` in `MainWindow.xaml` are instant. Control styles
  live in `Themes/Controls*.xaml` where `VisualStateManager`/transitions attach.
- **Scope.** Add transitions to the content host and to the shared control templates; centralise a
  standard duration/easing token. Cosmetic; reduced-motion respected if a system preference is read.
- **Owner.** Same.
- **Done-when.** View changes and interactive states animate consistently and subtly in both themes;
  nothing feels jarring or slow.

---

## A3 — Skeleton Loaders
*Proposed slot: UX-26 · Depends-on: UX-06*

- **What.** Replace spinner-only loading with **content-shaped skeleton placeholders** (greyed card
  and row silhouettes) during first load of lists and dashboards.
- **Why.** Skeletons reduce *perceived* latency and prevent layout jump on arrival — the screen looks
  like itself immediately rather than a blank panel with a spinner.
- **Research.** Perceived-performance research (skeleton screens feel faster than spinners at equal
  wall-clock time); NN/g *response-time* guidance.
- **Codebase grounding.** `Views/Shell/BusyOverlay` (UX-06) is the current first-load affordation and
  is bound to the `IsBusy` flag standardised in UX-15. A skeleton is an alternate visual for the same
  flag — opt-in per view, falling back to `BusyOverlay` where a skeleton isn't worth authoring.
- **Scope.** A `SkeletonPanel`/shimmer component; use it on the dashboards and the largest lists,
  bound to the existing `IsBusy`. Cosmetic.
- **Owner.** Same — Owner sees the same first-load experience.
- **Done-when.** Dashboards and major lists show shaped skeletons on first load; no blank-with-spinner
  flashes on the highest-traffic screens.

---

## A4 — Keyboard-Shortcut Discoverability Overlay
*Proposed slot: UX-27 · Depends-on: UX-16, B1*

- **What.** A dismissible cheat-sheet overlay (triggered by `?` or `Ctrl+/`) listing every global
  shortcut — Ctrl+K (palette), Ctrl+1..4 (modules), Ctrl+D0 (dev tools), plus the dialog/form keys
  from B1.
- **Why.** UX-16 added powerful shortcuts but no surface to **discover** them. A cheat sheet turns
  hidden power into learnable power — the standard companion to a command palette.
- **Research.** NN/g heuristic #6 *Recognition over recall* and #7 *Flexibility & efficiency*; the
  ubiquitous "press ? for shortcuts" convention.
- **Codebase grounding.** Reuses the exact overlay pattern already built for the palette
  (`CommandPalette.xaml`: `OverlayBrush` backdrop, centered `SurfaceBrush` panel, `Esc` handling via
  `MainWindow_PreviewKeyDown`). Shortcuts are already declared in `MainWindow.xaml`
  `<Window.InputBindings>` — the overlay just lists them.
- **Scope.** A `ShortcutsOverlay` component + a `Ctrl+/` (and `?`) input binding; static content
  describing the bindings. No logic.
- **Owner.** Same — lists the role-appropriate shortcuts.
- **Done-when.** `?`/`Ctrl+/` opens a readable shortcut list; Esc closes it; it matches the palette's
  look.

---

## A5 — Search & Filter UX Maturity
*Proposed slot: UX-28 · Depends-on: UX-11, UX-16*

- **What.** Richer list controls: a live **result count** ("12 of 48"), removable **filter chips**, a
  **clear-all** affordance, a distinct "no results for *X*" empty state, and filters that are
  **remembered** per view within a session.
- **Why.** UX-11 made filter toolbars responsive and UX-16 added global search; per-view filtering is
  still bare — users can't see how much a filter narrowed results or clear it in one move.
- **Research.** NN/g faceted-search and filter-UX guidance; heuristic #1 *Visibility of system
  status* (show how many results a filter yields).
- **Codebase grounding.** `WrapPanel` filter bars exist post-UX-11 (e.g.
  `Views/Inventory/StockDashboardView.xaml`, `ProductManagementView.xaml`,
  `Views/Purchasing/PurchaseOrderListView.xaml`). The differentiated empty state reuses B3's
  `EmptyStatePanel` ("No results" vs "No data").
- **Scope.** A shared filter-bar header (count + chips + clear-all) bound to existing filter VM
  state; remember last filter in the VM. Additive read-only.
- **Owner.** Owner filters read-only reports the same way.
- **Done-when.** Filterable lists show counts and clearable chips; an empty filtered result reads
  differently from a genuinely empty dataset.

---

## A6 — Notification Actions & Undo
*Proposed slot: UX-29 · Depends-on: UX-06*

- **What.** Add **action buttons** to toasts (e.g. "View", "Undo") and a short **undo window** for
  reversible operations — most importantly soft-deletes and voids (which set `IsDeleted` rather than
  hard-deleting).
- **Why.** Undo is the safety net that lets users move fast without fear; it pairs naturally with the
  destructive-action confirms from B4 (confirm the dangerous, undo the routine).
- **Research.** NN/g heuristic #3 *User control & freedom* ("emergency exit" / undo); the
  delete-with-undo pattern as a superior alternative to confirm-everything.
- **Codebase grounding.** `INotificationService` (UX-06) already raises success/error/info/warning
  toasts; the schema's `IsDeleted` soft-delete means a delete can be reversed by clearing the flag —
  the data substrate for undo already exists.
- **Scope.** Extend the notification contract to carry an optional action/undo callback; wire undo on
  soft-delete/void commands (reverse = clear `IsDeleted`, within a time window). Additive.
- **Owner.** Owner has no reversible writes; toasts remain informational.
- **Done-when.** Routine reversible actions show an Undo toast that restores state; action toasts can
  deep-link to the relevant screen.

---

## A7 — POS Keyboard-First Fast Path
*Proposed slot: UX-30 · Depends-on: B1*

- **What.** Make the cart screen fully driveable from the keyboard/keypad: focus discipline that keeps
  the SKU/scan field hot, hotkeys for add-item, set-quantity, apply-discount, tender, and
  hold/recall, and Enter-to-commit on the tender step.
- **Why.** The POS cart is the highest-frequency screen (`SalesCartView` graded **84** in the UX-08
  rubric — the top score). Cashier speed compounds across every transaction; reaching for the mouse
  per line item is the dominant friction.
- **Research.** Fitts's Law (eliminate pointer travel on the hottest path); POS-domain convention
  (keypad/scanner-first checkout flows).
- **Codebase grounding.** `Views/POS/SalesCartView.xaml` is the target; it already uses peso
  `StringFormat` (B5 territory) and an `AmountTendered` input. Builds on B1's focus/keyboard
  foundation rather than reinventing it.
- **Scope.** Input bindings + focus management on the cart; a documented keypad flow. Behaviour of the
  underlying sale command is unchanged — only how it's invoked.
- **Owner.** Owner has no POS write access; not applicable.
- **Done-when.** A full sale (add lines → discount → tender → complete) can be rung up without the
  mouse; focus returns to the scan field after each line.

---

## Exit criteria for Level 2

Advanced is "done" when A1–A7 are shipped: data freshness is visible, the app moves smoothly, first
loads feel instant, shortcuts are discoverable, lists filter richly, routine actions are reversible,
and the cashier path is keyboard-fast. At that point the app *feels* good, not just correct, and
**[Level 3 — Pro](ROADMAP-L3-pro.md)** becomes the active level.
</content>
