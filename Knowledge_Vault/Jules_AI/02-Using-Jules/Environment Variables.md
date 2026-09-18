---
title: Environment Variables
type: guide
section: Using Jules
tags: [jules/environment, jules/setup, jules/security]
source: https://jules.google/docs/changelog/2025-10-01
captured: 2026-09-12
---

# Environment Variables

Repository-level variables that give Jules the project-specific configuration it needs to run builds, execute tests, or talk to services. Shipped October 2025.

## How it works

1. **Add them in repo settings** — open the repository's settings page and add the variables; they're bound to that project.
2. **Enable per task** — when starting a task you choose whether to make them available to it.
3. **Task-long access** — once enabled for a task, Jules has them for that task's full duration.

> [!warning] Not changeable mid-task
> The setting can't be modified after a task has begun. Decide before you hit go.

## Practical notes

- This is the supported way to hand Jules config — **not** committing a `.env` to the repo, which the [[FAQ|security guidance]] explicitly warns against.
- Variables complement the [[Environment Setup|setup script]]: the script installs, the variables configure.
- Anything the variables grant access to is reachable from a VM with internet access. Scope credentials tightly and prefer read-only or throwaway keys.

**Source:** <https://jules.google/docs/changelog/2025-10-01>
