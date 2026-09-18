---
title: GitHub Issues Trigger
type: guide
section: Integrations
tags: [jules/github, jules/automation]
source: https://jules.google/docs/running-tasks/
captured: 2026-09-12
aliases: [jules label]
---

# GitHub Issues Trigger

Label a GitHub issue **`jules`** (case-insensitive) and Jules starts a task from it. Shipped June 2025.

## How

1. Open the issue
2. Click the gear next to **Labels**
3. Add the label `jules`

Make sure the **Jules GitHub App** is authorised for that repository ([[Managing Tasks and Repos]]).

Shortly after, Jules comments on the issue automatically. When it finishes, it posts a link to the pull request with its work.

## Related behaviours

- **PR feedback** (Sept 2025) — Jules reads review comments on its PRs, marks each with 👀 as it reads, and pushes a commit with the requested changes. Switch to **Reactive Mode** in <https://jules.google.com/settings> to have it act only on comments that mention `@Jules`.
- For finer control over triggers and prompts, use [[GitHub Actions]] instead of labels.

> [!warning] Anyone can file an issue
> On public repos, label-triggered work is effectively open to strangers. Prefer an allowlist-guarded Action for anything sensitive.

**Sources:** <https://jules.google/docs/running-tasks/> · <https://jules.google/docs/changelog/2025-06-26>
