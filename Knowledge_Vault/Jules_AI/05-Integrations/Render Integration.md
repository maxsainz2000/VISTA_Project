---
title: Render Integration
type: guide
section: Integrations
tags: [jules/integration, jules/automation]
source: https://jules.google/docs/integrations/render
captured: 2026-09-12
---

# Render Integration

Connect Jules to Render and deployment failures become fixes: it watches builds, analyses errors, and pushes commits before you switch context.

## Prerequisites

1. **Enable Pull Request Previews** in your Render dashboard.
2. **Verify GitHub permissions** at <https://github.com/settings/installations> — find **Google Labs Jules**; if there's a **Review Request** link, click it to accept the updated permissions. No link means you're current.

## Setup

1. **Provision a key** — Render Dashboard → **Help menu** (top right) → **Coding Agents**. (Direct: <https://dashboard.render.com/jules>.)
2. **Create the key** — click **Create API key** and copy it.
   > [!warning] Shown once
   > If you lose the key you must provision a new one.
3. **Connect to Jules** — in Jules go to **Settings → Integrations** (<https://jules.google.com/settings/integrations>), paste the key, submit.

## What happens then

1. Jules opens a PR after you accept a plan.
2. If the Render build for that PR fails, Jules analyses the logs automatically.
3. It writes a fix and pushes a new commit to the same branch.
4. You review the fix like any other change and merge.

> [!warning] Only Jules' own PRs
> Jules monitors and fixes build failures only on **pull requests it created**. It does not debug failures on PRs opened by you or teammates.

This is the third leg of [[Continuous AI]], alongside [[Scheduled Tasks]] and [[Suggested Tasks]]. The GitHub Actions equivalent — CI Fixer — arrived Feb 2026; see [[Release History 2026]].

**Source:** <https://jules.google/docs/integrations/render>
