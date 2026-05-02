---
type: concept
title: "Utang Credit System"
aliases: [utang, credit system, informal credit, customer credit]
sources: [Sources/POS-Module_AcademicPaper.md]
related: [module-pos, bir-compliance, module-accounting]
last-updated: 2026-05-02
---

# Utang Credit System

## Definition

**Utang** is the Philippine practice of informal credit where farmers purchase agricultural inputs on credit and settle their balances after harvest. It is culturally embedded in agricultural communities and sustains commerce during non-harvest periods.

## Current State (Pre-VISTA)

- Credit tracked in informal handwritten ledger
- Balances frequently lost or disputed
- No mechanism to prevent stacking credit on unpaid accounts
- 15–20% discrepancy between recorded and actual balances (Garcia & Santos, 2023)

## VISTA Implementation

| Feature | Behavior |
|---|---|
| Digital Credit Account | One per customer — running balance, payment history |
| **Credit Blocking** | Hard block when outstanding balance > 0 — **non-negotiable** |
| Payment Recording | Partial or full payments recorded against the account |
| AR Visibility | Total AR displayed in [[module-accounting\|Accounting]] dashboard |
| Collection Flags | Overdue accounts flagged for follow-up |

> [!IMPORTANT]
> Credit blocking is a **zero-tolerance** system rule. If a customer has any unpaid balance > 0, the system disables the sale completion button for credit payment. This is enforced at the transaction level, not by managerial discretion.

## Prevalence

60–70% of rural agricultural retail transactions in the Philippines involve some form of credit extension (Llanto & Badiola, 2022).

## Source References

- [[wiki/sources/pos-module-paper|POS Paper]] — S2, S4, credit blocking rule, AR management
