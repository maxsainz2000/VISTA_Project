---
module: MerchSys.App
source: ux_review_report.md
originally-generated: 2026-06-05
last-synced: 2026-06-08
scope-extended: 2026-06-08 (broadened beyond the report into the single UX operator checklist; UX-33 → UX-43 appended)
---

# Operator Verification Checklist — UX (Experience)

> Originally extracted from `ux_review_report.md` (Verification & Testing Debt, §1.1, §1.2, §1.10);
> **broadened 2026-06-08** into the single UX operator checklist — it now also covers the manual
> verification owed by the post-report plans **UX-33 → UX-43** (their summaries each marked operator
> verification "pending"). Only **operator / manual** verification tasks are listed here — items that
> need a human to boot the app and look at the screen. The code for every item below is already
> implemented; this is the realization/verification pass, not a code backlog.
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
| UI settings (window state + theme + personalization) | `%LOCALAPPDATA%\MerchSys\ui-settings.json` — now also holds `lastViewKey`, `favoriteKeys`, `recentKeys` (UX-38) |
| WindowPlacementService | `WPF_Applications\MerchSys\src\MerchSys.App\Services\WindowPlacementService.vb` |
| UiSettingsStore | `WPF_Applications\MerchSys\src\MerchSys.App\Services\UiSettingsStore.vb` |
| MainWindow (placement hook-up) | `WPF_Applications\MerchSys\src\MerchSys.App\MainWindow.xaml.vb` |
| Reduced-motion (startup + live) | `WPF_Applications\MerchSys\src\MerchSys.App\Application.xaml.vb` — `Application_Startup` reads `SystemParameters.ClientAreaAnimation`; a `SystemParameters.StaticPropertyChanged` listener re-runs `UpdateMotionSettings()` mid-session (UX-34), detached in `Application_Exit` |
| Icon dictionary | `WPF_Applications\MerchSys\src\MerchSys.App\Themes\Icons.xaml` |
| Theme dictionaries | `WPF_Applications\MerchSys\src\MerchSys.App\Themes\Light.xaml`, `Dark.xaml` |
| Theme toggle service | `WPF_Applications\MerchSys\src\MerchSys.App\Services\Theming\ThemeService.vb` |
| Personalization (favorites/recents/last-view) | `WPF_Applications\MerchSys\src\MerchSys.App\Services\UserPreferencesService.vb` |
| Design Gallery (Developer-only) | `WPF_Applications\MerchSys\src\MerchSys.App\Views\DeveloperTools\DesignGalleryView.xaml` |

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

> Note: the two-state password-reveal icon (`IconEyeOffGeometry`) is now consumed and the dead
> `IconChevronLeftGeometry` was removed (both by **UX-34**) — verify the reveal toggle in **Test 15**.

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

### Test 10: Mid-session reduced-motion toggle (now partially live — UX-34)

**What to do:**
1. With the app **running** (and animations currently on), turn OS animation effects **off** without
   restarting the app. Navigate between views; open a data view that fetches (to trigger a skeleton).

**What you should see / record:**
- **UX-34 added a live `SystemParameters.StaticPropertyChanged` listener**, so the change is now
  **partially live**: runtime-read consumers react immediately — the `MotionEnabled` gate and the
  skeleton shimmer stop animating without a restart.
- **Sealed template/trigger animations** (view-transition fades, `VisualTransition` durations) are baked
  when their template is sealed, so they **keep their startup durations until the app restarts**. This
  split is the expected behavior — record which interactions stopped live and which needed a restart.

- [ ] Skeleton shimmer / `MotionEnabled` gate stop animating immediately on the mid-session OS change —
- [ ] Sealed template transitions keep animating until restart (expected) —

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

# Post-report plans (UX-33 → UX-43) — appended 2026-06-08

> The sections above cover the original `ux_review_report` debt (UX-07 … UX-32). The sections below
> cover the manual verification owed by the plans that landed **after** the report. All code is
> implemented; each plan's summary marked operator verification "pending." Same rules: log in as
> `manager` unless a test says otherwise, and do each check in **both Light and Dark**.

## UX-33 — Destructive-Action Guardrail Parity

### Test 13: Typed-confirmation on irreversible actions

**What to do:**
1. **Void a sale:** POS → Transaction History → select a transaction → **Void Transaction**. Try
   clicking confirm without typing; then type `VOID` exactly.
2. **Record shrinkage:** Inventory → Shrinkage → record a shrinkage; in the dialog type `RECORD`.

**What you should see:**
- The confirm button stays **disabled** until the typed text matches exactly (`VOID` / `RECORD`);
  wrong or partial text keeps it disabled. Esc cancels with no change.
- After confirming, the action applies (the transaction shows voided; stock is reduced).

- [ ] Void requires typing `VOID`; confirm disabled until exact match —
- [ ] Shrinkage record requires typing `RECORD`; confirm disabled until exact match —

### Test 14: Confirm + Undo on reversible actions

**What to do:**
1. **Product deactivate:** Inventory → Product Management → deactivate a product → watch for the Undo
   toast → click **Undo** within the window.
2. **Credit block:** POS → Credit Management → select an account → **Block Credit** (confirm) → Undo
   toast → Undo. (Unblock requires the account balance = 0.)
3. **PO draft delete:** Purchasing → Purchase Orders → delete a *draft* PO (confirm) → Undo toast → Undo.

**What you should see:**
- Each shows a confirmation first (block / PO) or an immediate Undo toast (deactivate); clicking
  **Undo** within the time window restores the prior state (product re-activated, account unblocked,
  draft restored). Letting the toast expire keeps the change.

- [ ] Product deactivate → Undo restores active state —
- [ ] Credit block → confirm, then Undo restores unblocked —
- [ ] PO draft delete → confirm, then Undo restores the draft —

---

## UX-34 — Finishing Touches

### Test 15: Two-state password reveal

**What to do:** On the login screen, click the eye button in a password field; click it again. Repeat
on the change-password flow's new-password field.

**What you should see:** The icon toggles between the eye and eye-off glyph in sync with whether the
password text is shown or hidden.

- [ ] Reveal eye icon toggles eye ↔ eye-off in sync with visibility (both fields) —

### Test 16: Shortcuts overlay — Confirmation Dialogs group

**What to do:** Open the shortcuts overlay (the shortcuts hotkey / `?`). Scroll to the **Confirmation
Dialogs** group.

**What you should see:** A "Confirmation Dialogs" section explaining Enter (confirm), Esc (cancel), and
the type-to-confirm exact-match behavior (matches Test 13).

- [ ] Shortcuts overlay shows a "Confirmation Dialogs" group with Enter / Esc / type-match —

> Reduced-motion mid-session behavior (also UX-34) is verified in **Test 10** above.

---

## UX-36 — Accessibility (WCAG 2.2 AA)

### Test 17: Screen-reader naming

**What to do:** Start **Narrator** (Ctrl+Win+Enter). Tab through: the activity-rail module icons, the
login password-reveal button, the freshness Refresh chip, the SalesCart payment/remove buttons, the AP
ledger rows, and the VAT Payable KPI tile. Turn Narrator off when done.

**What you should see:** Narrator announces a **meaningful name** for each icon-only / row control — not
just "button," not the raw glyph.

- [ ] Narrator announces meaningful names on swept icon-only controls & rows —

### Test 18: Modal focus traps

**What to do:** Open each modal and press **Tab / Shift+Tab** repeatedly: the Command Palette (Ctrl+K),
a Confirmation Dialog (e.g. start a void), and the Concurrency Conflict prompt (if reproducible).

**What you should see:** Focus **cycles within** the modal — Tab never lands on a control behind the
overlay. Esc still cancels. On close, focus returns to the control that opened it.

- [ ] Tab cannot escape any of the 3 modals; focus restores to the invoker on close —

---

## UX-37 — Performance & Perceived Performance

### Test 19: Virtualized smooth scrolling

**What to do:** Open a long grid (Transaction History or AP Ledger with many rows) and scroll fast from
top to bottom.

**What you should see:** Scrolling is smooth — no per-row realization stutter, no blank-then-pop rows.

- [ ] Long grids scroll smoothly (no virtualization stutter) —

### Test 20: Optimistic VAT save + concurrency rollback

**What to do:**
1. Accounting/POS → VAT Settings → change a value → **Save**: status shows "Saving…" immediately, then
   "VAT settings saved."
2. **Conflict path (2 clients):** open VAT Settings on two clients; save on client A, then save a
   different value on client B → on B, choose **Refresh** at the conflict prompt.

**What you should see:** On B, the form **reverts to its pre-save values**, the conflict prompt appears
("Data changed elsewhere…"), and Refresh loads the authoritative DB values — no silent overwrite.
Choosing Cancel at the conflict leaves "Not saved — data changed elsewhere" (status is not stuck on
"Saving…").

- [ ] Optimistic "Saving…" appears instantly; success reconciles to "saved" —
- [ ] Concurrency conflict → form rolls back, prompt shown, Refresh loads DB values —

---

## UX-38 — Personalization & Workspace Memory

### Test 21: Restore last view

**What to do:** Navigate to a non-default screen (e.g. AP Ledger). Close the app. Relaunch and log in.

**What you should see:** After login the app reopens on the **last-viewed screen** (AP Ledger), not the
default dashboard. If that screen is role-excluded or removed, it falls back to the default cleanly.

- [ ] App restores the last-viewed screen after relaunch —
- [ ] Stale / role-excluded last-view falls back to default without error —

### Test 22: Favorites pin persistence

**What to do:** Pin a couple of screens via the sidebar ☆ button. Open the command palette (empty
query) → check the **Favorites** group. Log out and back in.

**What you should see:** ★ state persists across logout/restart; favorites appear in the palette
zero-state Favorites group and navigate correctly.

- [ ] Pinned favorites persist across logout/restart and show in the palette —

### Test 23: Recents

**What to do:** Navigate through several screens. Open the command palette (empty query) → **Recent**
group.

**What you should see:** Recently-viewed screens, most-recent-first, de-duplicated, bounded (~10),
excluding the screen you're currently on; survives restart.

- [ ] Recent list is correct, bounded, de-duplicated, and survives restart —

---

## UX-41 — Advanced Data Visualization

### Test 24: Sparkline hover tooltips

**What to do:** Hover the sparkline bars on the Owner Dashboard **Revenue Trend** card, and on a
Financial Overview sparkline.

**What you should see:** Revenue Trend bars show a "MMM d" period label **above** the value; the
unlabeled Financial Overview sparkline shows the value only (the label row collapses).

- [ ] Revenue Trend tooltip shows date label + value; plain sparkline shows value only —

### Test 25: KPI drill-down

**What to do:** On the Owner Dashboard, click **View →** on each of the four secondary KPI cards
(Purchasing, Inventory, Sales, Accounting). Test as both Owner and Manager.

**What you should see:** Each navigates to the correct detail view (PurchasingDashboard / StockDashboard
/ SalesSummary / FinancialOverview) — works in both roles.

- [ ] Each "View →" navigates to the correct detail view (Owner + Manager) —

### Test 26: Period selector

**What to do:** On the Revenue Trend card, switch 7d / 30d / 90d.

**What you should see:** The sparkline re-renders for the chosen window with daily date labels and a
zero-filled (gap-free) x-axis; recolors correctly in both themes.

- [ ] 7 / 30 / 90-day toggle re-renders the trend correctly in both themes —

---

## UX-42 — Print & Export

### Test 27: BIR Official-Receipt print

**What to do:** Complete a POS sale → in the receipt preview click **Print Official Receipt** → use the
print dialog (a real printer, or "Microsoft Print to PDF").

**What you should see:** The OR shows business name / address / TIN, VAT status, OR number, date,
itemized lines, totals, and the VAT disclosure block (VATable / VAT-Exempt / Zero-Rated / Output VAT).

- [ ] OR prints with all BIR-required fields and the correct VAT block —

### Test 28: Income Statement CSV / PDF export

**What to do:** Accounting → Income Statement → **Export CSV** (open in Excel/LibreOffice) and **Export
PDF** (preview window → Print / Save As).

**What you should see:** CSV opens with clean columns — a product name containing a `"` (e.g. `2" PVC
pipe`) does not break alignment; figures match the on-screen values. The PDF/preview text matches the
report.

- [ ] Income Statement CSV opens cleanly; values match the screen —
- [ ] Export PDF preview shows the correct report; Print / Save As work —

### Test 29: VAT Relief Report preview

**What to do:** Accounting → VAT Relief Report → **Export PDF** (preview) and **Export CSV**. Also try
clicking export immediately on open, before data loads.

**What you should see:** The preview shows the current-month three-bucket sales/purchases summary plus
the trailing 12-month trend; clicking export before load is null-guarded (no crash).

- [ ] VAT Relief preview / CSV show correct data; no crash when exported pre-load —

---

## UX-43 — Living Design-System Gallery (Developer only)

### Test 30: Gallery renders and recolors

**What to do:** Log in with the **Developer** account (the gallery is hidden from Manager/Owner). Open
Developer Tools → **Design Gallery**. Toggle the theme (Ctrl+T).

**What you should see:** Section A (tokens: color swatches, type ramp, spacing, radii, motion), Section
B (shared components in their states), Section C (all 20 icons). Every swatch and component **recolors
live** on the theme toggle — any swatch that fails to recolor flags a token-key mismatch.

- [ ] Gallery is visible only to the Developer role; all three sections render —
- [ ] Every swatch / component recolors live on the Light ↔ Dark toggle —

---

## Not an operator test (noted for completeness)

- **Verification Debt #6 — automated unit tests for UX ViewModel logic** (filter session memory, undo
  time-boxing, freshness stamping, confirmation gating). This is **automated testing**, deferred to the
  dedicated testing phase per CLAUDE.md — it is not a manual operator task and is intentionally out of
  scope for this checklist.
