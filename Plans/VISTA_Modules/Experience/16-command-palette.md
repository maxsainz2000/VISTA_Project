---
module: MerchSys.App
plan-id: UX-16
title: "Global Command Palette — Spotlight-Style Navigation & Search"
depends-on: [UX-06]
estimated-files: 10
---

# Global Command Palette — Spotlight-Style Navigation & Search

## Context

VISTA's shell is navigation-list-heavy: an Activity Rail of modules, a Module Detail Panel of
per-module nav items, and a content host. Reaching a screen in another module is a two-step
rail→panel→item drill every time. This plan adds a **macOS-Spotlight-style command palette**: a global
hotkey (`Ctrl+K`) opens a centered overlay with a search box that fuzzy-matches **every screen the
current role can reach** and (optionally) **business entities** (SKU / product, vendor, PO number);
selecting a result navigates there via the existing `NavigateCommand`.

It is mostly **read-only and additive** — it builds a new overlay on top of the existing navigation and
read queries, invents no new navigation scheme, and performs no writes. The one structural touch is
giving the shell a *single source of truth* for "all role-visible nav items" (see watch-item #1).

It is born on the finished theme + state epic: UX-00 tokens, UX-05 components, UX-06 busy/empty states
(the entity-search results reuse them), UX-07 icons.

Read `[[wpf-mainwindow-not-shell-window]]`, the existing `MainWindowViewModel` (per-module
`NavigationItem` collections + `NavigateCommand` + `ActiveModule`), `UX-05`/`UX-06`, and
`[[wpf-vista-state-feedback]]` first.

## Prerequisites

- The shell nav model in `MerchSys.App/ViewModels/MainWindowViewModel.vb`: the role-aware per-module
  `ObservableCollection(Of NavigationItem)` (`PurchasingItems`, `InventoryItems`, `PosItems`,
  `AccountingItems`, `DeveloperToolsItems`), the `NavigationItem` model (`DisplayName`, `ViewType`,
  `IsActive`), `NavigateCommand`, `ActiveModule`/`SelectModuleCommand`, and the role-aware builders.
- UX-05 components + UX-00 tokens + UX-07 icons for the overlay chrome.
- For entity search: the existing **read** query contracts/services (product/SKU lookup, vendor
  lookup, PO lookup). Cross-module reads go through **MediatR queries** only.
- Existing deps only: `CommunityToolkit.Mvvm`, `MediatR`. **No new NuGet** (build the overlay from a
  `Popup`/`Adorner`/overlay `Grid` — do not add a palette library).

## Scope

- **New control + VM:** a `CommandPalette` overlay (`Views/Shell/CommandPalette.xaml` + a
  `CommandPaletteViewModel`) — search box, results list, keyboard-driven selection.
- **Single nav source of truth:** a read-only aggregator on `MainWindowViewModel` exposing **all
  role-visible nav items with their owning module**, which both the palette and the existing
  dashboard-navigation code consume (today the module dashboards reach into the legacy
  `NavigationGroups` collection — see watch-item #1).
- **Global hotkey + open/close:** `Ctrl+K` (and a clickable affordance in the shell chrome) opens it;
  `Esc`/click-away closes it.
- **Entity search (optional, additive):** debounced read-only lookup of products/vendors/POs via
  MediatR queries, surfaced as result rows that navigate to the relevant operational view.

> **Out of scope:**
> - Any **write/command execution** beyond navigation. The palette navigates; it does not save, edit,
>   create POs, record payments, etc. ("Command palette" here = *navigate-to*, not *mutate*.)
> - Changing the Activity Rail / Module Detail Panel layout or the role-aware nav membership.
> - A new global search index / full-text service. Entity search reuses existing read queries with a
>   small result cap; if a needed lookup query doesn't exist cheaply, ship **nav-only** and document
>   the entity-search deferral.

## Deliverables

`CommandPalette.xaml` (+ code-behind), `CommandPaletteViewModel`, the `MainWindowViewModel` nav
aggregator + hotkey wiring, any read-only entity-lookup query reuse, the implementation summary, and a
wiki update.

## Specification

### 0. Carry-forward watch-items (the structural risks)

1. **Establish ONE nav source of truth before building on it.** `MainWindowViewModel` currently holds
   the per-module collections **and** a `NavigationGroups` collection explicitly commented "*Legacy
   flat-group collection (kept for reference; new UI uses per-module collections)*". Yet the UX-13
   `PurchasingDashboardView` navigates via `mainVm.NavigationGroups.SelectMany(...)` — so "legacy" is
   actually load-bearing, and the two representations can drift. **Do not add the palette as a third
   consumer of the legacy collection.** Add a single read-only aggregator (e.g.
   `AllNavigableItems : IReadOnlyList(Of (Module As AppModule, Item As NavigationItem))`) built from the
   role-aware per-module collections, have the palette consume it, and repoint the dashboard nav helper
   onto it too. Rebuild it in `RefreshNavigation` so it tracks role/login changes.
2. **Navigating cross-module must also set `ActiveModule`, or the rail desyncs.** `Navigate(item)` sets
   the current view + `IsActive` but does **not** change `ActiveModule` (only `SelectModuleCommand`
   does). If the palette jumps from an Inventory screen to an Accounting screen via `NavigateCommand`
   alone, the Activity Rail will still highlight Inventory and the Module Detail Panel will show the
   wrong list. The palette's selection must set `ActiveModule` to the target item's module **then**
   navigate (use the aggregator's module tag). Verify the rail + panel reflect the jump.
3. **A window-level keyboard-shortcut scheme already exists — extend it, don't reinvent it.**
   `MainWindow.xaml` has a `<Window.InputBindings>` block with `Ctrl+1..4` (module switching) and
   `Ctrl+D0` (Developer Tools), each a `KeyBinding Gesture=…` bound to a `RelayCommand`. Register the
   palette's open gesture (`Ctrl+K`) in that **same** block, wired to a command, **not** in a bespoke
   `PreviewKeyDown`/code-behind handler. `K` is free — confirm no collision with the existing gestures
   before adding. Two fall-through hazards to close: (a) those module shortcuts are window-level, so
   they keep firing while the palette is open — decide whether to suppress them or accept it (suppress
   is cleaner); and (b) the palette must handle `Esc`/`↑`/`↓`/`Enter` **locally** (mark the key event
   `Handled`) so they don't bubble up to the window bindings or the content underneath.
4. **Role-awareness is mandatory — the palette lists only what the role can reach.** Build results from
   the role-aware collections, so Owner sees only the Owner-visible (read-only) screens and Developer
   Tools never appear for Manager/Owner. Do not bypass the existing role gating. (This also keeps the
   OWASP-DA5 posture: the palette is a shortcut to already-authorized views, not a new entry point.)
5. **Entity search is read-only and async-safe.** Debounce input; cap results; run lookups off the UI
   thread via MediatR queries; never `Await` inside a `Catch`/`Finally` (BC36943). Reuse UX-06
   `BusyOverlay`/`EmptyStatePanel` semantics for the "searching…/no matches" states inside the palette.
   Selecting an entity navigates to its operational view (read-only) — it does not open an editor in
   write mode.

### 1. The overlay (`CommandPalette` + VM)

- A centered, dismissable overlay (`Popup`/adorner/overlay `Grid` over the shell content), tokenized
  surface + shadow, that does not steal the whole window. Search `TextBox` auto-focused on open.
- `CommandPaletteViewModel` (CommunityToolkit.Mvvm): a `Query` string, an `ObservableCollection` of
  result rows (each carrying a display label, an icon/section tag, and the navigation target), a
  `SelectedIndex`, an `OpenCommand`/`CloseCommand`, and an `ExecuteSelectedCommand` that performs the
  set-module-then-navigate action (watch-item #2).
- **Matching:** case-insensitive substring/fuzzy match over `DisplayName` (+ module name). Keep it
  simple and synchronous for nav items; entity rows stream in from the async lookup.

### 2. Keyboard & open/close

- Register `Ctrl+K` at the shell window (an `InputBinding`/`KeyBinding` on the main window, available
  from any screen). Add a small clickable search affordance in the shell chrome as a discoverable
  alternative.
- `Esc` and click-away close; `↑`/`↓` move selection; `Enter` executes; focus returns to the prior
  content on close.

### 3. Entity search (optional, additive)

If product/vendor/PO read queries are cheaply available, add a debounced lookup that contributes result
rows (grouped/sectioned: "Screens", "Products", "Vendors", "Purchase Orders"). Each entity row
navigates to the relevant operational view. If a lookup is not cheaply available, ship **nav-only** and
document the deferral — do not add a write path or a new index.

### 4. Constraints

- **Read-only / navigate-only** — no writes; the palette is a shortcut to existing, already-authorized
  views via the existing `NavigateCommand`.
- **Architecture** — view in `MerchSys.App`; VM in `MerchSys.App` (it orchestrates shell navigation, an
  app-level concern); cross-module entity reads via MediatR queries only; DI via `Application.xaml.vb`
  matching the existing pattern.
- **One nav source of truth** (watch-item #1) — no third consumer of the legacy `NavigationGroups`.
- **Tokens only** — UX-00 tokens / UX-05 components / UX-07 icons; no inline hex; theme-reactive.
- **No new NuGet.**
- **VB/XAML traps** — full root prefix on `clr-namespace`/`x:Class` (MC3074); no `Await` in
  `Catch`/`Finally` (BC36943); `Enumerable.Count(list, pred)` not `.Count(pred)`; reserved-keyword-safe
  variable names.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — boot in **both** themes and as **both** Manager and Owner: `Ctrl+K` opens the
  palette, typing filters, `Enter` navigates cross-module **and the Activity Rail/Module Detail Panel
  update to the target module**, `Esc` closes, and Owner sees only Owner-visible screens.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. `Ctrl+K` opens a tokenized, keyboard-navigable command palette from any screen; `Esc`/click-away
   closes it; typing fuzzy-filters the results.
3. Selecting a screen result navigates to it **and** sets the correct `ActiveModule` so the rail +
   detail panel reflect the jump (watch-item #2).
4. The palette lists only role-visible screens (Owner = reduced read-only set; Developer Tools hidden
   for Manager/Owner); it is built from a **single** nav aggregator, and the legacy `NavigationGroups`
   has no new consumer.
5. Entity search, if shipped, is read-only via MediatR queries with busy/empty states and navigates
   (no write); if deferred, the deferral is documented.
6. No writes anywhere in the palette; tokens only / hex sweep clean; theme toggle recolors the overlay.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-16-summary.md` (template `Progress/_template.md`). Include: the
nav-aggregator design + whether the dashboard nav was repointed off the legacy collection, the
set-module-then-navigate handling, the matching approach, whether entity search shipped or was deferred
(and why), the Manager-vs-Owner realization result, and `codebase_wiki` discrepancies (the new
palette view/VM + the `MainWindowViewModel` aggregator).

### Documentation
Add `patterns/wpf-vista-command-palette.md` (the single-nav-source-of-truth rule, the
set-module-then-navigate fix, role-aware result building, the read-only entity-search recipe), per
`workflow-agent-wiki-update.md`. Update `agent_wiki/index.md` + `log.md`.
