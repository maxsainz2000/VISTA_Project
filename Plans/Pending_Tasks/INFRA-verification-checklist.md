---
module: Infrastructure
source: Infrastructure-audit-2026-05-15.md
generated: 2026-05-16
---

# Operator Verification Checklist — Infrastructure

> Extracted from the 2026-05-15 module audit. Only operator/manual verification tasks are included here.
> Code changes are tracked as separate plan files (INFRA-12, INFRA-13, INFRA-14).

---

## INFRA-06 — MariaDB Central Schema & Reconciliation

- [ ] Replace `appsettings.json` `Pwd=CHANGE_ME` with a user-level `appsettings.Production.json` outside the repo before any live deployment
  - **How:** Create `appsettings.Production.json` with real credentials; ensure `.gitignore` excludes it
- [ ] Run `mariadb-init.sql` against a fresh MariaDB 11.4.x instance and verify acceptance criteria 2–3 manually
  - **How:** Execute the SQL script, then verify table structures and seed data match specification

---

## INFRA-08 — MariaDB Receipt Integrity Triggers

- [ ] ⏳ **Deferred:** Replace INFRA-06 triggers (`trg_Pos_ReceiptIntegrity_NoUpdate`, etc.) with versions using `DEFINER = <admin>` once a formal DB admin account is established
  - **Blocked on:** DB admin account creation

---

## INFRA-10 — Sync Status Shell Indicator

- [ ] Runtime smoke-test: launch the app and confirm the sync indicator renders in the status bar with no binding errors
- [ ] Confirm `Dispose` is invoked on shutdown (requires a test or debug trace on application exit)
  - **How:** Set a breakpoint in the indicator's `Dispose` method, close the app, confirm breakpoint is hit

---

## INFRA-11 — Production Deployment Configuration

- [ ] ⏳ **Deferred:** Pomelo 10.x bump — defer until `Pomelo.EntityFrameworkCore.MySql` 10.x is published on NuGet.org; remove NU1608 suppression from `Directory.Build.props` at that time
- [ ] Live operator walkthrough against a real MariaDB 11.4.x instance to validate the INFRA-06 acceptance criteria steps 2–3 documented in the runbook
  - **Reference:** `Plans/VISTA_Modules/Infrastructure/runbooks/`
