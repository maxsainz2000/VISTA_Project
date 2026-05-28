---
module: MerchSys.Infrastructure
plan-id: INFRA-29
title: "Operational Runbook — UPS, Nightly Backup, Failover"
depends-on: [INFRA-25]
estimated-files: 4
priority: high
amendment-ref: AMD-2026-05-28-01
---

# INFRA-29: Operational Runbook — UPS, Nightly Backup, Failover

## Context

The pivot to pure client-server makes the MariaDB host laptop a single point of failure (per `system_plan_amendment_2026-05-28.md` §3 trade-offs). The amendment elevates UPS-backing from a recommendation to a requirement and mandates nightly backups + a documented failover runbook with a 30-minute RTO.

This plan is **documentation-only**. It produces operator-facing runbooks, a Task Scheduler XML for the nightly backup, and a per-laptop setup checklist.

## Prerequisites

- **INFRA-25** — MariaDB is the live DB.

(INFRA-27 and INFRA-28 do not block this plan, but the runbook references INFRA-28's `ConnectionStatusIndicator`. If INFRA-28 hasn't shipped when this plan runs, mark the runbook section "TBD — fill in after INFRA-28".)

## Wiki References

- `LLM_Wiki/Sources/system_plan_amendment_2026-05-28.md` §4 (Risks), §5 (Mitigations), §6 (Constraints)
- `LLM_Wiki/wiki/concepts/centralized-database-architecture.md` (Operational Requirements)

## Deliverables

```
Plans/VISTA_Modules/Infrastructure/runbooks/
├── 01-host-laptop-setup.md                         ' NEW — XAMPP install, MariaDB config, UPS wiring, network setup
├── 02-client-laptop-setup.md                       ' NEW — install VISTA, populate appsettings.json, smoke test
├── 03-nightly-backup.md                            ' NEW — Task Scheduler job + mysqldump script
└── 04-host-failover.md                             ' NEW — recover from host laptop failure (RTO 30 min)

Plans/VISTA_Modules/Infrastructure/runbooks/scripts/
├── backup-mysqldump.ps1                            ' NEW — PowerShell wrapper around mysqldump
└── vista-nightly-backup.xml                        ' NEW — Task Scheduler job export
```

The `Plans/` location is intentional — these are operator-facing operational documents, kept versioned alongside the implementation plans. A `Operator/runbooks/` mirror may also make sense; copy on completion if that path exists in the repo.

## Specification

### 01-host-laptop-setup.md

Sections:
1. **Hardware** — minimum spec (RAM, disk), UPS make/model recommendation (or category), Ethernet preference.
2. **OS prep** — Windows 10/11, static IP assignment (or DHCP reservation on router), Windows Firewall rule for TCP 3306 inbound from LAN subnet only.
3. **XAMPP install** — exact version, default settings, change the `root` password, create `vista_app` user with `GRANT ALL ON merchsys_central.*`.
4. **MariaDB config** — `bind-address = 0.0.0.0` (or restrict to LAN subnet), `max_connections = 50` (10 per client × 4 clients × safety margin), `default-storage-engine = InnoDB`.
5. **First launch of VISTA on the host** — bootstrap runs (INFRA-24), seed data lands, `SHOW TABLES` confirmation.
6. **UPS wiring** — host laptop + router + switch all behind the UPS. Shutdown delay configuration.
7. **Smoke test** — connect from a second laptop on the LAN, confirm Online state.

### 02-client-laptop-setup.md

Sections:
1. **Prereqs** — Windows 10/11, .NET 10 runtime, LAN connectivity to host.
2. **Install** — copy VISTA build output to `C:\Program Files\VISTA\`, create Start Menu shortcut.
3. **Configure `appsettings.json`** — copy from `appsettings.Example.json`, edit `Server=<HOST-IP>` and `WorkstationName=<friendly-name>`.
4. **First launch** — verify ConnectionStatusIndicator goes Online within 10 seconds.
5. **Smoke test** — login as manager, Stock Dashboard renders 20 products, log out.

### 03-nightly-backup.md

Sections:
1. **What gets backed up** — `merchsys_central` database (full dump). Backups stored on a secondary machine (NAS, second laptop, or external USB drive — operator choice).
2. **Schedule** — 02:00 daily via Windows Task Scheduler.
3. **Retention** — keep last 7 daily, last 4 weekly, last 6 monthly. Older dumps purged by the script.
4. **Script** — `backup-mysqldump.ps1`:

```powershell
$ts = Get-Date -Format "yyyy-MM-dd_HH-mm"
$out = "\\BACKUP-MACHINE\vista\merchsys_central_$ts.sql.gz"
& "C:\xampp\mysql\bin\mysqldump.exe" `
    --single-transaction --routines --triggers --events `
    -u backup_user -p"<PASSWORD>" merchsys_central `
  | & "C:\Program Files\7-Zip\7z.exe" a -tgzip -si "$out"

# Verify size > 1 KB and exit with error if dump is suspiciously small
$size = (Get-Item $out).Length
if ($size -lt 1024) { Write-Error "Backup suspiciously small: $size bytes"; exit 1 }

# Retention cleanup (keep last 7 daily, last 4 weekly Sundays, last 6 monthly 1st-of-month)
# ... script body in deliverable file
```

5. **Restore drill** — quarterly: pick a dump at random, restore to a sandbox MariaDB, confirm row counts. Document the procedure.

6. **Backup user** — create with read-only privileges: `GRANT SELECT, SHOW VIEW, LOCK TABLES, RELOAD, EVENT, TRIGGER ON merchsys_central.* TO 'backup_user'@'localhost'`.

### 04-host-failover.md

RTO target: **30 minutes** from host-laptop failure detection to all clients back Online.

Procedure:
1. **Detect** — Clients all show Offline simultaneously. Confirm host laptop is dead (not just network).
2. **Bring up backup MariaDB** — on a designated standby laptop or any spare Windows machine with XAMPP installed:
   - Restore the latest `mysqldump` from the backup location.
   - Confirm `merchsys_central` has expected row counts.
3. **Update each client's `appsettings.json`** — edit `Server=<NEW-HOST-IP>`.
4. **Restart each client** — verify ConnectionStatusIndicator goes Online.
5. **Post-incident** — note in incident log: timestamp of failure, time to recovery, root cause of host failure, any data loss (last successful backup timestamp vs failure timestamp).

### Backup verification

The runbook explicitly requires a quarterly restore drill. Backups that are never restored are not backups; they are wishes.

## Acceptance Criteria

1. All four runbook files exist and are operator-readable (no developer jargon without a glossary).
2. `backup-mysqldump.ps1` executes against a local MariaDB and produces a non-empty compressed dump.
3. `vista-nightly-backup.xml` Task Scheduler job imports cleanly on a Windows 10/11 machine.
4. Host failover runbook is walked-through end-to-end on a non-production host as a one-time verification; document the walk-through in the implementation summary.
5. UPS is physically installed on the host laptop (verify with a photo or operator confirmation in the summary).
6. Restore drill procedure is testable from the document alone (no missing steps).

## Out of Scope (Defer)

- Automated host failover (e.g., MariaDB replication with semi-sync). Manual failover is the deliberate choice for current scale.
- Off-site backup (cloud upload). Local NAS or external drive is sufficient for a single-location shop.
- Backup encryption at rest. Worth doing — future plan.
- Monitoring/alerting (Prometheus, etc.). Out of scope until operational pain demands it.

## Output Requirements

### Implementation Summary

`Progress/VISTA_Modules/Infrastructure/INFRA-29-summary.md` per `Progress/_template.md`. Include:

- Confirmation that all four runbooks exist with operator-readable language.
- Output of a successful `backup-mysqldump.ps1` run.
- Photograph or operator confirmation of UPS installation.
- Notes from the failover walk-through.
- Date of first quarterly restore drill (to be scheduled).

### Documentation

- Each runbook starts with: target audience, prerequisites, expected duration, and a one-sentence summary of what success looks like.
- `backup-mysqldump.ps1` has a header comment with INFRA-29 reference and the contact for issues.
- A `runbooks/README.md` index file listing all four runbooks with their purposes.
