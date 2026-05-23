---
test-id: POS-16-Test-4
checklist: POS-verification-checklist.md
branch: debug/POS-test-4
started: 2026-05-23T00:00
status: in-progress
---

# Debug Session — POS-16 Test 4

## Problem Statement

Verify that `ReceiptArchivalService.ArchiveEligibleAsync` moves exactly 100 expired receipts and
leaves 100 in-window receipts untouched. The checklist calls for:
- 100 `Pos_OfficialReceipts` rows with `RetentionExpiresAt` in the past AND `IssueDate` from a
  previous fiscal year (2015) — must be archived.
- 100 `Pos_OfficialReceipts` rows with `RetentionExpiresAt` in the future — must NOT be archived.

**Known risk:** The archival service's candidate query uses `ToListAsync()` on a LINQ projection
that includes full entity objects (`r` and `ri`). This matches the known EF Core 10 VB.NET bug
`[[efcore-vbnet-tolistasync-entity-empty]]` — full entity materialization silently returns an empty
list. If that bug is present here, `ReceiptsMoved` will be 0 instead of 100.

Approach: self-contained harness in a scratch SQLite DB. `Pos_ArchivalSession` table is created
manually after `EnsureCreatedAsync()` since it is seeded by `DatabaseInitializer`, not EF Core.

## Starting State
- **Commit:** `a5eeea1`
- **Build status:** clean — 0 errors, 0 warnings
- **Relevant files:**
  - `WPF_Applications\MerchSys\src\MerchSys.POS\Services\Archival\ReceiptArchivalService.vb`
  - `WPF_Applications\MerchSys\src\MerchSys.POS\Debug\ReceiptArchivalHarness.vb` (new)
  - `WPF_Applications\MerchSys\src\MerchSys.App\Views\Debug\DebugMenuExtensions.vb`

## Allowed Files
- `WPF_Applications\MerchSys\src\MerchSys.POS\Debug\ReceiptArchivalHarness.vb` — new harness file
- `WPF_Applications\MerchSys\src\MerchSys.App\Views\Debug\DebugMenuExtensions.vb` — add Dev menu button
- `WPF_Applications\MerchSys\src\MerchSys.POS\Services\Archival\ReceiptArchivalService.vb` — fix candidate query if ToListAsync bug confirmed

### Off-limits (do NOT touch)
- `SharedKernel/Events/*` — shared contracts, not the bug source
- Other modules' services/handlers

---

## Attempt Log

### Attempt 1
- **Hypothesis:** Wire a scratch-DB harness to the Dev menu. Run 100 expired + 100 in-window
  receipts through `ArchiveEligibleAsync`. Expect ReceiptsMoved=100, LiveRemaining=100.
  If the EF Core ToListAsync bug applies, ReceiptsMoved will be 0.
- **Changed:**
  - `MerchSys.POS/Debug/ReceiptArchivalHarness.vb` — new harness
  - `MerchSys.App/Views/Debug/DebugMenuExtensions.vb` — new Dev menu button
- **Build result:**
- **Runtime result:**
- **Verdict:**
- **Action:**

---

### Attempt 2
- **Hypothesis:**
- **Changed:**
- **Build result:**
- **Runtime result:**
- **Verdict:**
- **Action:**

---

### Attempt 3
- **Hypothesis:**
- **Changed:**
- **Build result:**
- **Runtime result:**
- **Verdict:**
- **Action:**

---

### Attempt 4
- **Hypothesis:**
- **Changed:**
- **Build result:**
- **Runtime result:**
- **Verdict:**
- **Action:**

---

### Attempt 5
- **Hypothesis:**
- **Changed:**
- **Build result:**
- **Runtime result:**
- **Verdict:**
- **Action:**

> **⛔ HARD STOP** — If all 5 attempts failed, STOP here. Write the resolution section below and escalate to the operator.

---

## Resolution

- **Status:** in-progress
- **Root cause:**
- **Fix description:**
- **Final commit:**
- **Agent wiki entry needed?**

### Summary for operator (if escalated)
