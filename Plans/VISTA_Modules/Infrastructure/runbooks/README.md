# VISTA Operational Runbooks

**Target audience:** Store operator / IT responsible person at Villon Farm Supply.
**System:** VISTA (Villon Integrated Supply and Trade Application) — MariaDB 11.4.x on XAMPP, Windows 10/11.

| Runbook | Purpose | When to use |
|---|---|---|
| [01-host-laptop-setup.md](01-host-laptop-setup.md) | First-time setup of the MariaDB host laptop | New installation or OS reinstall |
| [02-client-laptop-setup.md](02-client-laptop-setup.md) | Install and configure each VISTA client workstation | New client or reinstall |
| [03-nightly-backup.md](03-nightly-backup.md) | Automated nightly backup via Windows Task Scheduler | Initial setup; also troubleshooting |
| [04-host-failover.md](04-host-failover.md) | Recover from host laptop failure (RTO 30 min) | Host laptop failure or replacement |

## Quick Reference

- **Host laptop MariaDB port:** 3306 (TCP, LAN only)
- **Database name:** `merchsys_central`
- **App user:** `vista_app`
- **Backup user:** `backup_user`
- **Backup script:** `scripts/backup-mysqldump.ps1`
- **Task Scheduler job:** `scripts/vista-nightly-backup.xml`

## Emergency Contacts

*(Fill in during initial deployment)*

- IT responsible: _______________
- Backup location: `\\<BACKUP-MACHINE>\vista\`
- Host laptop IP: _______________
