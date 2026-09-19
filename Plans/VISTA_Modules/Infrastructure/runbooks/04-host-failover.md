# Runbook 04 — Host Laptop Failover

**Target audience:** IT responsible person / store owner.
**Prerequisites:** A recent backup from Runbook 03; a spare Windows 10/11 laptop available.
**RTO target:** 30 minutes from failure detection to all clients back Online.
**Success:** All VISTA clients show green "Online" badge and can process sales.

---

## Step 0 — Detect and Confirm the Failure

1. **Symptom:** All client VISTA sessions show the red "**Offline**" badge simultaneously.
2. **Confirm it is the host laptop**, not the network:
   - Can you ping other devices on the LAN? If yes → host laptop issue.
   - Is the XAMPP MySQL service stopped? Check by visiting the host laptop directly.
3. **Quick fix first** (< 2 min): If the host laptop is merely rebooted or MySQL crashed:
   - Restart MySQL in XAMPP Control Panel on the host.
   - Clients click the "**Retry**" button on the Offline badge.
   - If they go Online → no failover needed, proceed normally.
4. If the host laptop is unresponsive, physically damaged, or MySQL cannot start → **proceed to Step 1**.

---

## Step 1 — Prepare the Standby Machine (~10 min)

> You need a Windows 10/11 laptop on the same LAN.

1. Install XAMPP on the standby machine (see Runbook 01 §3) — or keep one pre-installed.
2. Start the MySQL service in XAMPP.
3. Set the `root` password (same procedure as Runbook 01 §4a).
4. Create the database and users:
   ```sql
   CREATE DATABASE IF NOT EXISTS merchsys_central
       CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
   CREATE USER IF NOT EXISTS 'vista_app'@'%' IDENTIFIED BY '<VISTA-APP-PASSWORD>';
   GRANT ALL PRIVILEGES ON merchsys_central.* TO 'vista_app'@'%';
   FLUSH PRIVILEGES;
   ```
5. Assign the standby machine a **static IP** or note its current DHCP-assigned IP — you will need this in Step 3.

---

## Step 2 — Restore the Latest Backup (~10 min)

1. Locate the most recent `.sql.gz` file on the backup destination (`\\BACKUP-MACHINE\vista\`).
2. Copy it to the standby machine (e.g., to `C:\Temp\`).
3. Decompress:
   ```powershell
   & "C:\Program Files\7-Zip\7z.exe" e "C:\Temp\merchsys_central_<timestamp>.sql.gz" -oC:\Temp\
   ```
4. Restore:
   ```powershell
   & "C:\xampp\mysql\bin\mysql.exe" -u root -p<ROOT-PASSWORD> merchsys_central < "C:\Temp\merchsys_central_<timestamp>.sql"
   ```
5. Verify:
   ```sql
   USE merchsys_central;
   SELECT TABLE_NAME, TABLE_ROWS FROM information_schema.TABLES
   WHERE TABLE_SCHEMA = 'merchsys_central'
   ORDER BY TABLE_NAME;
   ```
   Confirm row counts look correct (especially `Pos_SalesTransactions` for recent activity).

> **Data loss note:** Any transactions entered between the last backup and the host failure are lost. Check the timestamp of the backup file — this tells you the extent of data loss. Document it in the incident log (Step 5).

---

## Step 3 — Reconfigure Each Client (~5 min total, ~1 min per client)

On each client laptop:

1. Open `C:\Program Files\VISTA\appsettings.json` in Notepad.
2. Change the `Server=` value from the old host IP to the **standby machine's IP**:
   ```json
   "MerchSysCentral": "Server=<STANDBY-IP>;Port=3306;..."
   ```
3. Save and close.

---

## Step 4 — Restart VISTA on Each Client (~2 min total)

1. Close VISTA if open.
2. Relaunch VISTA.
3. Within 10 seconds the Connection Status badge should show green "**Online**".
4. Log in and verify the Stock Dashboard shows the expected data.

If the badge stays red: re-check the IP in `appsettings.json` and confirm the standby machine's MySQL service is running. Also verify the Windows Firewall rule for TCP 3306 on the standby machine (see Runbook 01 §5).

---

## Step 5 — Post-Incident Log

Record the following in the store's IT log or incident register:

| Field | Value |
|---|---|
| Failure detected at | (timestamp) |
| Failover completed at | (timestamp) |
| Total downtime | (minutes) |
| Root cause | (hardware failure / power / OS crash / other) |
| Last backup timestamp | (from filename of restored dump) |
| Estimated data loss | (time delta between backup and failure) |
| Actions taken | (summary) |
| Follow-up needed | (replace host laptop? UPS battery check? etc.) |

---

## Step 6 — Restore Permanent Host (~ongoing)

Once a replacement laptop is available:

1. Install XAMPP and configure it as per Runbook 01.
2. Restore the latest backup (repeat Step 2 above).
3. Set up the nightly backup job (Runbook 03) on the new host.
4. Reconfigure each client's `appsettings.json` back to the permanent host IP.
5. Decommission the standby machine's MariaDB installation (or keep it as the new standby).

---

## Checklist Summary

```
[ ] 1. Confirmed host failure (not transient network issue)
[ ] 2. Standby machine running MySQL with correct credentials
[ ] 3. Latest backup restored to standby machine
[ ] 4. Row counts verified on standby
[ ] 5. All clients updated with new Server= IP
[ ] 6. All clients show green Online badge
[ ] 7. Test transaction processed successfully
[ ] 8. Incident log filled in
[ ] 9. Nightly backup reconfigured on standby/new host
```
