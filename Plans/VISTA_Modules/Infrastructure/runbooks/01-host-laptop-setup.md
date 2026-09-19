# Runbook 01 — Host Laptop Setup

**Target audience:** IT responsible person / store owner.
**Prerequisites:** A dedicated Windows 10/11 laptop on the store LAN.
**Expected duration:** 60–90 minutes.
**Success:** All client laptops show the green "Online" badge in VISTA within 10 seconds of launch.

---

## 1. Hardware Requirements

| Item | Minimum | Recommended |
|---|---|---|
| RAM | 4 GB | 8 GB |
| Storage | 60 GB free | 120 GB free (for backups) |
| Network | Wi-Fi | Ethernet (preferred — lower latency, no dropout) |
| Power | UPS-backed | **Required** (see §6) |

The host laptop must stay powered on during business hours. Treat it like a server — do not use it for browsing or other applications that could slow MariaDB.

---

## 2. OS Preparation

1. Ensure the laptop runs **Windows 10 or Windows 11** (64-bit).
2. Apply all Windows Updates before proceeding.
3. **Assign a static LAN IP address** (strongly recommended — prevents clients from losing connection after router reboots):
   - Option A — DHCP reservation: log in to your router admin page, find the host laptop's MAC address, assign a fixed IP (e.g., `192.168.1.50`).
   - Option B — Static IP on the adapter: open *Network Connections → Properties → IPv4 → Manual*, set IP/mask/gateway/DNS.
4. Note the IP address — you will need it in every client's `appsettings.json`.

---

## 3. XAMPP Installation

1. Download **XAMPP 8.2.x** from [apachefriends.org](https://www.apachefriends.org/) (includes MariaDB 11.x).
2. Run the installer; install to the default path `C:\xampp\`.
3. During setup, **uncheck Apache** (not needed); **check MariaDB**.
4. After install, open the XAMPP Control Panel, start **MySQL** (the MariaDB service).
5. Verify it starts without errors — the green indicator should appear.

---

## 4. MariaDB Configuration

### 4a. Set the root password

In a Command Prompt or PowerShell (run from `C:\xampp\mysql\bin\`):

```powershell
& "C:\xampp\mysql\bin\mysql.exe" -u root -e "ALTER USER 'root'@'localhost' IDENTIFIED BY '<ROOT-PASSWORD>';"
```

Replace `<ROOT-PASSWORD>` with a strong password. Store it in a secure location (password manager or printed sheet kept in a locked drawer).

### 4b. Allow remote connections from the LAN

Open `C:\xampp\mysql\bin\my.ini` (or `C:\xampp\mysql\bin\my.cnf`) in Notepad:

1. Under `[mysqld]`, find or add:
   ```ini
   bind-address = 0.0.0.0
   max_connections = 50
   default-storage-engine = InnoDB
   ```
2. Save and restart the MySQL service in XAMPP Control Panel.

### 4c. Create the application database and users

Open an interactive MariaDB session:

```powershell
& "C:\xampp\mysql\bin\mysql.exe" -u root -p<ROOT-PASSWORD>
```

Then run:

```sql
CREATE DATABASE IF NOT EXISTS merchsys_central
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Application user (used by all VISTA clients)
CREATE USER IF NOT EXISTS 'vista_app'@'%'
    IDENTIFIED BY '<VISTA-APP-PASSWORD>';
GRANT ALL PRIVILEGES ON merchsys_central.* TO 'vista_app'@'%';

-- Backup user (localhost only, read-only)
CREATE USER IF NOT EXISTS 'backup_user'@'localhost'
    IDENTIFIED BY '<BACKUP-PASSWORD>';
GRANT SELECT, SHOW VIEW, LOCK TABLES, RELOAD, EVENT, TRIGGER
    ON merchsys_central.* TO 'backup_user'@'localhost';

FLUSH PRIVILEGES;
```

Replace the `<...>` placeholders with real passwords. Record all three passwords securely.

---

## 5. Windows Firewall Rule

Allow inbound TCP 3306 from your LAN subnet only (replace `192.168.1.0/24` with your actual subnet):

```powershell
New-NetFirewallRule `
    -DisplayName "VISTA MariaDB LAN" `
    -Direction Inbound `
    -Protocol TCP `
    -LocalPort 3306 `
    -RemoteAddress 192.168.1.0/24 `
    -Action Allow
```

Run in an **Administrator** PowerShell. Verify the rule appears in *Windows Defender Firewall → Inbound Rules*.

---

## 6. UPS Wiring

> **This step is required.** Power loss to the host laptop mid-write corrupts InnoDB pages. A UPS prevents data loss and avoids the cost of point-in-time recovery.

1. Connect to the UPS (in order of priority):
   - Host laptop power adapter
   - Router
   - Network switch (if separate)
2. Confirm the UPS has at least **10 minutes runtime** at typical load.
3. Configure the UPS software (if available) to send a graceful shutdown signal after 5 minutes on battery — this gives the system time to flush writes and close MariaDB cleanly before full battery discharge.

---

## 7. First Launch of VISTA on the Host

1. Copy the VISTA build output to `C:\Program Files\VISTA\`.
2. Edit `C:\Program Files\VISTA\appsettings.json`:
   - Set `ConnectionStrings:MerchSysCentral` to `Server=127.0.0.1;Port=3306;Database=merchsys_central;User Id=vista_app;Password=<VISTA-APP-PASSWORD>;ConnectionTimeout=5;DefaultCommandTimeout=10;`
   - Set `Client:WorkstationName` to `Host-Laptop`
3. Launch VISTA. The `MariaDbSchemaInitializer` runs automatically — it creates all tables and inserts seed data (20 products, 4 categories, 3 vendors).
4. Confirm by running:
   ```powershell
   & "C:\xampp\mysql\bin\mysql.exe" -u root -p<ROOT-PASSWORD> merchsys_central -e "SHOW TABLES;"
   ```
   You should see 20+ tables with `Pur_`, `Inv_`, `Pos_`, `Acc_` prefixes.
5. Log in as Manager. Verify the Stock Dashboard shows 20 products.

---

## 8. LAN Connectivity Smoke Test

From a second laptop on the same LAN:

1. Follow Runbook 02 (client setup).
2. Launch VISTA — the **Connection Status** badge (bottom of sidebar) should turn **green "Online"** within 10 seconds.

If it stays red/orange:
- Confirm the firewall rule is active (`netsh advfirewall firewall show rule name="VISTA MariaDB LAN"`).
- Ping the host laptop IP from the client.
- Check XAMPP MySQL is still running.
