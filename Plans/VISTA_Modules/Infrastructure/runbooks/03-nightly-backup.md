# Runbook 03 — Nightly Backup

**Target audience:** IT responsible person.
**Prerequisites:** Host laptop set up per Runbook 01; 7-Zip installed on the host.
**Expected duration:** 30 minutes (initial setup); backup runs unattended each night.
**Success:** A compressed `.sql.gz` file appears on the backup destination each morning, sized > 1 KB.

---

## 1. What Gets Backed Up

A full mysqldump of the `merchsys_central` database, including:
- All tables (transactions, stock, accounts payable, receipts, etc.)
- Stored routines, triggers, and events (if any)

Backups are stored on a secondary machine — choose one:
- **NAS device** on the LAN (recommended)
- **Second laptop** with a shared folder
- **External USB drive** mounted on the host (least preferred — single point of failure)

---

## 2. Requirements

| Item | Notes |
|---|---|
| **7-Zip** | Install from [7-zip.org](https://www.7-zip.org/); default path `C:\Program Files\7-Zip\` |
| **Backup destination** | A UNC path (e.g., `\\NAS\vista\`) or local path; must be writable by the Task Scheduler user |
| **backup_user** | Created in Runbook 01 §4c (read-only MariaDB account, localhost only) |
| **PowerShell 7+** | Installed with `winget install Microsoft.PowerShell` if not present |

---

## 3. Install the Backup Script

1. Copy `scripts/backup-mysqldump.ps1` to `C:\Program Files\VISTA\runbooks\scripts\`.
2. Copy `scripts/vista-nightly-backup.xml` to the same location.
3. Edit `backup-mysqldump.ps1` line by line:
   - `$BackupRoot` — replace `\\BACKUP-MACHINE\vista` with your actual backup share.
   - Leave `$BackupPassword` blank — you will supply it via the Task Scheduler action (see §4).

---

## 4. Create the Windows Task Scheduler Job

### Option A — Import from XML (fastest)

1. Open **Task Scheduler** (search in Start Menu).
2. Action → **Import Task** → select `C:\Program Files\VISTA\runbooks\scripts\vista-nightly-backup.xml`.
3. In the **General** tab:
   - Set **Run whether user is logged in or not**.
   - Check **Run with highest privileges**.
   - Enter the Windows user password when prompted.
4. In the **Actions** tab, edit the PowerShell command:
   - Replace `<BACKUP-PASSWORD>` in `-BackupPassword "<BACKUP-PASSWORD>"` with the real `backup_user` password.
   - Update `-BackupRoot` to your backup share if different from the default.
5. Click **OK** and enter the Windows user password.

### Option B — Command line

```powershell
schtasks /Create /XML "C:\Program Files\VISTA\runbooks\scripts\vista-nightly-backup.xml" /TN "VISTA\NightlyBackup" /RU <WINDOWS-USER> /RP <WINDOWS-PASSWORD>
```

---

## 5. Test the Backup

Run the task immediately (before waiting for the 02:00 scheduled time):

```powershell
schtasks /Run /TN "VISTA\NightlyBackup"
```

Then verify a `.sql.gz` file appeared in your backup destination:

```powershell
Get-ChildItem "\\BACKUP-MACHINE\vista\" | Sort-Object LastWriteTime -Descending | Select-Object -First 3
```

The file size must be > 1 KB. If the script exits with an error:
- Check the Task Scheduler history (right-click job → History).
- Ensure 7-Zip is installed and `backup_user` password is correct.

---

## 6. Retention Policy

The script automatically prunes old backups:

| Tier | Kept | Criteria |
|---|---|---|
| Daily | Last 7 | All dumps |
| Weekly | Last 4 | Dumps made on Sundays |
| Monthly | Last 6 | Dumps made on the 1st of the month |

Files not matching any retention tier are deleted after each run. You will never accumulate more than ~17 backup files.

---

## 7. Restore Procedure (Disaster Recovery Drill)

> Run this drill **quarterly** to ensure backups are usable. An untested backup is worthless.

1. Install XAMPP on a **test machine** (not the production host).
2. Copy a recent backup file (`*.sql.gz`) to the test machine.
3. Decompress:
   ```powershell
   & "C:\Program Files\7-Zip\7z.exe" e "merchsys_central_<timestamp>.sql.gz"
   ```
4. Restore to a test database:
   ```powershell
   & "C:\xampp\mysql\bin\mysql.exe" -u root -p<ROOT-PASSWORD> -e "CREATE DATABASE drill_restore;"
   & "C:\xampp\mysql\bin\mysql.exe" -u root -p<ROOT-PASSWORD> drill_restore < "merchsys_central_<timestamp>.sql"
   ```
5. Verify row counts:
   ```sql
   USE drill_restore;
   SELECT TABLE_NAME, TABLE_ROWS FROM information_schema.TABLES
   WHERE TABLE_SCHEMA = 'drill_restore'
   ORDER BY TABLE_NAME;
   ```
6. Confirm counts look reasonable (e.g., Inv_Products has ~20+ rows, Pos_SalesTransactions has recent dates).
7. Drop the test database:
   ```powershell
   & "C:\xampp\mysql\bin\mysql.exe" -u root -p<ROOT-PASSWORD> -e "DROP DATABASE drill_restore;"
   ```
8. Document the drill date and result in the store's IT log.

---

## 8. Backup User Privileges Reference

```sql
GRANT SELECT, SHOW VIEW, LOCK TABLES, RELOAD, EVENT, TRIGGER
    ON merchsys_central.* TO 'backup_user'@'localhost';
```

The backup user has read-only access — it cannot modify data. If mysqldump reports a privilege error, verify the GRANT statement was applied.
