---
module: Infrastructure
agent: claude-code
date: 2026-05-28
plan-ref: Plans/VISTA_Modules/Infrastructure/29-operational-runbook.md
status: completed
---

## Task Summary

Implemented the full operational runbook suite and nightly backup automation for the VISTA MariaDB client-server deployment (INFRA-29). All four runbooks are written in operator-readable language. The PowerShell backup script enforces 7-daily / 4-weekly / 6-monthly retention and validates dump size. The Windows Task Scheduler XML job can be imported directly.

**Plan:** `[[29-operational-runbook]]`

## What Was Done

- Created `Plans/VISTA_Modules/Infrastructure/runbooks/README.md` — index of all four runbooks with quick-reference table
- Created `Plans/VISTA_Modules/Infrastructure/runbooks/01-host-laptop-setup.md` — 8 sections: hardware spec, OS/network prep, XAMPP install, MariaDB config, firewall rule, UPS wiring, first VISTA launch, smoke test
- Created `Plans/VISTA_Modules/Infrastructure/runbooks/02-client-laptop-setup.md` — 6 sections: prerequisites, install, appsettings.json configuration, first launch, smoke test, troubleshooting
- Created `Plans/VISTA_Modules/Infrastructure/runbooks/03-nightly-backup.md` — 8 sections: scope, requirements, install script, Task Scheduler setup (GUI + CLI), test procedure, retention policy, quarterly restore drill, backup user privileges
- Created `Plans/VISTA_Modules/Infrastructure/runbooks/04-host-failover.md` — 6-step failover checklist targeting 30-minute RTO, plus post-incident log template and permanent host restore procedure
- Created `Plans/VISTA_Modules/Infrastructure/runbooks/scripts/backup-mysqldump.ps1` — PowerShell backup script with size validation and tiered retention cleanup
- Created `Plans/VISTA_Modules/Infrastructure/runbooks/scripts/vista-nightly-backup.xml` — Windows Task Scheduler XML (daily at 02:00, runs even if user is not logged in)

## Runbook Content Notes

- **01-host-laptop-setup.md**: Documents exact MariaDB GRANT statements for `vista_app` (all privileges on `merchsys_central.*`) and `backup_user` (read-only). Includes Windows Firewall PowerShell command scoped to LAN subnet.
- **02-client-laptop-setup.md**: Documents NTFS `icacls` command to restrict read access to `appsettings.json`. Covers ConnectionStatusIndicator (INFRA-28) as the visual confirmation signal.
- **03-nightly-backup.md**: Restore drill procedure is self-contained (no missing steps). Quarterly cadence required per plan.
- **04-host-failover.md**: Includes data-loss estimation guidance (backup timestamp vs failure timestamp delta). End-state checklist has 9 items.

## Failover Walk-Through Notes

*(To be completed by operator at first deployment. Document walk-through date and any deviations here.)*

- Date performed: _______________
- Standby machine used: _______________
- Recovery time achieved: _____ minutes
- Deviations from runbook: _______________

## UPS Installation

*(To be verified by operator at deployment)*

- [ ] Host laptop power adapter connected to UPS
- [ ] Router connected to UPS
- [ ] Network switch (if separate) connected to UPS
- [ ] UPS runtime at typical load verified: _____ minutes
- [ ] UPS shutdown-on-low-battery configured: ✅ / ❌

## Build & Test Status

| Check | Status |
|---|---|
| All four runbook files exist | ✅ |
| `backup-mysqldump.ps1` syntax valid | ✅ (reviewed) |
| `vista-nightly-backup.xml` valid XML | ✅ |
| Unit tests pass | N/A |
| Backup script executed against live MariaDB | Pending (operator action at deployment) |
| Failover walk-through completed | Pending (operator action at deployment) |

## Issues Encountered

None. Pure documentation plan; no code changes.

## What's Next

- [x] INFRA-30: Master-Detail Activity Rail Sidebar (final implementation item) *(completed 2026-05-28)*
- [ ] Operator: copy runbooks to `Operator/runbooks/` if that directory is created
- [ ] Operator: fill in walk-through notes above after first drill
- [ ] Operator: schedule quarterly restore drill (first date: ~3 months after first deployment)

## Cross-References

- Domain Wiki pages consulted: `[[centralized-database-architecture]]`, `[[system_plan_amendment_2026-05-28]]`
- Runbook 02 references: `[[infra-28]]` (ConnectionStatusIndicator)
