# Feature Gap Audit

**Date:** 2026-05-10
**Context:** Verification of alignment between `LLM_Wiki/wiki` (Domain Knowledge) and `LLM_Wiki/codebase_wiki` (Implementation Architecture).

## Summary
The following features are formally defined in the Domain Wiki but have no corresponding implementation trace in the Codebase Wiki.

### 1. App / Infrastructure Layer
* **Offline-First Sync (Dual-Condition Synchronization)**
  * **Wiki Reference:** `wiki/concepts/offline-first-sync.md`, `Sources/system_plan.md`
  * **Gap:** While local SQLite is implemented, there is no implementation for the `SyncWorker`, background service, or dual-condition network check required to sync transactions to the central MariaDB instance.

### 2. POS Module
* **BIR Compliance (OR-YYYY-XXXX)**
  * **Wiki Reference:** `wiki/concepts/bir-compliance.md`, `Sources/POS-Module_AcademicPaper.md`
  * **Gap:** The `ReceiptService` exists, but there is no explicit codebase trace for generating the strict BIR-mandated `OR-YYYY-XXXX` sequential format, nor are the required long-term tamper-proof retention constraints enforced in the schema.
* **VAT-Ready Structure**
  * **Wiki Reference:** `wiki/concepts/vat-ready.md`, `Sources/POS-Module_AcademicPaper.md`
  * **Gap:** The system is missing VAT calculation logic, VAT configuration toggles (12% vs. Non-VAT), and VAT columns in the POS and Accounting transaction structures.

### 3. Accounting Module
* **VAT Reporting**
  * **Wiki Reference:** `wiki/concepts/vat-ready.md`
  * **Gap:** Along with the POS VAT gaps, the Accounting module does not document the generation of required BIR VAT reports.

## Conclusion
The architectural implementation tracked in the `codebase_wiki` currently handles the core offline scenarios (SQLite, POS, Inventory, Reorder, Accounting) but is lacking the infrastructure for remote data synchronization and strict BIR tax compliance required by the business profile.
