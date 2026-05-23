---
test-id: POS-16-Test-5
checklist: POS-verification-checklist.md
branch: debug/POS-test-5
started: 2026-05-23T00:00
status: in-progress
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
  - `DebugMenuExtensions.vb` — add "Run Batch-Size Test (batchSize=50)" button
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

> **⛔ HARD STOP** — If all 5 attempts failed, STOP here.

---

## Resolution

- **Status:** in-progress
- **Root cause:**
- **Fix description:**
- **Final commit:**
- **Agent wiki entry needed?**
