---
module: POS
source: POS-audit-2026-05-15.md
generated: 2026-05-16
---

# Operator Verification Checklist — POS

> Extracted from the 2026-05-15 module audit. Only operator/manual verification tasks are included here.
> Code changes are tracked in module-specific plan files.

---

## POS-12 — View — Daily Summary

- [ ] Manual QA of Daily Summary view once application is runnable end-to-end

---

## POS-13 — BIR Tamper-Proof Receipt Retention & Sequence

- [ ] Run concurrency harness (`Pos_SequenceConcurrencyHarness.RunAsync`) against a scratch database
  - **How:** Launch in Debug, invoke harness, confirm no duplicate or gapped sequence numbers

---

## POS-15 — Receipt Numbering Integration & Concurrency Validation

- [ ] Execute `ReceiptSequenceHarnessReport.RunAndReportAsync()` against a scratch DB in a debug session
  - **Expected results:** Duplicates=0, Gaps=0, TotalReservations=800, elapsed time within acceptable range

---

## POS-16 — Receipt Archival Background Service

- [ ] Seed 100 expired + 100 in-window receipts; verify batch moves exactly 100
- [ ] Verify `batchSize = 50` produces `HadMoreEligible = True`
- [ ] Confirm fiscal-year guard: receipts issued in current year must **not** be archived even if `RetentionExpiresAt` is past
- [ ] Confirm rollback: forced archive-insert failure leaves live tables intact
- [ ] Confirm normal `DELETE FROM Pos_OfficialReceipts` fails with `BIR-immutable` after trigger amendment
- [ ] Confirm `Pos_OfficialReceiptArchive` and `Pos_ReceiptIntegrityArchive` reject UPDATE and DELETE

---

## POS-17 — VAT Settings UI

- [ ] Manual testing: verify load, save, and reload round-trip via the UI
- [ ] Verify `VatConfigurationLoader.GetAsync()` returns new values after save (acceptance criterion 3)
- [ ] Verify `VatConfigurationChangedEvent` is published with the correct `PreviousIsVatRegistered` (acceptance criterion 11)
- [ ] Verify **Manager** sees "VAT Settings" in sidebar; **Owner** does not (acceptance criteria 9–10)
