---
test-id: POS-16-Test-6
checklist: POS-verification-checklist.md
branch: debug/POS-test-6
started: 2026-05-23T00:00
status: in-progress
---

# Debug Session — POS-16 Test 6

## Problem Statement

Verify the fiscal-year guard in `ReceiptArchivalService`. Receipts with `IssueDate` in the
current fiscal year (2026) must NEVER be archived, even if `RetentionExpiresAt` is in the past.

The service candidate query:
```vb
Where ri.RetentionExpiresAt < archiveCutoff AndAlso
      r.IssueDate.Year < currentFiscalYear
```

Both conditions must be true. Seeding receipts with `IssueDate.Year = 2026` means
`r.IssueDate.Year < currentFiscalYear` (2026 < 2026) is False — the guard should exclude them.

Approach: add `RunFiscalYearGuardTestAsync()` — seeds 20 receipts with IssueDate=2026 and
RetentionExpiresAt in the past, runs archival, asserts ReceiptsMoved=0.

## Starting State
- **Commit:** `2d9b707`
- **Build status:** clean — 0 errors, 0 warnings
- **Relevant files:**
  - `WPF_Applications\MerchSys\src\MerchSys.POS\Debug\ReceiptArchivalHarness.vb`
  - `WPF_Applications\MerchSys\src\MerchSys.App\Views\Debug\DebugMenuExtensions.vb`

## Allowed Files
- `WPF_Applications\MerchSys\src\MerchSys.POS\Debug\ReceiptArchivalHarness.vb` — add fiscal-year guard test
- `WPF_Applications\MerchSys\src\MerchSys.App\Views\Debug\DebugMenuExtensions.vb` — add Dev menu button

### Off-limits (do NOT touch)
- `SharedKernel/Events/*`
- Other modules' services/handlers
- `ReceiptArchivalService.vb` unless the guard condition is wrong

---

## Attempt Log

### Attempt 1
- **Hypothesis:** Add `RunFiscalYearGuardTestAsync()` to the harness — seeds 20 receipts with
  IssueDate=2026 and RetentionExpiresAt in the past, runs archival with batchSize=500,
  asserts ReceiptsMoved=0 and LiveRemaining=20.
- **Changed:**
  - `ReceiptArchivalHarness.vb` — add `RunFiscalYearGuardTestAsync()`
  - `DebugMenuExtensions.vb` — add "Run Fiscal-Year Guard Test" button
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
