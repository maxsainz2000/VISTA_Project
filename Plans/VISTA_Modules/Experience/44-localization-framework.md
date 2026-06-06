---
module: MerchSys.App
plan-id: UX-44
title: "Localization Framework — Resource-Based i18n (English ↔ Filipino/Taglish)"
depends-on: [UX-21]
estimated-files: 12
---

# Localization Framework — Resource-Based i18n (English ↔ Filipino/Taglish)

> **Level 3 (Pro) — item P9** of [ROADMAP-L3-pro.md](ROADMAP-L3-pro.md). A resource-based i18n layer so
> UI labels can switch language (English ↔ Filipino/Taglish), with a per-user language preference. The
> largest lift in the roadmap and genuinely optional — best done **last**, after the UI has stabilised.

## Context

The store operates in the Philippines; Taglish labels can make the tool more natural for staff. Today
labels are inline literals throughout `Views/`, and no i18n scaffolding exists. Peso/date formatting
already centralised in **UX-21 (B5)** — formatting is half of localization, so B5 is the natural
precursor. This plan adds the **framework** (resource-based strings + a language switcher) and proves it
on the core screens; full coverage follows mechanically.

It is best done last because extracting strings while the UI still churns means re-extracting; with the
UI stabilised after UX-36..43, extraction is a one-time mechanical pass.

## Prerequisites

- **UX-21** — centralised number/date/currency formatting (the formatting half of localization); the
  language layer composes with it (formatters stay culture-aware).
- Existing deps only. **No new NuGet** (.NET resource-based localization — `.resx`/`ResourceManager` —
  is built in).

## Scope

### A. Resource scaffolding
Introduce resource dictionaries/`.resx` for UI strings with a default (English) and a Filipino/Taglish
set, plus a localization helper (e.g. a markup extension or a strings provider) views bind to instead of
inline literals. Keys are stable and namespaced by area.

### B. Language preference + live switch
A language preference persisted per user (via the UX-38 prefs service if present, else the same per-
laptop store), with a switcher in preferences. Switching language updates labels — live where feasible,
or on next view-load with a clear note if a full live re-resolve is out of scope.

### C. Core-screen proof
Extract strings on the core screens (shell/nav, login, POS cart, one Accounting report) to prove the
framework end-to-end; the remaining screens are a mechanical follow-on (tracked, not necessarily all
done in this plan).

> **Out of scope:**
> - Translating *every* screen in this plan — prove the framework on core screens; the rest is a
>   mechanical follow-on.
> - Localizing data/domain content (product names, ledger data) — UI chrome only.
> - Right-to-left layout (not needed for en/fil).
> - Any business/data/contract change.

## Specification

### 0. Watch-items

1. **UI chrome only.** Localize labels/buttons/headers/tooltips — never domain data or computed values.
   No business/query/write change.
2. **Compose with UX-21 formatting.** Numbers/dates stay on the centralised culture-aware formatters;
   language selection must not fork a second formatting path.
3. **Stable, namespaced keys.** Keys are area-namespaced and stable; a missing translation falls back to
   the default language, never to a blank or the raw key in production.
4. **One persistence strategy.** Language persists via the existing prefs/per-laptop store, not a new
   settings format.
5. **Don't break bindings/mnemonics.** Replacing literals must preserve access-key mnemonics (UX-17) and
   not break existing bindings or `AutomationProperties.Name` (UX-36) — those become localized too.
6. **VB traps** — full `clr-namespace` root prefix (MC3074) for any markup extension; reserved-keyword-
   safe names; no `Await` in `Catch`/`Finally` (BC36943); `System.Console` if logging.

### 1. Constraints

- Reuse UX-21 formatting and the existing prefs/persistence; add no parallel string or settings system.
- No new NuGet. No inline hex on any touched view.
- **Role model** — Owner picks their own language over read-only screens.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check (both themes):** switching language on the core screens swaps the UI labels
  (English ↔ Filipino/Taglish), formatters still render peso/dates correctly, mnemonics and accessible
  names remain correct, and the choice persists across restart.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. A resource-based i18n framework exists (default English + Filipino/Taglish sets + a binding helper)
   with stable namespaced keys and default-language fallback.
3. A persisted per-user language preference with a switcher changes core-screen labels; numbers/dates
   stay on UX-21 formatting.
4. The core screens (shell/nav, login, POS cart, one Accounting report) are proven localized; remaining
   screens are tracked as a mechanical follow-on.
5. No domain-data localization, no RTL, no business/data/contract change; mnemonics and accessible names
   stay correct.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-44-summary.md` (template `Progress/_template.md`). Include: the
resource/`.resx` scheme and binding helper, the language-switch mechanism (live vs next-load) and
persistence, the core screens proven, the follow-on coverage list, and both-theme realization. Note
`codebase_wiki` discrepancies (resource scaffolding, language pref, extracted-string views) for
Antigravity.

### Documentation
Add `patterns/wpf-vista-localization.md` (the `.resx`/helper pattern, key-namespacing + fallback rule,
composition with UX-21 formatting, mnemonic/accessible-name preservation) per
`workflow-agent-wiki-update.md`; update `agent_wiki/index.md` + `log.md`.
