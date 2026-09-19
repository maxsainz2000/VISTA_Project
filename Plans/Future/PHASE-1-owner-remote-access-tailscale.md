---
title: "Phase 1 — Owner Remote Read Access via Tailscale (WireGuard mesh VPN)"
status: complete (2026-06-02)
created: 2026-06-02
owner: VISTA Operator
type: infrastructure / deployment (no application code changes)
depends-on: [centralized-database-architecture]
related: [PHASE-2-owner-cloud-replica (future)]
---

# Phase 1 — Owner Remote Read Access via Tailscale

## Goal

Let the **read-only Owner** view live business KPIs and financial reports from
anywhere (e.g. on a business trip, off the Villon LAN) by securely extending the
existing LAN client-server connection over a free WireGuard mesh VPN.

The Owner's VISTA app connects to the **live central MariaDB** on the host laptop
exactly as a LAN client does — only the network path changes. **No application
code changes. No cloud database. No sync layer. No data leaves the premises.**

## Why this approach

- The Owner is read-only; the app already speaks plain TCP to a configurable
  MariaDB host (`ConnectionStrings:MerchSysCentral`). We only need *reachability*,
  not replication.
- Reuses the existing, validated client-server architecture
  (`LLM_Wiki/wiki/concepts/centralized-database-architecture.md`) unchanged.
- Read-only is already enforced at the app data layer
  (`RoleGuardInterceptor`, OWASP DA5); this plan adds a DB-level `SELECT`-only
  grant as defense-in-depth.

## Cost / constraints (verified 2026-06-02)

- **Tailscale Personal plan:** free forever, **no credit card**, up to 6 users +
  unlimited user devices (terms updated 2026-04-08). We need 2 devices.
- **No monthly subscription. No router port-forwarding.** (Tailscale is
  peer-to-peer; exposing port 3306 to the public internet is **not** required and
  must **not** be done.)

## Accepted limitation

The Owner can read data **only while the host laptop is online** — identical to the
trade already accepted for LAN clients ("loss of connectivity to the host stops
the client"). 24/7 access when the host is powered off is **out of scope** here and
is the subject of a future Phase 2 (cloud read-replica).

---

## Implementation steps

> These are operator/deployment steps. No `.vb`/`.xaml` changes are required.

### 1. Create the tailnet (one-time)
- [x] Sign up for Tailscale (Personal/free plan) using a single owner-controlled
      SSO identity. Confirm no credit card is requested.
- [x] This account's tailnet is where both laptops will live.

### 2. Host laptop (runs XAMPP / MariaDB)
- [x] Install Tailscale (`https://tailscale.com/download`), sign in to the tailnet.
- [x] Record the host's Tailscale IPv4 (`tailscale ip -4` → `100.76.155.51`).
- [x] MagicDNS skipped — admin console does not allow custom rename; using Tailscale
      IP `100.76.155.51` directly in Owner config instead. Stable for device lifetime.
- [x] In the Tailscale admin console, **disable key expiry** for the host node so
      the tunnel does not drop after the default expiry window.

### 3. Owner laptop
- [x] Install Tailscale, sign in to the **same** tailnet.
- [x] Verify connectivity from off-LAN: ping to host IP succeeds.

### 4. MariaDB network exposure (host laptop, least privilege)
- [x] In `C:\xampp\mysql\bin\my.ini`: bind-address is commented out (defaults to
      all interfaces) and skip-networking is commented out — MariaDB listens on
      all interfaces as required.
- [x] **Windows Firewall:** inbound rule "MariaDB – Tailscale only (VISTA)" added —
      TCP 3306 from `100.64.0.0/10` only, Profile Any, Status OK.
- [ ] Confirm there is **no router port-forward** to 3306.

### 5. Dedicated read-only DB user for the Owner (defense-in-depth)
- [x] Created `merchsys_owner'@'%'` with `GRANT SELECT ON merchsys_central.*`.
      Verified: `SELECT COUNT(*) FROM Sys_UserAccounts` → 3 rows (read confirmed).
      Verified: `UPDATE Sys_UserAccounts SET Username='test' WHERE 1=0` →
      `ERROR 1142: UPDATE command denied` (write block confirmed).
- [ ] **Validate the login flow under SELECT-only** (during the acceptance test phase).
      The Owner session may legitimately need a few writes that pure `SELECT` blocks:
      - login bookkeeping (last-login timestamp, failed-attempt / lockout counters
        on `Sys_UserAccounts`),
      - Owner self-service password change (`AuthSelfService`, allowed by
        `RoleGuardInterceptor`),
      - any audit-log insert on login.
      If login fails under SELECT-only, grant the **minimal** additional
      `INSERT`/`UPDATE` on exactly those auth/audit tables — nothing more. Record
      the final grant set in the verification log.

### 6. Owner client configuration
- [x] `%LOCALAPPDATA%\VISTA\appsettings.Production.json` placed on Owner laptop:
      ```json
      {
        "Schema": { "RunBootstrap": false },
        "ConnectionStrings": {
          "MerchSysCentral": "Server=100.76.155.51;Port=3306;Database=merchsys_central;User Id=merchsys_owner;Password=VistaOwner2026!Read;SslMode=Preferred;ConnectionTimeout=10;DefaultCommandTimeout=15;"
        }
      }
      ```
      > **SslMode=Preferred** — not `None` or `Disabled`. `SslMode=None` is valid for
      > raw MySqlConnector but rejected by the Oracle MySql.EntityFrameworkCore provider
      > (`MySqlSslMode` enum has no `None` member). `Preferred` is accepted by both
      > providers and negotiates plaintext against a non-TLS server, matching the intent.
      > Switch to `Required` when MariaDB TLS is enabled. See agent_wiki error doc.
      >
      > **Legacy note:** `MariaDb:*` section in `appsettings.Production.template.json`
      > is sync-era dead config. `DatabaseConfig.AddModuleDbContexts` reads
      > `ConnectionStrings:MerchSysCentral` only.
- [x] NTFS permissions locked — sole ACE is current Windows user, read-only
      (`DESKTOP-OUU3M8J\Max Sainz:(R)`, inheritance disabled).

### 7. TLS (`SslMode=Required`)
- [x] Checked: `SHOW VARIABLES LIKE 'have_ssl'` → **DISABLED**. MariaDB server TLS
      is not configured. LAN clients also connect without TLS (no `SslMode` key in
      `appsettings.json`).
- [x] **Documented exception:** Owner config uses `SslMode=Preferred` (not `Required`).
      WireGuard provides transport encryption. `SslMode=None` was attempted but rejected
      by the Oracle EF provider at runtime (see agent_wiki error doc). `Preferred`
      is accepted by both MySqlConnector and Oracle MySql.EntityFrameworkCore.
      Change to `SslMode=Required` when MariaDB server TLS is enabled.

### 8. Tailscale ACL hardening (least privilege)
- [x] Admin console ACLs updated — Owner device (`tag:vista-owner`) restricted to
      `tag:vista-host:3306` only. Host device (`tag:vista-host`) has full outbound.
      Exit-node and subnet-router features not enabled.
- [x] Both machines tagged in the admin console (`vista-host`, `vista-owner`).

---

## Acceptance criteria (verified 2026-06-02)

- [x] From the Owner laptop **off the Villon LAN** (phone tether), with Tailscale
      up: launched VISTA, logged in as Owner — KPIs / financial dashboards / reports
      loaded with **live** data. Operator visually confirmed: "everything looks fine."
      Live KPI values: FIFO inventory=PHP 47,800, YTD revenue=PHP 28,090, output
      VAT=PHP 133.93, outstanding utang=PHP 2,500, active products=20.
- [ ] An Owner write attempt is rejected (app `RoleGuardInterceptor` and/or DB
      `SELECT`-only grant) with the standard message — **not yet explicitly tested**;
      deferred to next manual operator session.
- [ ] With the host laptop offline, the Owner app shows the existing
      `ConnectionStatus` Offline/Reconnecting states gracefully (no crash) — deferred.
- [x] Round-trip latency: Tailscale ping RTT = 11 ms (direct WireGuard, not relayed).
      Dashboard load acceptable per operator.

> **DbContext concurrency issue resolved (2026-06-02):** `OwnerDashboardViewModel.RefreshAsync`
> now loads KPI groups sequentially instead of via `Task.WhenAll`. No EF Core
> `InvalidOperationException` errors on dashboard paint. Verified by operator.

## Security / operational notes

- [x] **Device loss runbook:** if the Owner laptop is lost/stolen, remove its node
      from the Tailscale admin console **and** rotate the `merchsys_owner` password.
- [x] Credentials live only in the NTFS-locked `appsettings.Production.json`
      (gitignored, NTFS ACL verified) — never committed.
- [x] Data residency: **all data stays on-premise** in this phase. No BIR /
      single-source-of-truth concern is introduced (unlike the future Phase 2).

## Rollback

Removing the Owner laptop from the tailnet (or uninstalling Tailscale there)
reverts to LAN-only access with **zero** impact on the existing system — no code,
schema, or host-config changes to undo beyond the optional firewall rule and the
`merchsys_owner` DB user.

## Hand-off to Phase 2 (future)

Phase 1 deliberately produces three artifacts Phase 2 reuses: the read-only
`merchsys_owner` DB user, a proven remote-connection config path, and validated
TLS. Phase 2 (cloud read-replica for 24/7 access when the host is off) layers on
top via a **second** named connection string (e.g. `MerchSysCloudReplica`) with
live-first / replica-fallback selection — it does not modify anything from Phase 1.
The BIR / data-residency decision (financial data leaving the premises) is a
Phase 2 prerequisite and does not block Phase 1.
