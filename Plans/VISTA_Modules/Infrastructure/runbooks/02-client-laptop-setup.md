# Runbook 02 — Client Laptop Setup

**Target audience:** IT responsible person / store operator.
**Prerequisites:** Host laptop fully set up per Runbook 01; host IP address known; .NET 10 Runtime available.
**Expected duration:** 15–20 minutes per client.
**Success:** Client logs in as Manager, Stock Dashboard renders 20 products, connection badge shows green "Online".

---

## 1. Prerequisites Checklist

- [ ] Windows 10 or Windows 11 (64-bit)
- [ ] **.NET 10 Desktop Runtime** installed (download from microsoft.com/dotnet; choose the "Desktop Runtime" for Windows x64)
- [ ] LAN connectivity to the host laptop (same subnet, can ping host IP)
- [ ] Host IP address and `vista_app` password from Runbook 01

---

## 2. Install VISTA

1. Create the application directory:
   ```powershell
   New-Item -ItemType Directory -Force "C:\Program Files\VISTA"
   ```
2. Copy all files from the VISTA build output (`MerchSys.App\bin\Release\net10.0-windows\`) into `C:\Program Files\VISTA\`.
3. Create a Start Menu shortcut pointing to `C:\Program Files\VISTA\MerchSys.App.exe`.

> **Important:** Each client has its own `appsettings.json`. Do not copy an `appsettings.json` from another machine — it will point to the wrong host IP or have the wrong `WorkstationName`.

---

## 3. Configure appsettings.json

1. Copy `appsettings.Example.json` (in the VISTA install directory) to `appsettings.json`.
2. Open `C:\Program Files\VISTA\appsettings.json` in Notepad.
3. Edit the following values:

```json
{
  "ConnectionStrings": {
    "MerchSysCentral": "Server=<HOST-IP>;Port=3306;Database=merchsys_central;User Id=vista_app;Password=<VISTA-APP-PASSWORD>;ConnectionTimeout=5;DefaultCommandTimeout=10;"
  },
  "Client": {
    "WorkstationName": "<WORKSTATION-NAME>"
  }
}
```

| Placeholder | Replace with |
|---|---|
| `<HOST-IP>` | LAN IP of the host laptop (e.g., `192.168.1.50`) |
| `<VISTA-APP-PASSWORD>` | The `vista_app` password set in Runbook 01 §4c |
| `<WORKSTATION-NAME>` | Unique name for this laptop (e.g., `Cashier-1`, `Cashier-2`, `Manager-Office`, `Owner-Office`) |

4. Save the file.

> **Security note:** The `appsettings.json` file contains the database password. Set NTFS permissions so only the Windows user account that runs VISTA can read it:
> ```powershell
> icacls "C:\Program Files\VISTA\appsettings.json" /inheritance:r /grant:r "${env:USERNAME}:(R)"
> ```

---

## 4. First Launch

1. Open VISTA from the Start Menu shortcut.
2. Watch the **Connection Status badge** at the bottom of the sidebar:
   - Green "**Online**" within 10 seconds → connection is good, proceed.
   - Orange "**Reconnecting…**" → verify host IP and firewall (see troubleshooting below).
   - Red "**Offline**" → cannot reach MariaDB; troubleshoot before use.
3. Log in with the Manager credentials.
4. Navigate to **Inventory → Stock Dashboard** and verify 20 products are listed.
5. Log out.

---

## 5. Smoke Test

| Step | Expected result |
|---|---|
| Launch VISTA | Connection badge → green "Online" within 10 s |
| Log in as Manager | Successful; sidebar shows all navigation items |
| Stock Dashboard | 20 products visible |
| Sales Cart | Cart loads with product search available |
| Log out | Returns to login screen |

If all steps pass, this client is operational.

---

## 6. Troubleshooting

**Badge stays orange/red:**
1. Verify the host laptop is powered on and XAMPP MySQL is running.
2. From this client, run:
   ```powershell
   Test-NetConnection -ComputerName <HOST-IP> -Port 3306
   ```
   If `TcpTestSucceeded: False` — the firewall is blocking. Ask IT to verify Runbook 01 §5.
3. Verify the IP address in `appsettings.json` matches the host laptop's current IP.

**Login fails with "Invalid credentials":**
- Credentials are stored in the database (set during initial Manager account creation at first launch on the host). Confirm with the Manager or Owner.

**"Cannot initialize database schema" at startup:**
- This appears only on the host laptop first launch. On client laptops, schema bootstrap is skipped (tables already exist). If this error appears on a client, verify `appsettings.json` is pointing to the correct host.
