---
title: Integrations MOC
type: index
section: Integrations
tags: [jules, moc, jules/integration]
captured: 2026-09-12
---

# Integrations

Ways Jules connects to the rest of your stack — and, more importantly, ways it starts working **without you typing a prompt**.

- [[Integrations Overview]] — the connection model and its security posture
- [[Render Integration]] — watch deploys, fix failed builds on its own PRs
- [[GitHub Actions]] — trigger Jules from any workflow event, with inputs and example workflows
- [[GitHub Issues Trigger]] — label an issue `jules`
- [[MCP Servers]] — Linear, Stitch, Neon, Tinybird, Context7, Supabase

## Trigger comparison

| Trigger | Starts work when | Configure in |
| --- | --- | --- |
| Issue label | An issue is labelled `jules` | GitHub |
| GitHub Action | Any workflow event (cron, PR, issue, dispatch) | `.github/workflows/` |
| Render | A preview build fails on a Jules PR | Jules Settings → Integrations |
| Scheduled task | A cadence you set | Jules dashboard — [[Scheduled Tasks]] |
| Suggested task | Jules finds a TODO or a bottleneck | Repo → Proactivity — [[Suggested Tasks]] |
| CI Fixer | A GitHub Actions check fails on a Jules PR | Automatic since Feb 2026 |

> [!warning] Anything publicly triggerable needs an allowlist
> See the security section of [[GitHub Actions]].

Together these form what Google calls [[Continuous AI]]. Anything not listed here can be built on the [[API MOC|REST API]].
