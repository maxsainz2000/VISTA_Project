---
title: Errors and Failures
type: guide
section: Using Jules
tags: [jules/troubleshooting]
source: https://jules.google/docs/errors/
captured: 2026-09-12
aliases: [Troubleshooting, Debugging Jules]
---

# Errors and Failures

## How errors surface

Two places: the **activity feed** (the failed step is logged) and a **notification badge** (red dot) in the UI. Both appear whether the task failed outright or just needs you.

## Automatic retries

Jules retries failed steps where it can — network hiccups, transient install errors, slow dependency resolution. After repeated retries it marks the task **failed**.

## Most common causes

1. Incomplete or missing **environment setup scripts** — [[Environment Setup]]
2. Prompts that are vague or too broad — [[Prompting Jules]]
3. Repos with unusual or nonstandard build systems
4. **Long-running processes** (like `npm run dev`) in the setup script

## Debugging loop

- Click **rerun** from the task summary view, or
- Modify the setup script or prompt first, then restart

Address the specific feedback in the failure logs before rerunning — a rerun without a change usually fails the same way.

## Checklist when a task keeps failing

- Does the setup script exit cleanly on its own?
- Does it run in a clean checkout, with no local state?
- Are required environment variables configured? — [[Environment Variables]]
- Is the prompt narrow enough to finish inside one task?
- Does `AGENTS.md` explain how to build and test? — [[AGENTS.md File]]
- Are you at your daily/concurrent limit? — [[Plans and Limits]]

## Related automation

Since Feb 2026 Jules auto-fixes **CI failures** on PRs it created, and the [[Render Integration]] does the same for failed deploy builds.

**Source:** <https://jules.google/docs/errors/>
