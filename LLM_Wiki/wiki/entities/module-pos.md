---
type: entity
title: "POS Module"
aliases: [MerchSys.POS, Point of Sale, POS]
sources: [Sources/system_plan.md, Sources/POS-Module_AcademicPaper.md]
related: [villon-farm-supply, module-inventory, module-accounting, utang-credit-system, bir-compliance]
last-updated: 2026-05-02
---

# POS Module (MerchSys.POS)

Handles all customer-facing sales operations — from cart to receipt to credit management.

## Problems → Features

| Problem | Feature |
|---|---|
| S1 — No digital sales record | Cart-based transaction interface + searchable history |
| S2 — Informal utang ledger | Digital credit accounts with balance tracking |
| S3 — No BIR OR numbering | [[bir-compliance\|OR-YYYY-XXXX]] auto-sequential receipts |
| S4 — No credit blocking | Hard block when balance > 0 (non-negotiable) |
| S5 — No daily summary | Automated daily sales summary |

## Payment Methods

| Method | Behavior |
|---|---|
| Cash | Change calculated automatically |
| GCash | Recorded as e-wallet payment (no actual transfer) |
| Bank Transfer | Recorded as bank payment (no actual transfer) |
| Credit (Utang) | Charged to customer credit account; blocked if balance > 0 |

## Data Flow

```
Customer → [Sale in Cart] → POS Module → Inventory (stock −)
                                        → Accounting (revenue, AR)
Credit Payment → POS → Accounting (AR reduced)
```

## User Roles

- **Manager:** Full access — process sales, manage credit accounts, view history
- **Owner:** Read-only — view transaction history, credit accounts

## Namespace

`MerchSys.POS`

## Source References

- [[wiki/sources/system-plan|System Plan]] — architecture, problem list
- [[wiki/sources/pos-module-paper|POS Paper]] — detailed requirements, credit system, BIR compliance
