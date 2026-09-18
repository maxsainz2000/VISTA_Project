---
title: GitHub Actions
type: guide
section: Integrations
tags: [jules/integration, jules/automation, jules/github, jules/security]
source: https://github.com/google-labs-code/jules-action
captured: 2026-09-12
aliases: [jules-invoke, Jules Action]
---

# GitHub Actions

The official action triggers Jules from any GitHub event — issues, pull requests, schedules, or manual dispatch.

## Setup

1. **Get an API key** — authenticate at <https://jules.google.com>, generate a key in settings ([[Authentication]]).
2. **Add it as a secret** — repo **Settings → Secrets and variables → Actions → New repository secret**, named `JULES_API_KEY`.
3. **Add a workflow** under `.github/workflows/`.

```yaml
name: Daily Security Scan

on:
  schedule:
    - cron: '0 6 * * *'   # every day at 6 AM
  workflow_dispatch:

jobs:
  scan:
    runs-on: ubuntu-latest
    permissions:
      contents: read
    steps:
      - uses: google-labs-code/jules-invoke@v1
        with:
          prompt: |
            You are a security agent. Scan for vulnerabilities and fix them:

            CRITICAL (fix immediately):
            - Hardcoded secrets, API keys, passwords
            - SQL injection, command injection
            - Missing auth on sensitive endpoints

            Rules:
            - Fix highest severity first
            - Keep changes under 100 lines
            - Run tests before creating PR
          jules_api_key: ${{ secrets.JULES_API_KEY }}
```

## Inputs

| Input | Description | Default |
| --- | --- | --- |
| `prompt` | **Required.** The task for Jules. | — |
| `jules_api_key` | **Required.** Your API key (use secrets). | — |
| `starting_branch` | Branch to start from. | `main` |
| `include_last_commit` | Include the last commit's diff as context. | `false` |
| `include_commit_log` | Include recent commit history as context. | `false` |

## Example workflows in the repo

| Example | Trigger | Purpose |
| --- | --- | --- |
| `weekly-cleanup` | cron | Automated maintenance and refactoring |
| `performance-improver` | daily cron | Hunt and fix performance bottlenecks |
| `bug-fixer` | issue labelled `bug` | Diagnose and fix bugs with structured prompts |
| `ci-failure-fix` | CI workflow fails | Automatically fix failed builds |
| `unblocked-issues` | issue closed | Pick up issues that were blocked by it |

## Writing prompts for scheduled agents

Jules does best with a **measurable target it can verify itself** — run the benchmark, optimise, re-run, and only open a PR on a real gain. Give it a persona, a prioritised checklist, and hard constraints ("don't change public API contracts", "all existing tests must pass"). See [[Prompting Jules]].

Other ideas from the repo: security audit agent (`npm audit` → fix), dependency updater (update → test → PR if green), docs updater, test-coverage improver targeting 90%.

## Security

> [!warning] Issue-triggered workflows are an attack surface
> Anyone can often open an issue, so restrict who can trigger Jules with an allowlist condition:

```yaml
- name: Invoke Jules
  if: ${{ contains(fromJSON('["trusted-user", "another-user"]'), github.event.issue.user.login) }}
  uses: google-labs-code/jules-invoke@v1
  with:
    prompt: ...
```

- Never commit `JULES_API_KEY` — always use Actions secrets
- Treat Jules like any team member: review its PRs before merging

Since Feb 2026 Jules also **auto-fixes CI failures** on PRs it created, without a workflow of your own — see [[Release History 2026]].

**Source:** <https://github.com/google-labs-code/jules-action>
