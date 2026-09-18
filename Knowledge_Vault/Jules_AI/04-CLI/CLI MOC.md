---
title: CLI MOC
type: index
section: CLI
tags: [jules, moc, jules/cli]
captured: 2026-09-12
---

# Jules Tools (CLI)

The terminal surface for Jules: create and monitor cloud sessions, pull work-in-progress code locally, and pipe Jules into scripts.

- [[Jules Tools CLI]] — install, auth, every command and flag, the TUI
- [[CLI Examples]] — piping and scripting with `gh`, `jq`, and the Gemini CLI

## Cheat sheet

```bash
npm install -g @google/jules     # install
jules login                      # authenticate
jules                            # interactive dashboard (TUI)
jules remote list --repo         # connected repositories
jules remote list --session      # sessions, active and past
jules remote new --session "…"   # new task (repo inferred from cwd)
jules remote pull --session ID   # pull results locally
```

## When to use which surface

| Want to… | Use |
| --- | --- |
| Review a plan and diffs comfortably | Web app — [[Reviewing Code Changes]] |
| Fire off tasks without leaving the terminal | CLI |
| Build automation, bots, or CI steps | [[API MOC]] |
| React to GitHub events | [[GitHub Actions]] |

The CLI is a client of the same service as the [[API Overview|REST API]], so anything the CLI does can be scripted directly against the API too.
