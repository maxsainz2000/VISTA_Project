---
title: Jules Tools CLI
type: reference
section: CLI
tags: [jules/cli]
source: https://jules.google/docs/cli/reference
captured: 2026-09-12
aliases: [jules CLI, Jules Tools, "@google/jules"]
---

# Jules Tools CLI

A lightweight command-line interface for Jules: manage coding sessions, inspect progress, and wire Jules into existing workflows and scripts without leaving the terminal. It's both a command surface and a dashboard.

## Install

```bash
npm install -g @google/jules
```

Also works with pnpm, or run without installing: `npx @google/jules`.

## Authenticate

```bash
jules login    # opens a browser for Google auth
jules logout
```

## Help

```bash
jules help              # general help
jules remote --help     # help for one command
```

## Global flags

| Flag | Effect |
| --- | --- |
| `-h`, `--help` | Help for `jules` or a specific command |
| `--theme <string>` | TUI theme: `dark` (default) or `light` — e.g. `jules --theme light` |

## Commands

### `version`

```bash
jules version
```

### `remote`

The primary way to work with cloud sessions.

**`remote list`** — list connected repos or sessions.

```bash
jules remote list --repo      # all repositories connected to Jules
jules remote list --session   # all active and past sessions
```

**`remote new`** — create a session. Jules can infer the repo from your working directory, so `--repo` is often unnecessary.

| Flag | Meaning |
| --- | --- |
| `--repo <repo_name>` | Repository (e.g. `torvalds/linux`, or `.` for the current directory's repo) |
| `--session "<prompt>"` | The task description |
| `--parallel <number>` | Start multiple parallel sessions on the same task (max 5) |

```bash
jules remote new --repo torvalds/linux --session "write unit tests"
```

**`remote pull`** — pull results (code changes) from a session onto your machine, so you can test work-in-progress without waiting for a commit.

```bash
jules remote pull --session 123456
```

### `completion`

```bash
jules completion bash   # shell autocompletion script
```

## Interactive dashboard (TUI)

```bash
jules
```

Running bare launches the terminal UI: a dashboard of sessions, a **side-by-side diff viewer**, and guided flows for creating sessions — much like the web app.

## Version history highlights

| Version | Change |
| --- | --- |
| `0.1.36` | Side-by-side diff viewer, test coverage, auto-approval and timeout fixes |
| `0.1.37` | PNPM installation fixes |
| `0.1.38` | Repository inference from the current directory |
| `0.1.39` | OAuth2 error handling and recovery |
| `0.1.40` | Credential fixes for WSL and Arch Linux |
| — | `--parallel` flag added to `remote new` |

Next: [[CLI Examples]]

**Sources:** <https://jules.google/docs/cli/reference> · <https://jules.google/docs/changelog/2025-11-10>
