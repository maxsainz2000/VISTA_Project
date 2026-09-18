---
title: Environment Setup
type: guide
section: Using Jules
tags: [jules/environment, jules/setup]
source: https://jules.google/docs/environment/
captured: 2026-09-12
aliases: [Setup script, VM, Virtual machine]
---

# Environment Setup

Every task runs in a short-lived **Ubuntu VM**. Jules clones the repo there, installs dependencies, and runs tests.

For simple projects Jules infers setup by studying the repo — it also reads `AGENTS.md` and `README.md` for hints. For complex projects, give it an explicit **setup script**.

## What's preinstalled

Node.js, Bun, Python, Go, Java, and Rust ship on the base image, alongside Docker, common linters/formatters, and CLI staples. Full captured version list: [[Preinstalled Tools]].

To dump the live inventory yourself, put this in a setup script and click **Run to Validate**:

```bash
set +x; . /opt/environment_summary.sh
```

## Adding a setup script

1. Click the repo in the left sidebar (under **Codebases**)
2. Select **Configuration** at the top
3. Enter the commands in the **Initial Setup** window

```bash
npm install
npm run test
```

## Validate and snapshot

Click **Run and Snapshot**. The script runs, you see the output, and on success Jules stores an **environment snapshot**.

That snapshot is reused for future tasks from this repository — a large speedup for projects with long installs. (Snapshots shipped Aug 2025.)

## Validation tips

- Check versions by adding commands like `node -v` and clicking **Run to Validate**
- Always include the commands that install packages, lint, or run tests
- Use validation to catch errors *before* a task wastes a run
- Keep setup lightweight and fast

> [!warning] No long-running processes
> `npm run dev`, watch scripts, and anything that doesn't exit will hang the setup. Use discrete install/test commands only.

## Disk

The VM has **20 GB** of disk (raised from a smaller cap in Aug 2025) — enough for large dependency trees and build artifacts.

Related: [[Environment Variables]] · [[Errors and Failures]] · [[Preinstalled Tools]]

**Source:** <https://jules.google/docs/environment/>
