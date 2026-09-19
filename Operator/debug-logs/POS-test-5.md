---
test-id: POS-16-Test-5
checklist: POS-verification-checklist.md
branch: debug/POS-test-5
started: 2026-05-23T00:00
status: resolved
---

# Debug Session — POS-16 Test 5

## Problem Statement

Verify the batch size flag in `ReceiptArchivalService`. With 100 expired receipts seeded and
`batchSize = 50`, exactly 50 receipts should be moved and `HadMoreEligible` must be `True`
(indicating the service knows there are more receipts waiting for the next batch).

The existing `ReceiptArchivalHarness.RunAsync()` uses `batchSize=500` (moves all 100).
Approach: add `RunBatchSizeTestAsync()` to the same harness module — seeds 100 expired receipts,
calls `ArchiveEligibleAsync` with `batchSize=50`, asserts `ReceiptsMoved=50` and
`HadMoreEligible=True`.

## Starting State
- **Commit:** `8a435c0`
- **Build status:** clean — 0 errors, 0 warnings
- **Relevant files:**
  - `WPF_Applications\MerchSys\src\MerchSys.POS\Debug\ReceiptArchivalHarness.vb`
  - `WPF_Applications\MerchSys\src\MerchSys.App\Views\Debug\DebugMenuExtensions.vb`

## Allowed Files
- `WPF_Applications\MerchSys\src\MerchSys.POS\Debug\ReceiptArchivalHarness.vb` — add batch-size test function
- `WPF_Applications\MerchSys\src\MerchSys.App\Views\Debug\DebugMenuExtensions.vb` — add Dev menu button

### Off-limits (do NOT touch)
- `SharedKernel/Events/*`
- Other modules' services/handlers
- `ReceiptArchivalService.vb` unless `HadMoreEligible` logic is wrong

---

## Attempt Log

### Attempt 1
- **Hypothesis:** Add `RunBatchSizeTestAsync()` to the existing harness — seeds 100 expired
  receipts, runs with `batchSize=50`, checks `ReceiptsMoved=50` and `HadMoreEligible=True`.
- **Changed:**
  - `ReceiptArchivalHarness.vb` — add `RunBatchSizeTestAsync()` and `ReceiptArchivalBatchTestResult`
  - `DebugMenuExtensions.vb` — add "Run Batch-Size Test (batchSize=50)" button + ScrollViewer wrapper (panel was not scrollable, new button was off-screen)
- **Build result:** clean — 0 errors, 0 warnings
- **Runtime result:** PASS — ReceiptsMoved=50, HadMoreEligible=True, LiveRemaining=50, ArchiveCount=50, ElapsedMs=1101. Cleanup warning (scratch DB file in use) is harmless.
- **Verdict:** ✅ fixed
- **Action:** committed as `95619c2`

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

> **⛔ HARD STOP** — If all 5 attempts failed, STOP here.

---

## Resolution

- **Status:** resolved
- **Root cause:** No bug in production code. Dev menu panel lacked a ScrollViewer, hiding the new button below the visible area.
- **Fix description:** Added `RunBatchSizeTestAsync()` to the archival harness (batchSize=50, 100 expired receipts, asserts HadMoreEligible=True). Wrapped DebugMenuView root StackPanel in a ScrollViewer.
- **Final commit:** `95619c2`
- **Agent wiki entry needed?** no
