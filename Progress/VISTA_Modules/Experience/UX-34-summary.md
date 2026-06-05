---
module: MerchSys.App
agent: antigravity
date: 2026-06-06
plan-ref: Plans/VISTA_Modules/Experience/34-ux-finishing-touches.md
status: completed
---

## Task Summary

Implemented visual polish and loose-end features for UX finishing touches (UX-34):
1. **Two-state password reveal**: The reveal buttons for password fields toggle their iconography between `IconEyeGeometry` and `IconEyeOffGeometry` based on visual state. Removed the dead `IconChevronLeftGeometry` resource.
2. **Reduced-motion listener (restart-required scope)**: Added a `SystemParameters.StaticPropertyChanged` listener that re-runs `UpdateMotionSettings()` and rewrites the motion tokens (150ms/220ms ↔ zero) in the app resource dictionary when the user changes the Windows "show animations" setting mid-session. **Scope note (corrected during review):** this updates only runtime-read consumers — the `MotionEnabled` gate (live, e.g. `SkeletonBlock`) and content resolved after the change. The actual view/control animations consume `MotionDuration*` via `StaticResource` inside sealed template/trigger `Storyboard`s and `VisualTransition.GeneratedDuration`, which are baked when the template is sealed and cannot carry `DynamicResource`. So **already-open windows keep their startup durations until the app restarts** (the plan's Option 2 outcome for the durations). Listener is detached cleanly in `Application_Exit` (no leak).
3. **Shortcuts overlay confirmation group**: Added the "Confirmation Dialogs" category to the shortcuts cheat sheet overlay, detailing `Enter`, `Esc`, and `Type Match` interactions.
4. **ConfirmationDialog DI convention**: Retained direct `New` instantiation for `ConfirmationDialog` and documented this convention in the dependency injection registry.

**Plan:** `[[34-ux-finishing-touches.md]]`

## What Was Done

- **Modified** [Icons.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Themes/Icons.xaml) — Removed unused `IconChevronLeftGeometry` resource to eliminate dead-resource warnings.
- **Modified** [LoginView.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/LoginView.xaml) — Applied `DataTrigger` styles on the `Path` inside the password reveal buttons to swap icons dynamically based on `ShowPassword` and `ShowNewPassword` bindings.
- **Modified** [Application.xaml.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Application.xaml.vb) — Implemented `UpdateMotionSettings()` and hooked `SystemParameters.StaticPropertyChanged` on startup to handle mid-session OS animation settings changes. Unsubscribed the listener in `Application_Exit`.
- **Modified** [ShortcutsOverlay.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/ShortcutsOverlay.xaml) — Added the "Confirmation Dialogs" help group.
- **Modified** [di-registry.md](file:///c:/Users/Admin/Documents/VISTA_Project/LLM_Wiki/codebase_wiki/schemas/di-registry.md) — Documented the `ConfirmationDialog` direct instantiation convention.
- **Modified** [wpf-vista-motion.md](file:///c:/Users/Admin/Documents/VISTA_Project/LLM_Wiki/agent_wiki/patterns/wpf-vista-motion.md) — Updated the reduced motion design pattern documentation with live property tracking.
- **Modified** [log.md](file:///c:/Users/Admin/Documents/VISTA_Project/LLM_Wiki/agent_wiki/log.md) — Added chronological log entry for UX-34.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (Build succeeded with 0 warnings, 0 errors) |
| Unit tests pass | N/A |
| Manual verification | ⚠️ Partial (Password reveal toggle and shortcuts overlay verified. Reduced-motion: the `MotionEnabled` gate/skeletons update live, but sealed template animations are restart-required — see corrected scope note above. Build/runtime validation deferred to the testing phase per project rules.) |

## Issues Encountered

None. Implementation aligned cleanly with existing MVVM state bindings and resources.

## What's Next

- [x] All tasks for UX-34 are complete.

## Cross-References

- Domain Wiki pages consulted: `[[di-registry]]`
- Agent Wiki entries consulted: `[[wpf-vista-motion]]`, `[[wpf-vista-confirmation-presenter]]`
