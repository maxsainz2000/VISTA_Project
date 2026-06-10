---
module: MerchSys.App
plan-id: UX-38
title: "Personalization & Workspace Memory — User Prefs, Restore-Last-View, Favorites & Recents"
depends-on: [UX-01, UX-23, UX-16]
estimated-files: 8
---

# Personalization & Workspace Memory — User Prefs, Restore-Last-View, Favorites & Recents

> **Level 3 (Pro) — item P3** of [ROADMAP-L3-pro.md](ROADMAP-L3-pro.md). Makes a daily-use tool feel
> personal: pick up where the user left off, surface favorites/recents, persist personal preferences.
> Additive — extends the existing per-laptop persistence to a small user-prefs service.

## Context

VISTA is a daily driver; every morning currently starts cold on a default screen. A considerate tool
remembers the user's workspace:

- **Restore last view** — relaunch returns to the screen the user was on.
- **Recents** — a recently-viewed list of screens.
- **Favorites** — pin frequently-used screens, surfaced in the command palette and/or sidebar.
- **Personal preferences** — theme already persists (`ThemeService`); extend the same store to a
  default module.

This reduces the cold-start tax and serves NN/g heuristic #7 (*flexibility & efficiency* — accelerators
for frequent users) and Apple HIG state-restoration. It is **additive personalization**, no business or
data change.

## Prerequisites

- **UX-01** — `Services/Theming/ThemeService.vb`, the existing per-laptop persistence mechanism the new
  user-prefs service mirrors/extends.
- **UX-23** (B7) — window-state persistence, the sibling per-laptop persistence this aligns with.
- **UX-16** — `MainWindowViewModel.AllNavigableItems` (the nav source-of-truth) and the command palette,
  which favorites/recents integrate with.
- Existing deps only. **No new NuGet.**

## Scope

### A. User-preferences service
A small `IUserPreferencesService` (+ implementation) persisting per-laptop user prefs (the same storage
strategy as `ThemeService`): default module, last-viewed screen key, recents list, favorites list.
Theme stays owned by `ThemeService`; the prefs service holds the rest and is consumed via DI.

### B. Restore-last-view
On shell load, after nav is initialized, restore the last-viewed screen (by stable key from
`AllNavigableItems`) instead of the hardcoded default — falling back to the default module if the key
no longer resolves (e.g. a removed/renamed view).

### C. Favorites & recents
Track recently-viewed screens (bounded list, most-recent-first, de-duplicated) and let the user
pin/unpin favorites. Surface both through the command palette (a "Favorites"/"Recent" group) and/or the
sidebar, reusing `NavigateCommand`. Pin/unpin persists via the prefs service.

> **Out of scope:**
> - Any business data, query, or write-path change; this stores *navigation/preference* state only.
> - Per-*user* server-side profiles (persistence is per-laptop, matching theme/window-state today).
> - Language preferences — out of scope; localization (UX-44) was dropped 2026-06-07, so no language
>   key is added here.

## Specification

### 0. Watch-items

1. **Additive, presentation/state only.** No MediatR contract, query, or write-path change; prefs are a
   new read/write-of-local-settings concern, not domain data.
2. **Stable keys, graceful fallback.** Restore/favorites key off a stable nav identifier from
   `AllNavigableItems`; if a key no longer resolves, fall back to default — never crash on a stale key.
3. **Bounded recents.** The recents list is capped and de-duplicated; it must not grow unbounded or
   leak across sessions incorrectly.
4. **One persistence strategy.** Reuse the `ThemeService`/window-state storage approach; do not invent a
   second settings file format.
5. **Role model.** Owner gets their own remembered workspace over read-only screens; no write
   affordances are introduced for Owner.
6. **VB traps** — reserved-keyword-safe names; no `Await` in `Catch`/`Finally` (BC36943); full
   `clr-namespace` root prefix (MC3074) for any new view; `System.Console` if logging.

### 1. Constraints

- Theme remains owned by `ThemeService`; the prefs service owns everything else and is DI-registered
  alongside it.
- No new NuGet. No inline hex in any new sidebar/palette markup — tokens only.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check (both themes):** relaunch restores the last screen; a removed key falls back
  cleanly; pinning a favorite persists and appears in the palette/sidebar; recents update as you
  navigate and survive restart.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. A user-preferences service persists default module, last-view, recents, and favorites per laptop,
   DI-registered and consumed by the shell.
3. Relaunch restores the last-viewed screen (with safe fallback on a stale key).
4. Favorites can be pinned/unpinned and, with recents, are surfaced via the command palette/sidebar
   using `NavigateCommand`; both persist.
5. No business/query/write change; Owner gets the same remembered-workspace behaviour read-only.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-38-summary.md` (template `Progress/_template.md`). Include: the
prefs-service shape and storage strategy, the stable-key scheme + fallback, how favorites/recents
surface, and both-theme realization (restore, fallback, pin persistence, recents). Note `codebase_wiki`
discrepancies (new service + DI registration, palette/sidebar additions) for Antigravity.

### Documentation
Add `patterns/wpf-vista-personalization.md` (the prefs-service pattern, stable nav keys, restore-with-
fallback, favorites/recents integration with the palette) per `workflow-agent-wiki-update.md`; update
`agent_wiki/index.md` + `log.md`.
