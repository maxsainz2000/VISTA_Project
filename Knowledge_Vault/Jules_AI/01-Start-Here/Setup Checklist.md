---
title: Setup Checklist
type: guide
section: Start Here
tags: [jules/setup, jules/environment, jules/security]
source: https://jules.google/docs/
captured: 2026-09-12
---

# Setup Checklist

A single pass that gets Jules from "signed in" to "productive", pulling together steps the docs spread across several pages. Tick top to bottom.

## Account

- [ ] Signed in at <https://jules.google.com> with a Google account (18+)
- [ ] Privacy notice accepted
- [ ] GitHub connected; the **Google Labs Jules** app has access to the repos you care about
- [ ] Browser notifications enabled (Settings → Notifications)
- [ ] Checked which tier you're on — [[Plans and Limits]]

## Per repository

- [ ] `AGENTS.md` committed at the repo root — [[AGENTS.md File]]
- [ ] Setup script configured under the repo's **Configuration → Initial Setup** — [[Environment Setup]]
- [ ] Setup script validated with **Run and Snapshot** (creates a reusable environment snapshot)
- [ ] Environment variables added at repo level if builds/tests need them
- [ ] **Knowledge → Memory** toggled the way you want it (Jules remembers corrections per repo)
- [ ] Decided on commit authorship: Jules / co-authored / you (Settings → Commit Authoring)

## Automation (optional, powerful)

- [ ] **Proactivity** toggled on for up to 5 repos → [[Suggested Tasks]]
- [ ] A recurring maintenance prompt set up → [[Scheduled Tasks]]
- [ ] `jules` label used on GitHub issues → [[GitHub Issues Trigger]]
- [ ] Workflow added under `.github/workflows/` → [[GitHub Actions]]
- [ ] Render key pasted into Settings → Integrations → [[Render Integration]]
- [ ] MCP service keys added if you use Linear / Supabase / Neon / etc. → [[MCP Servers]]

## Developer surfaces (optional)

- [ ] CLI installed: `npm install -g @google/jules`, then `jules login` → [[Jules Tools CLI]]
- [ ] API key generated at <https://jules.google.com/settings#api> (max 3) → [[Authentication]]
- [ ] `JULES_API_KEY` exported in your shell, and stored as a GitHub Actions secret

## Safety pass

> [!warning] Treat the VM as shared compute
> Jules runs your code in a cloud VM **with internet access**, and it acts on both code and non-code files in the repo.

- [ ] No secrets committed to the repo
- [ ] Dependencies audited; no known-vulnerable packages
- [ ] Issue-triggered automation restricted to an allowlist of users
- [ ] API keys never committed (exposed Google API keys are auto-disabled)

## Habits that pay off

- Scope prompts narrowly — see [[Prompting Jules]]
- Keep setup scripts fast and free of long-running processes (no `npm run dev`) — see [[Errors and Failures]]
- Review Jules' PRs like any teammate's

**Sources:** <https://jules.google/docs/> · <https://jules.google/docs/environment/> · <https://jules.google/docs/faq/>
