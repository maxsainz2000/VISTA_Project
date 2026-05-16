# Feature Gap Audit

**Date:** 2026-05-10
**Context:** Verification of alignment between `LLM_Wiki/wiki` (Domain Knowledge) and `LLM_Wiki/codebase_wiki` (Implementation Architecture).

## Summary
The following features are formally defined in the Domain Wiki but have no corresponding implementation trace in the Codebase Wiki.

### 1. App / Infrastructure Layer
* **Offline-First Sync (Dual-Condition Synchronization)** — ✅ Resolved
  * **Wiki Reference:** `wiki/concepts/offline-first-sync.md`, `Sources/system_plan.md`
  * **Original Gap:** While local SQLite is implemented, there is no implementation for the `SyncWorker`, background service, or dual-condition network check required to sync transactions to the central MariaDB instance.
  * **Resolved By:** INFRA-05 (Sync Worker & Dual-Condition Connectivity Probe), INFRA-06 (MariaDB Central Schema & Reconciliation), INFRA-09 (ISyncableRepository Per-Module Implementation), INFRA-10 (Sync Status Shell Indicator)

### 2. POS Module
* **BIR Compliance (OR-YYYY-XXXX)** — ✅ Resolved
  * **Wiki Reference:** `wiki/concepts/bir-compliance.md`, `Sources/POS-Module_AcademicPaper.md`
  * **Original Gap:** The `ReceiptService` exists, but there is no explicit codebase trace for generating the strict BIR-mandated `OR-YYYY-XXXX` sequential format, nor are the required long-term tamper-proof retention constraints enforced in the schema.
  * **Resolved By:** POS-13 (BIR Tamper-Proof Receipt Retention & Sequence), POS-15 (Receipt Numbering Integration & Concurrency Validation), POS-16 (Receipt Archival Background Service)
* **VAT-Ready Structure** — ✅ Resolved
  * **Wiki Reference:** `wiki/concepts/vat-ready.md`, `Sources/POS-Module_AcademicPaper.md`
  * **Original Gap:** The system is missing VAT calculation logic, VAT configuration toggles (12% vs. Non-VAT), and VAT columns in the POS and Accounting transaction structures.
  * **Resolved By:** POS-14 (VAT Configuration, Schema Extension & Calculation), POS-17 (VAT Settings UI), POS-18 (Receipt Body VAT Bucket Printing), INFRA-07 (Cross-Module VAT Event Payload Contracts)

### 3. Accounting Module
* **VAT Reporting** — ✅ Resolved
  * **Wiki Reference:** `wiki/concepts/vat-ready.md`
  * **Original Gap:** Along with the POS VAT gaps, the Accounting module does not document the generation of required BIR VAT reports.
  * **Resolved By:** ACC-10 (Accounting VAT Ledger Schema Extension), ACC-11 (BIR VAT Reporting Service & Views), ACC-12 (VAT Payable KPI in Financial Overview), ACC-14 (VAT Tile Integration into Financial Overview)

## Conclusion
All four feature gaps identified in this audit have been **resolved** as of 2026-05-15. The infrastructure for remote data synchronization (INFRA-05/06/09/10) and strict BIR tax compliance (POS-13/14/15/16/17/18, ACC-10/11/12/14, INFRA-07) are now architecturally complete. Remaining work is tracked as operator verification checklists and follow-up plans in `Plans/Pending_Tasks/`.
