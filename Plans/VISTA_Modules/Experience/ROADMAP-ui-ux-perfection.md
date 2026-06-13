---
module: MerchSys.App
plan-id: ROADMAP
title: "Road to UI/UX Perfection of VISTA App — Master Roadmap & Governance"
depends-on: [UX-16]
estimated-files: 0
---

# Road to UI/UX Perfection of VISTA App

> **This is a standing roadmap, not an implementable batch.** It is the canonical map of every
> remaining UI/UX improvement, organised into three levels — **Basic → Advanced → Pro**. Its
> purpose is to replace the recurring "what's the next UI/UX improvement?" question with a fixed
> backlog: pick the next unclaimed slot, generate its plan, ship it, repeat. Every entry is grounded
> in the **actual** VISTA codebase and in named UX research, so the map cannot drift into duplicated
> or invented work.

## Why this exists (governance)

The macOS UX epic shipped 16 plans (UX-01 → UX-16) by repeatedly asking "what's next?" after each
one. That loop has two failure modes:

1. **Duplication** — proposing something already shipped (e.g. re-pitching empty-state handling when
   `EmptyStatePanel` already exists).
2. **Invention / hallucination** — proposing something that *sounds* good but isn't grounded in the
   codebase or in evidence.

A fixed, written backlog removes both. The rule going forward:

> **Do not brainstorm a new UI/UX item from scratch.** Consult this roadmap, claim the
> next unclaimed slot at the appropriate level, and generate its plan. Only add a *new* entry here
> (with research + codebase grounding) when a genuinely novel need is discovered — and add it to the
> map **before** implementing it.

When a roadmap item is turned into a real plan, it is assigned the next free `UX-NN` number in
`Plans/VISTA_Modules/Experience/`, implemented, and its roadmap entry is checked off. The roadmap
entries below carry a **proposed slot** only as a hint; the authoritative number is assigned at
plan-generation time.

## You are here — shipped baseline (UX-01 → UX-16)

The roadmap is honest about coverage. The following is **done** and must not be re-proposed:

| Area | Delivered by | Status |
|------|-------------|--------|
| Central theme tokens (color + structure) | UX-01 | ✅ `Themes/Tokens.xaml`, `Light.xaml`, `Dark.xaml` |
| Light/Dark toggle (live swap, persisted per laptop) | UX-01 | ✅ `Services/Theming/ThemeService.vb` |
| Inter typeface + type ramp | UX-01 | ✅ |
| Shell reskin (MainWindow, ActivityRail, ModuleDetailPanel) | UX-02 | ✅ |
| Implicit control styles (Button/TextBox/ListBox/TabControl/DataGrid/ScrollBar) | UX-03 | ✅ `Themes/Controls*.xaml` |
| View migration — inline hex → tokens (zero hardcoded color) | UX-04 | ✅ |
| Shared component library | UX-05 | ✅ `Themes/Components.xaml` |
| State & feedback (Busy / Empty / Conflict / validation / notifications) | UX-06 | ✅ `Views/Shell/BusyOverlay`, `EmptyStatePanel`, `ConcurrencyConflictPrompt`; `INotificationService` |
| Vector icon system | UX-07 | ✅ `Themes/Icons.xaml` |
| Dashboard metric hierarchy (40-30-20-10) | UX-09, UX-10 | ✅ |
| Layout-resilience sweep (ScrollViewer + WrapPanel filters) | UX-11 | ✅ |
| Trend / delta indicators (sparklines) | UX-12 | ✅ `Views/Shell/Sparkline`, `DeltaIndicator` |
| Purchasing dashboard | UX-13 | ✅ |
| Concurrency-conflict consolidation (the write-collision moment) | UX-14 | ✅ `SharedKernel/Persistence/ConcurrencyHelper.vb` |
| Three-state load model + `ErrorStatePanel` | UX-15 | ✅ `Views/Shell/ErrorStatePanel` |
| Spotlight command palette (Ctrl+K) | UX-16 | ✅ `Views/Shell/CommandPalette`, `ViewModels/Shell/CommandPaletteViewModel.vb` |

Connection health is also covered (`Services/ConnectionHealthMonitor.vb`, `ConnectionStatusIndicator`)
and session/idle timeout exists — these are **not** roadmap items.

## The three levels

The levels are about **what kind of value** the work delivers, not difficulty alone:

| Level | Definition | Bar it clears |
|-------|-----------|---------------|
| **Basic** | The table-stakes WPF UX correctness floor — the things a competent line-of-business WPF app is *expected* to get right. Mostly small, high-certainty, broad-reach. | "Nothing here is broken, surprising, or unlearnable." |
| **Advanced** | Interaction *quality* and considerate polish beyond correctness — the app feels responsive, fluid, and helpful, not just correct. | "This feels good to use, not just usable." |
| **Pro** | Differentiators and deep refinement — accessibility completeness, performance at scale, personalization, and the craft details that separate a good app from an exceptional one. | "This is the best version of itself." |

Each level lives in its own file:

- **[Basic — ROADMAP-L1-basic.md](ROADMAP-L1-basic.md)**
- **[Advanced — ROADMAP-L2-advanced.md](ROADMAP-L2-advanced.md)**
- **[Pro — ROADMAP-L3-pro.md](ROADMAP-L3-pro.md)**

## Consolidated backlog (the map)

Proposed slots are suggestions; dependencies are real. Recommended execution: finish **Basic** before
**Advanced**, and **Advanced** before **Pro**, because each level leans on primitives the prior level
establishes (e.g. Pro accessibility builds on the Basic keyboard/focus floor).

### Level 1 — Basic (correctness floor)

| Slot | Item | Depends-on | Reach |
|------|------|-----------|-------|
| B1 | Keyboard & Focus Foundation (tab order, focus visuals, `IsDefault`/`IsCancel`, mnemonics, Enter-to-submit) | UX-16 | App-wide |
| B2 | Form & Input UX Standards (label/required/inline-error placement, numeric masking, error summary) | B1, UX-06 | All forms |
| B3 | Actionable Empty States (add a CTA command to `EmptyStatePanel`, role-aware) | UX-06 | All lists |
| B4 | Confirmation & Destructive-Action Dialogs (standard confirm pattern for delete/void/submit) | B1 | All write paths |
| B5 | Centralised Number / Date / Currency Formatting (peso, dates, qty; grid right-alignment) | — | App-wide |
| B6 | Tooltips & Affordance (icon-only controls become learnable) | UX-07 | App-wide |
| B7 | Window-State Persistence (remember size/position/maximized per laptop) | UX-01 | Shell |

### Level 2 — Advanced (interaction quality)

| Slot | Item | Depends-on | Reach |
|------|------|-----------|-------|
| A1 | Data Freshness & Manual Refresh chip (closes the concurrency narrative) | UX-14, UX-15 | All data views |
| A2 | Motion & Micro-interactions (view transitions, hover/press/selection easing) | UX-02..05 | App-wide |
| A3 | Skeleton Loaders (content-shaped first-load placeholders vs spinner) | UX-06 | Lists/dashboards |
| A4 | Keyboard-Shortcut Discoverability Overlay (`?` / Ctrl+/ cheat sheet) | UX-16, B1 | Shell |
| A5 | Search & Filter UX Maturity (result counts, filter chips, clear-all, remembered filters) | UX-11, UX-16 | Filterable views |
| A6 | Notification Actions & Undo (toast actions + undo for soft-delete/void) | UX-06 | Write paths |
| A7 | POS Keyboard-First Fast Path (cashier hotkeys, keypad/barcode focus discipline) | B1 | POS |

### Level 3 — Pro (differentiators & depth)

| Slot | Item | Depends-on | Reach |
|------|------|-----------|-------|
| P1 | Full Accessibility / WCAG 2.2 AA (`AutomationProperties`, SR names, focus traps) | B1 | App-wide |
| P2 | Performance & Perceived Performance (UI virtualization, async-everywhere, optimistic UI) | UX-15 | Large lists |
| P3 | Personalization & Workspace Memory (per-user last-view/favorites/recents) | UX-01, B7 | App-wide |
| ~~P4~~ | ~~System-Aware Theming~~ — dropped 2026-06-06 (adds noise; Light/Dark kept) | — | — |
| ~~P5~~ | ~~Density Modes~~ — dropped 2026-06-06 (adds noise) | — | — |
| P6 | Advanced Data Visualization (interactive charts, hover tooltips, drill-down, period selectors) | UX-12 | Dashboards |
| P7 | Print & Export UX (BIR Official-Receipt template, PDF/CSV export, print preview) | — | POS, Accounting |
| P8 | Living Design-System Gallery (token/component/state catalogue in Developer Tools) | UX-05, UX-07 | Dev Tools |
| ~~P9~~ | ~~Localization Framework (resource-based i18n; Taglish/Filipino labels)~~ — dropped 2026-06-07 (descoped; English-only UI kept) | — | — |
| P11 | Comparative Income Statement & Report Readability (prior-period column + Δ, P&L legibility) | UX-12 | Accounting |
| P12 | Severity-Aware Insight Banners (tone the "What This Means" callout to the signal) | UX-06, UX-07 | Accounting |
| P13 | Animated Login Experience (time-of-day farm scene + reactive carabao mascot + CapsLock/shake feedback) | UX-01, UX-07, UX-25 | Login (every user, every session) |
| P13a | Login Scene Redesign — dusk storefront hero shot + tindera avatar + frosted-glass card (replaces P13's artwork; keeps its machinery) | UX-48 | Login (every user, every session) |
| P13b | Login Scene Raster Hero — vector facade → licensed CC0 cozy-pixel storefront over the kept gradient sky, dusk-graded (fixes the medium; keeps the machinery) | UX-49 | Login (every user, every session) |

## Research basis (binding evidence, not opinion)

Every item cites at least one named source in its detail entry. The roadmap as a whole rests on:

- **Nielsen Norman Group (NN/g)** — 10 Usability Heuristics (esp. #1 *Visibility of system status*,
  #3 *User control & freedom*, #5 *Error prevention*, #6 *Recognition over recall*); F-pattern
  eye-tracking; the 5–9 working-memory ceiling. (Already applied in the UX-08 dashboard rubric.)
- **WCAG 2.2 Level AA** — §2.1 keyboard operable, §2.4.7 focus visible, §1.4.3 contrast,
  §3.3 input assistance. (UX-00 already mandates the 4.5:1 contrast floor.)
- **Apple Human Interface Guidelines** — motion, depth, and restraint (the macOS-inspired north star
  of this epic; see UX-00).
- **Microsoft WPF / Fluent guidance** — keyboard & focus model, UI Automation, virtualization,
  implicit-style theming.
- **Fitts's Law & Hick's Law** — target size/distance and choice-count efficiency (drives B1, A7).
- **Perceived-performance research** — skeleton screens and optimistic UI reduce *felt* latency even
  when wall-clock time is unchanged (drives A3, P2).
- **In-repo prior research** — `Research/UX/macos-theme-feasibility-2026-06-02.md`, and the
  research-derived layout rubric in `Plans/VISTA_Modules/Experience/08-dashboard-ia-overview.md`.

## Cross-cutting rules (inherited by every roadmap item, per UX-00 / UX-08)

1. **`DynamicResource` for all color/brush references**; structure tokens may be `StaticResource`.
2. **No inline hex in `Views/`** — the UX-04 hex sweep stays at zero. New brushes become tokens.
3. **Behaviour discipline by tier.** Basic/Advanced cosmetic-and-interaction items must not alter
   business logic, MediatR contracts, concurrency, or write paths; any data an item needs is
   **additive read-only** (new VM property / query contract), never a change to an existing one.
4. **VB.NET / XAML traps** — full root prefix on every `clr-namespace` (MC3074); no `Await` in
   `Catch`/`Finally` (BC36943); a `<Setter>` targets a `DependencyProperty` only; never feed a brush
   token into a `Color` property; use the raw `MySqlConnector` reader (not `ToListAsync` on entity
   queries) for any new read path. See `LLM_Wiki/agent_wiki/`.
5. **Role model** — Manager = full, Owner = read-only. Every interactive item must define its
   Owner behaviour (typically: no write affordances, but full read/observe access).
6. **Build gate** — `dotnet build WPF_Applications/MerchSys/MerchSys.slnx` must finish **0 errors,
   0 warnings**. If it fails, document errors in `Progress/` and stop (per CLAUDE.md).
7. **Realization check** — a clean build does not prove a `Style`/`Geometry`/template resolves; each
   plan's summary confirms the touched screens were booted in **both** themes.
8. **No new NuGet packages** unless the specific plan explicitly authorises one.

## Output requirements

This roadmap has **no code deliverable**. When an item is implemented, its real `UX-NN` plan produces
a summary at `Progress/VISTA_Modules/Experience/UX-NN-summary.md` (template `Progress/_template.md`),
and the corresponding roadmap entry is checked off in the relevant level file.

## Non-goals (out of scope for the entire roadmap)

- Custom window chrome / traffic-light buttons (owner declined — UX-00).
- macOS vibrancy / Acrylic / Mica backdrops.
- New **business** features or KPI definitions not already computed by a service (the roadmap is
  UI/UX; print/export and i18n are presentation layers over existing data).
- Any data-access, `DbContext`, sync, or business-rule change.
- Touching `LLM_Wiki/wiki/` or `LLM_Wiki/codebase_wiki/` (read-only per CLAUDE.md).
</content>
</invoke>
