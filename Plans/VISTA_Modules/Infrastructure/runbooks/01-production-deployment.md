# Runbook 01 — Production Deployment

**Prerequisite plans landed:** INFRA-05 (Sync Worker), INFRA-06 (MariaDB Central Schema), INFRA-08 (Receipt Integrity Triggers), INFRA-10 (Sync Status Shell Indicator)

**Audience:** System operator deploying VISTA on a live LAN. Commands target Windows 11 + XAMPP MariaDB 11.4.x.

---

## 1. Prerequisites

Before applying this runbook:

- MariaDB 11.4.x is installed and running (XAMPP recommended; default port 3306).
- The VISTA WPF host machine has TCP/IP network reachability to the MariaDB server on port 3306.
- You have the MariaDB `root` (admin) password for the server.
- The VISTA application has been built in Release configuration:
  ```
  dotnet build WPF_Applications/MerchSys/MerchSys.slnx -c Release
  ```

---

## 2. Schema Initialisation

Apply the two SQL scripts in order. Do **not** apply `02-pos-receipt-integrity-triggers.sql` before `mariadb-init.sql` — it is a schema delta that requires the base tables to exist.

### 2a. Apply base schema (INFRA-06)

```bash
mysql -h <server-hostname> -u root -p < "Plans/VISTA_Modules/Infrastructure/sql/mariadb-init.sql"
```

**Expected output:** No errors. MariaDB processes all `CREATE TABLE IF NOT EXISTS` and trigger `DELIMITER $$` blocks silently on success.

**Verification:**

```sql
USE merchsys_central;

-- Confirm table count (expect 19 tables)
SELECT COUNT(*) AS table_count
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'merchsys_central'
  AND TABLE_TYPE = 'BASE TABLE';

-- Spot-check key table groups
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'merchsys_central'
ORDER BY TABLE_NAME;
```

Expected tables include (not exhaustive): `Acc_ExpenseRecords`, `Acc_FinancialPeriods`, `Acc_RevenueRecords`, `Inv_Products`, `Inv_StockBatches`, `Pos_OfficialReceipts`, `Pos_ReceiptIntegrity`, `Pos_SalesTransactions`, `Pur_PurchaseOrders`, `Pur_Vendors`.

**Verify base triggers:**

```sql
SELECT TRIGGER_NAME, EVENT_MANIPULATION, EVENT_OBJECT_TABLE
FROM INFORMATION_SCHEMA.TRIGGERS
WHERE TRIGGER_SCHEMA = 'merchsys_central'
ORDER BY EVENT_OBJECT_TABLE, TRIGGER_NAME;
```

Expected triggers include `trg_Pos_OfficialReceipts_NoDelete`, `trg_Pos_OfficialReceipts_NoUpdate`, `trg_Pos_ReceiptIntegrity_NoDelete`, `trg_Pos_ReceiptIntegrity_NoUpdate`, and equivalent triggers for the `Acc_*` append-only tables.

### 2b. Apply receipt integrity triggers (INFRA-08)

```bash
mysql -h <server-hostname> -u root -p < "Plans/VISTA_Modules/Infrastructure/sql/02-pos-receipt-integrity-triggers.sql"
```

**Expected output:** No errors. This delta adds the `Pos_OfficialReceiptArchive` table and its immutability triggers.

**Verification:**

```sql
-- Confirm Pos_OfficialReceiptArchive exists
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'merchsys_central'
  AND TABLE_NAME = 'Pos_OfficialReceiptArchive';

-- Confirm all receipt archive triggers are present
SELECT TRIGGER_NAME
FROM INFORMATION_SCHEMA.TRIGGERS
WHERE TRIGGER_SCHEMA = 'merchsys_central'
  AND TRIGGER_NAME LIKE '%ReceiptArchive%'
ORDER BY TRIGGER_NAME;
```

---

## 3. User Provisioning

The `mariadb-init.sql` script already creates the `merchsys_sync` role and user with a placeholder password. You must **change the password** before the application can connect.

```sql
-- Connect as root
ALTER USER 'merchsys_sync'@'%' IDENTIFIED BY '<real-strong-password>';
FLUSH PRIVILEGES;
```

**Verify grants:**

```sql
SHOW GRANTS FOR 'merchsys_sync'@'%';
```

Expected output includes `SELECT, INSERT, UPDATE` on `merchsys_central.*`. The user has **no** `DELETE` grant — this is intentional; the immutability triggers enforce append-only semantics at the database layer.

**Verify restricted operations (negative path test):**

```sql
-- Attempt a prohibited DELETE — expect SQLSTATE 45000 error
INSERT INTO `Pos_OfficialReceipts`
  (Id, TransactionId, ReceiptNumber, BusinessName, BusinessAddress, BusinessTIN,
   IssueDate, TotalAmount, VatAmount, IsVatRegistered, CreatedBy, CreatedAt)
VALUES (999999, 999999, 'TEST-PROBE-001', 'Test', 'Test', '000-000-000',
        NOW(), 0, 0, 0, 'probe', NOW());

DELETE FROM `Pos_OfficialReceipts` WHERE Id = 999999;
-- Expected: ERROR 1644 (45000): Deletions from Pos_OfficialReceipts are prohibited (BIR compliance)

-- Cleanup the probe row (UPDATE is also prohibited, but the trigger fires on DELETE;
-- the probe row can be left — it carries no real data)
```

---

## 4. Production Config Placement

The application reads its MariaDB credentials from a user-level configuration overlay, never from the committed `appsettings.json`.

1. **Locate the template** in the deployed application directory:
   ```
   WPF_Applications\MerchSys\src\MerchSys.App\appsettings.Production.template.json
   ```

2. **Create the target directory** (if it does not exist):
   ```powershell
   New-Item -ItemType Directory -Force "$env:LOCALAPPDATA\VISTA"
   ```

3. **Copy the template to the user-level path:**
   ```powershell
   Copy-Item `
     "appsettings.Production.template.json" `
     "$env:LOCALAPPDATA\VISTA\appsettings.Production.json"
   ```

4. **Edit the live file** — replace every placeholder:

   | Field | Replace with |
   |---|---|
   | `<central-server-hostname>` | IP or hostname of the MariaDB server |
   | `3306` | Port (leave as-is unless non-standard) |
   | `merchsys_central` | Schema name (leave as-is unless changed) |
   | `merchsys_sync` | User name (leave as-is unless changed) |
   | `<replace-with-real-password>` | Password set in step 3 above |
   | `Required` | SslMode — **do not change** |

5. **Restrict NTFS permissions** so only the service account can read the file:
   ```powershell
   $path = "$env:LOCALAPPDATA\VISTA\appsettings.Production.json"
   # Remove inherited permissions, keep only current user
   icacls $path /inheritance:r /grant:r "$env:USERNAME:(R)"
   ```

6. **Verify** the file is not world-readable:
   ```powershell
   icacls "$env:LOCALAPPDATA\VISTA\appsettings.Production.json"
   # Expected: only your user account listed with (R) or (F)
   ```

**Full path on Windows:**
```
C:\Users\<username>\AppData\Local\VISTA\appsettings.Production.json
```

---

## 5. First-Run Verification

1. Launch the VISTA WPF application (`MerchSys.App.exe`).
2. Watch the **Sync Status Indicator** in the application shell (bottom status bar — delivered by INFRA-10).
3. The indicator should transition through these states within one probe interval (default: 30 seconds):
   - **Offline** (red) → initial state
   - **Probing** (amber) → TCP probe in progress
   - **Online** (green) → connection established and first sync cycle completed

If the indicator stays **Offline** after two probe intervals, check:
- `%LOCALAPPDATA%\VISTA\appsettings.Production.json` exists and is readable.
- The `Password` field is not still the placeholder string.
- The MariaDB server is reachable on TCP port 3306 from the VISTA host (`Test-NetConnection <server-hostname> -Port 3306`).
- The application log (`%LOCALAPPDATA%\MerchSys\logs\`) for detailed error messages.

---

## 6. Acceptance Verification (INFRA-06 Criteria Steps 2–3)

These steps confirm that the freshly provisioned instance satisfies the INFRA-06 acceptance criteria that were deferred to a live instance.

### Step 2 — Schema completeness check

Run against the live `merchsys_central` database:

```sql
-- All 19+ tables present (adjust count if schema has been extended)
SELECT COUNT(*) AS table_count
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'merchsys_central'
  AND TABLE_TYPE = 'BASE TABLE';

-- All append-only trigger pairs present (14+ triggers minimum)
SELECT COUNT(*) AS trigger_count
FROM INFORMATION_SCHEMA.TRIGGERS
WHERE TRIGGER_SCHEMA = 'merchsys_central';
```

**Expected:** `table_count >= 19`, `trigger_count >= 14`.

### Step 3 — Tamper-evidence trigger verification

Run the five negative-path probes from the diagnostic bundle at:
```
WPF_Applications/MerchSys/src/MerchSys.App/Resources/Sql/ReceiptIntegrityTriggerVerification.sql
```

```bash
mysql -h <server-hostname> -u root -p merchsys_central \
  < "WPF_Applications/MerchSys/src/MerchSys.App/Resources/Sql/ReceiptIntegrityTriggerVerification.sql"
```

**Expected output:** Each of the five probe statements produces an error of the form:
```
ERROR 1644 (45000): [table-specific immutability message]
```

Probes that succeed without an error indicate a missing trigger — re-apply the relevant SQL file and re-run.

---

*Last updated: 2026-05-15 | Relates to: INFRA-06, INFRA-08, INFRA-10, INFRA-11*
