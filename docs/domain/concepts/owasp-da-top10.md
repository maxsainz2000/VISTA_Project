---
type: concept
title: "OWASP Desktop Application Security Top 10 (2021)"
aliases: [OWASP DA Top 10, DA1-DA10, desktop security]
sources: [Sources/system_plan.md]
related: [modular-monolith, client-server-Windows Forms]
last-updated: 2026-05-02
---

# OWASP DA Top 10 (2021)

## Definition

The OWASP Desktop Application Security Top 10 defines the most critical security risks for desktop applications. VISTA must comply with all 10.

## Requirements

| ID | Risk | VISTA Mitigation |
|---|---|---|
| DA1 | Injections | Parameterized queries via EF Core — no raw SQL |
| DA2 | Broken Authentication | Role-based login; Owner role is read-only at data-access layer |
| DA3 | Sensitive Data Exposure | No plaintext credentials in config; encrypted connection strings |
| DA4 | Improper Cryptography | Standard .NET cryptographic APIs |
| DA5 | Improper Authorization | Owner cannot write — enforced at repository, not just UI |
| DA6 | Security Misconfiguration | Hardened default config; no debug flags in release |
| DA7 | Insecure Communication | TLS for MariaDB TCP 3306 connections over LAN |
| DA8 | Poor Code Quality | Linting, code review, modular architecture |
| DA9 | Using Components with Known Vulns | NuGet audit; keep dependencies updated |
| DA10 | Insufficient Logging | Audit columns on every table; user, action, timestamp |

> [!IMPORTANT]
> **DA5 is critical:** Owner read-only must be enforced at the **data-access layer**, not just by hiding UI buttons. If a UI bypass would allow writes, it's a DA5 violation.

## Source References

- [[wiki/sources/system-plan|System Plan]] — security requirements section
