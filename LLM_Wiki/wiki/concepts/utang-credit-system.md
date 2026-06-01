---
type: concept
title: "Utang Credit System"
aliases: [utang, credit system, informal credit, customer credit]
sources: [Sources/POS-Module_AcademicPaper.md, Sources/Villon_Interview_Populated.md]
related: [villon-interview-populated, module-pos, bir-compliance, module-accounting]
last-updated: 2026-06-01
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

## Operational Validation

The populated manager interview (`Sources/Villon_Interview_Populated.md`) validates these utang credit features:
- **Credit Blocking Rule (Q22):** The manager confirmed that while there is no formal credit limit per customer, the store strictly enforces a policy where a customer with any existing unpaid balance is blocked from incurring additional credit. This provides empirical validation for the system's hard credit blocking logic.
- **Collection Methods (Q22):** Overdue accounts are followed up in-store when customers visit, or through personal home visits by the manager if they do not come in.

## Source References

- [[wiki/sources/pos-module-paper|POS Paper]] — S2, S4, credit blocking rule, AR management
- [[wiki/sources/villon-interview-populated|Villon Interview]] — operational validation of S2/S4 credit blocking and collections

