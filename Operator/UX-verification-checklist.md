---
module: MerchSys.App
source: ux_review_report.md
originally-generated: 2026-06-05
last-synced: 2026-06-05
---

# Operator Verification Checklist — UX (Experience)

> Extracted from `ux_review_report.md` (Verification & Testing Debt, §1.1, §1.2, §1.10).
> Only **operator / manual** verification tasks are listed here — items that need a human to boot the
> app and look at the screen. Findings that need **code changes** are tracked as plans UX-31 → UX-35
> in `Plans/VISTA_Modules/Experience/`, not here.
>
> **How to use:** Do each step in order. Check the box when done. Write what you saw next to each item.
>
> **Login required (INFRA-15):** The app shows a login screen on launch. Unless a test says otherwise,
> log in as `manager` (default password `Vista2026!`; first login forces a change).
>
> **Architecture note (2026-05-28 pivot):** the app now talks to centralized **MariaDB** (XAMPP on the
> host laptop), not local SQLite. "DB down" tests mean stopping MySQL in the XAMPP Control Panel on the
> host (or disconnecting this client from the LAN), **not** deleting a local `.db` file.

### Key file locations

| What | Path |
|------|------|
| UI settings (window state + theme) | `%LOCALAPPDATA%\MerchSys\ui-settings.json` |
| WindowPlacementService | `WPF_Applications\MerchSys\src\MerchSys.App\Services\WindowPlacementService.vb` |
| UiSettingsStore | `WPF_Applications\MerchSys\src\MerchSys.App\Services\UiSettingsStore.vb` |
| MainWindow (placement hook-up) | `WPF_Applications\MerchSys\src\MerchSys.App\MainWindow.xaml.vb` |
| Reduced-motion check (startup) | `WPF_Applications\MerchSys\src\MerchSys.App\Application.xaml.vb` (`Application_Startup`, `SystemParameters.ClientAreaAnimation`) |
| Icon dictionary | `WPF_Applications\MerchSys\src\MerchSys.App\Themes\Icons.xaml` |
| Theme dictionaries | `WPF_Applications\MerchSys\src\MerchSys.App\Themes\Light.xaml`, `Dark.xaml` |
| Theme toggle service | `WPF_Applications\MerchSys\src\MerchSys.App\Services\Theming\ThemeService.vb` |

---

## UX-22 — Tooltips & Affordance

### Test 1: Tooltip chrome recolors on theme toggle

**What to do:**
1. Launch the app (F5) and log in as `manager`. Make sure the app is in **Light** theme.
2. Hover the mouse over every **icon-only** control you can find (toolbar icon buttons, the search/
   filter icons, refresh chips, the activity-rail module icons, password reveal eye button on login)
   and wait ~1 second for the tooltip to appear. Note the text and the tooltip's background/border.
3. Switch to **Dark** theme (theme toggle in the shell).
4. Hover the same controls again.

**What you should see:**
- Every icon-only control shows a tooltip with **correct, readable text** (no blank, no "PART_…",
  no binding error text).
- In Light theme the tooltip uses the light surface/foreground; in Dark theme the tooltip background
  and text **recolor** to the dark surface/foreground — it does **not** stay light-on-light or render
  with a stale hard-coded color.

- [ ] Tooltips show correct text on all icon-only controls (Light) —
- [ ] Tooltip chrome (background + text) recolors correctly in Dark —

---

## UX-23 — Window-State Persistence

> These five scenarios are the unchecked operator realization tests from the UX-23 summary.
> Tip: you can watch `%LOCALAPPDATA%\MerchSys\ui-settings.json` change between runs.

### Test 2: Resize / move → close → relaunch

**What to do:**
1. Launch the app. Resize the window to a clearly non-default size and drag it to a distinctive
   position (e.g. lower-right quadrant).
2. Close the app normally.
3. Relaunch.

**What you should see:**
- The window reopens at the **same size and position** you left it.

- [ ] Window restores last size + position after relaunch —

### Test 3: Maximize → close → relaunch

**What to do:**
1. Maximize the window. Close the app. Relaunch.

**What you should see:**
- The window reopens **maximized** (not restored to a floating size).

- [ ] Maximized state is restored after relaunch —

### Test 4: Off-screen position → fallback

**What to do:**
1. With the app closed, open `%LOCALAPPDATA%\MerchSys\ui-settings.json` in a text editor.
2. Edit the saved window position to coordinates that are off any current monitor (e.g. `Left` /
   `Top` set to a large value like `99999`). Save the file. (This simulates unplugging a second
   monitor the window was on.)
3. Launch the app.

**What you should see:**
- The window appears **on-screen** (clamped back onto a visible monitor) — it is **not** lost off the
  edge where you can't reach it.

- [ ] Off-screen saved position falls back to a visible location —

### Test 5: Delete ui-settings.json → first-run defaults

**What to do:**
1. Close the app. Delete `%LOCALAPPDATA%\MerchSys\ui-settings.json`.
2. Launch the app.

**What you should see:**
- The app launches cleanly at its **default** size/position (no crash, no error about a missing file).
- A fresh `ui-settings.json` is recreated after you move/resize and close again.

- [ ] Missing settings file → clean first-run defaults, no crash —

### Test 6: Theme toggle after window-move → both keys survive

**What to do:**
1. Launch, move/resize the window, then toggle the theme (Light↔Dark).
2. Close and relaunch.

**What you should see:**
- **Both** the window placement **and** the selected theme are restored together — toggling the theme
  did not wipe the window-state key (and vice versa). They share `ui-settings.json` without clobbering
  each other.

- [ ] Window placement and theme both persist together —

---

## UX-07 — Iconography

### Test 7: Swept icon-only controls render in both themes

**What to do:**
1. Launch in **Light** theme. Walk through every view that has icon-only buttons (toolbars, list
   action buttons, dashboard refresh chips, the activity rail).
2. Switch to **Dark** theme and repeat.

**What you should see:**
- Every icon renders as a **crisp vector glyph** — no empty squares, no missing-resource boxes, no
  clipped paths.
- Icons recolor with the theme (they use the foreground token, not a baked-in color).

> Note: `IconEyeOffGeometry` and `IconChevronLeftGeometry` are defined but not yet consumed — that is a
> known code gap handled by plan **UX-34** (two-state password reveal), not a defect to file here.

- [ ] All swept icon-only controls render correctly in Light —
- [ ] All swept icon-only controls render correctly in Dark —

---

## UX-15 — Error State Panel (DB down)

### Test 8: ErrorStatePanel appears when the database is unreachable

**What to do:**
1. On the **host** laptop, open the XAMPP Control Panel and **Stop** MySQL (or, on a client laptop,
   disconnect from the LAN / Tailscale so MariaDB can't be reached).
2. Launch the app (or, if already running, navigate to a data view and trigger a refresh).
3. Open a data-loading view (e.g. Stock Dashboard, Product Management, Transaction History).

**What you should see:**
- The view shows the **ErrorStatePanel** — a clear "couldn't load / connection problem" message with a
  **Retry** affordance — **not** a blank screen, an infinite spinner, or an unhandled-exception crash.
4. Restart MySQL (or reconnect), then click **Retry**.
- The view recovers and loads data without needing an app restart.

- [ ] Data view shows ErrorStatePanel (not blank/spinner/crash) when DB is down —
- [ ] Retry recovers the view after DB is back —

---

## UX-25 — Motion & Micro-interactions (Reduced Motion)

### Test 9: Reduced-motion is honored at startup

**What to do:**
1. Close the app. In Windows, turn **off** animations: Settings → Accessibility → Visual effects →
   **Animation effects = Off** (this drives `SystemParameters.ClientAreaAnimation`).
2. Launch the app and navigate between several views; open the command palette and a dialog.

**What you should see:**
- View transitions and micro-interactions are **disabled or minimal** (no slide/fade easing) — the app
  honors the OS "reduce motion" preference chosen before launch.

- [ ] Animations are suppressed when OS reduced-motion was on at launch —

### Test 10: Mid-session toggle behavior (document, don't fix here)

**What to do:**
1. With the app **running** (and animations currently on), turn OS animation effects **off** without
   restarting the app. Navigate between views.

**What you should see / record:**
- Per current implementation the check runs only at `Application_Startup`, so the app will **keep
  animating until restarted**. Confirm that is what happens and **record it** — this is the known
  behavior from §1.10. Whether to add a live `SystemParameters.StaticPropertyChanged` listener is a
  **code decision** tracked in plan **UX-34**; this test only documents the current behavior.

- [ ] Confirmed: mid-session OS reduced-motion change takes effect only after restart —

---

## UX-31 / UX-32 — Touched-View Boot Check (post-implementation regression pass)

> **Why this section exists.** UX-31 (FilterSummaryBar / FreshnessChip / SkeletonPanel adoption sweep)
> and UX-32 (empty-state CTAs + keyboard nav) each touched ~14 views. Their implementation summaries
> claimed *"verified all views in both themes,"* but `ExpiryMonitorView` crashed on navigation with a
> `XamlParseException` (a `TextBlock` style applied to an `<AccessText>` element). That bug is **fixed**,
> and a static sweep of all 664 `Style="{StaticResource …}"` applications found no siblings — but the
> *runtime* claim for the other touched views is now unverified by assertion. **This pass re-checks each
> touched view by actually booting it.** A `Style`/`TargetType` mismatch is a **runtime** error that
> sails through the 0/0 build gate, so "it compiled" is not evidence the view opens.
>
> Do every view in **both Light and Dark**. The first column is the must-pass: *does the view open at
> all without an exception dialog?*

### Test 11: Every UX-31/UX-32 touched view opens without a XAML/parse crash

**What to do:** Log in as `manager`, then navigate to each view below in **Light** theme, then toggle to
**Dark** and revisit. For each, confirm it **opens cleanly** (no exception dialog, no blank red error
surface), then eyeball the per-view items in the last column.

| View | Opens (Light) | Opens (Dark) | Also confirm |
|------|:---:|:---:|------|
| **ExpiryMonitorView** ⚠️ *(was the crash)* | [ ] | [ ] | Opens at all; "Near-expiry threshold:" label reads correctly + its `_t` Alt key works; filter bar count + chips; freshness chip stamps |
| StockDashboardView | [ ] | [ ] | Empty-state panel (UX-32) shows when no rows |
| TransactionHistoryView | [ ] | [ ] | FilterSummaryBar count + removable chips + clear-all; skeleton on first load only |
| APLedgerView | [ ] | [ ] | FilterSummaryBar; FreshnessChip stamps on load + updates on Refresh; skeleton on first load |
| VendorDirectoryView | [ ] | [ ] | FilterSummaryBar; FreshnessChip; skeleton on first load |
| ShrinkageView | [ ] | [ ] | FilterSummaryBar + FreshnessChip + skeleton; empty-state CTA "Record shrinkage" shows for Manager |
| TamperAuditReportView | [ ] | [ ] | FilterSummaryBar present; report keeps spinner (skeleton intentionally excluded) |
| CreditManagementView | [ ] | [ ] | FilterSummaryBar + FreshnessChip + skeleton |
| ReorderSuggestionsView | [ ] | [ ] | FilterSummaryBar + FreshnessChip + skeleton; empty-state CTA "Generate suggestions" shows for Manager |
| GoodsReceivingView | [ ] | [ ] | FreshnessChip + skeleton; empty-state info text when nothing awaiting receipt |
| DailySummaryView | [ ] | [ ] | FreshnessChip + skeleton; Alt keys (`_Daily`/`_Weekly`/`_Monthly`/`_Refresh`); Enter reloads |
| VendorCatalogView | [ ] | [ ] | FreshnessChip + skeleton; Alt keys; Enter-in-search adds, Esc closes search modal |
| SalesSummaryView | [ ] | [ ] | SkeletonPanel renders as **Cards** (not Rows) on first load |
| VatSettingsView | [ ] | [ ] | Alt keys (`_Reload`/`_Save`); Enter saves the form |

**What you should see overall:**
- Not one view throws an exception dialog on open or on theme toggle.
- Every component **recolors** with the theme (no stale light-on-light / baked color).
- Skeleton appears on **first** load only — navigating away and back to already-loaded content shows the
  data, not the skeleton.
- FilterSummaryBar count is honest (`{shown} of {total}` matches the grid); removing a chip or clear-all
  actually changes the list; the filter state **survives navigate-away-and-return** (session memory).

### Test 12: Owner role keeps read-only on the swept views

**What to do:** Log out, log back in as the **Owner** account, and open the views above that have CTAs
(ShrinkageView, ReorderSuggestionsView).

**What you should see:**
- The Manager-only empty-state CTAs (**"Record shrinkage"**, **"Generate suggestions"**) are **hidden**
  for Owner — Owner sees the informational empty state only.
- Refresh / clear-all (reads) still work for Owner; no write affordance is exposed.

- [ ] Owner sees no write CTAs on the gated views; refresh/clear-all still available —

---

## Not an operator test (noted for completeness)

- **Verification Debt #6 — automated unit tests for UX ViewModel logic** (filter session memory, undo
  time-boxing, freshness stamping, confirmation gating). This is **automated testing**, deferred to the
  dedicated testing phase per CLAUDE.md — it is not a manual operator task and is intentionally out of
  scope for this checklist.
