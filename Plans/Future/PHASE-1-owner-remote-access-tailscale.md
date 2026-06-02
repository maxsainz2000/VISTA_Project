---
title: "Phase 1 — Owner Remote Read Access via Tailscale (WireGuard mesh VPN)"
status: proposed
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
- [ ] Sign up for Tailscale (Personal/free plan) using a single owner-controlled
      SSO identity. Confirm no credit card is requested.
- [ ] This account's tailnet is where both laptops will live.

### 2. Host laptop (runs XAMPP / MariaDB)
- [ ] Install Tailscale (`https://tailscale.com/download`), sign in to the tailnet.
- [ ] Record the host's Tailscale IPv4 (`tailscale ip -4` → `100.x.y.z`).
- [ ] (Recommended) Enable MagicDNS and note the stable name (e.g. `villon-host`)
      so the Owner config doesn't depend on a memorized IP.
- [ ] In the Tailscale admin console, **disable key expiry** for the host node so
      the tunnel does not drop after the default expiry window.

### 3. Owner laptop
- [ ] Install Tailscale, sign in to the **same** tailnet.
- [ ] Verify connectivity from off-LAN: `tailscale ping villon-host` (or the IP)
      succeeds while tethered to a phone / external network.

### 4. MariaDB network exposure (host laptop, least privilege)
- [ ] In `C:\xampp\mysql\bin\my.ini`: confirm MariaDB listens on the Tailscale
      interface — `bind-address = 0.0.0.0` (listen all) **and** ensure
      `skip-networking` is **not** set.
- [ ] **Windows Firewall:** add an inbound rule allowing **TCP 3306 only from the
      Tailscale CGNAT range `100.64.0.0/10`** (and/or the Tailscale interface).
      Do **not** broaden 3306 to the public internet or the whole LAN beyond what
      is already required for existing LAN clients.
- [ ] Confirm there is **no router port-forward** to 3306.

### 5. Dedicated read-only DB user for the Owner (defense-in-depth)
- [ ] Create a remote login restricted to `SELECT`:
      ```sql
      CREATE USER 'merchsys_owner'@'%' IDENTIFIED BY '<strong-password>';
      GRANT SELECT ON merchsys_central.* TO 'merchsys_owner'@'%';
      FLUSH PRIVILEGES;
      ```
      (Tighten `'%'` to the Tailscale subnet/host if your MariaDB version permits.)
- [ ] **Validate the login flow under SELECT-only** (during the test phase). The
      Owner session may legitimately need a few writes that pure `SELECT` blocks:
      - login bookkeeping (last-login timestamp, failed-attempt / lockout counters
        on `Sys_UserAccounts`),
      - Owner self-service password change (`AuthSelfService`, allowed by
        `RoleGuardInterceptor`),
      - any audit-log insert on login.
      If login fails under SELECT-only, grant the **minimal** additional
      `INSERT`/`UPDATE` on exactly those auth/audit tables — nothing more. Record
      the final grant set in the verification log.

### 6. Owner client configuration
- [ ] Place `appsettings.Production.json` at `%LOCALAPPDATA%\VISTA\` on the Owner
      laptop. **Override the key the app actually consumes** —
      `ConnectionStrings:MerchSysCentral` — pointing at the Tailscale host:
      ```json
      {
        "ConnectionStrings": {
          "MerchSysCentral": "Server=villon-host;Port=3306;Database=merchsys_central;User Id=merchsys_owner;Password=<strong-password>;SslMode=Required;ConnectionTimeout=10;DefaultCommandTimeout=15;"
        }
      }
      ```
      > **Note / codebase discrepancy:** the `MariaDb:` section in
      > `appsettings.Production.template.json` is **legacy sync-era config** and is
      > *not* read by `DatabaseConfig.AddModuleDbContexts` (which calls
      > `configuration.GetConnectionString("MerchSysCentral")` + `UseMySQL`). Set
      > `ConnectionStrings:MerchSysCentral`, not `MariaDb:*`.
- [ ] Restrict NTFS permissions so only the Owner's Windows user can read the file
      (`icacls`), per the existing operator instructions.

### 7. TLS (`SslMode=Required`)
- [ ] Confirm existing **LAN** clients already connect with `SslMode=Required`. If
      they do, the MariaDB server already has TLS configured and the Owner needs no
      additional server change — keep `SslMode=Required`.
- [ ] If the server does **not** have TLS configured, enable MariaDB server TLS
      (preferred, satisfies the OWASP transport requirement). WireGuard already
      encrypts the path end-to-end, but project policy is `SslMode=Required` — do
      not silently downgrade it; document any exception explicitly.

### 8. Tailscale ACL hardening (least privilege)
- [ ] In the admin console ACLs, restrict the Owner device so it can reach **only**
      `host:3306` and nothing else on the tailnet (free plan includes 3 ACL groups).
- [ ] Do **not** enable exit-node or subnet-router features unless separately
      required — keep the attack surface minimal.

---

## Acceptance criteria (verify in the testing phase)

- [ ] From the Owner laptop **off the Villon LAN** (phone tether), with Tailscale
      up: launch VISTA, log in as Owner, and confirm KPIs / financial dashboards /
      reports load with **live** data.
- [ ] An Owner write attempt is rejected (app `RoleGuardInterceptor` and/or DB
      `SELECT`-only grant) with the standard message — no silent overwrite.
- [ ] With the host laptop offline, the Owner app shows the existing
      `ConnectionStatus` Offline/Reconnecting states gracefully (no crash).
- [ ] Round-trip latency for dashboard load over the remote link is acceptable.

## Security / operational notes

- [ ] **Device loss runbook:** if the Owner laptop is lost/stolen, remove its node
      from the Tailscale admin console **and** rotate the `merchsys_owner` password.
- [ ] Credentials live only in the NTFS-locked `appsettings.Production.json`
      (gitignored) — never committed.
- [ ] Data residency: **all data stays on-premise** in this phase. No BIR /
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
