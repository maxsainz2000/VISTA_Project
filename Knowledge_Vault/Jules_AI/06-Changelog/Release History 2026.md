---
title: Release History 2026
type: changelog
section: Changelog
tags: [jules/changelog]
source: https://jules.google/docs/changelog/
captured: 2026-09-12
---

# Release History 2026

Newest first. Each entry links to its official page.

| Date | Release | In one line |
| --- | --- | --- |
| Mar 9 | [Gemini 3.1 Pro in Jules](https://jules.google/docs/changelog/2026-03-09) | New default model for Google Pro users, replacing Gemini 3 Pro |
| Feb 19 | [CI Fixer & commit authoring](https://jules.google/docs/changelog/2026-02-19) | Auto-fixes failed GitHub Actions checks; choose who authors commits |
| Feb 2 | [MCP support](https://jules.google/docs/changelog/2026-02-02) | Connect Linear, Stitch, Neon, Tinybird, Context7, Supabase |
| Jan 30 | [Gemini 3 Flash base model](https://jules.google/docs/changelog/2026-01-30) | Faster, more capable base model for every tier |
| Jan 26 | [Planning Critic](https://jules.google/docs/changelog/2026-01-26-1) | Second agent reviews auto-approved plans; 9.5% fewer failures |
| Jan 26 | [Performance optimizations](https://jules.google/docs/changelog/2026-01-26-3) | Suggested Tasks now surfaces bottlenecks, not just TODOs |
| Jan 26 | [Repoless API, file outputs, activity filters](https://jules.google/docs/changelog/2026-01-26-4) | Three REST API additions |
| Jan 26 | [Editable scheduled tasks](https://jules.google/docs/changelog/2026-01-26-5) | Edit, pause, resume instead of delete-and-recreate |

## Details worth keeping

### CI Fixer (Feb 19)

Jules detects and fixes CI failures on **pull requests it created**. When a GitHub Actions check fails, it receives the error, works out a fix, and resubmits — no manual log copy-pasting or REST API glue. It operates in a loop: fix, commit, resubmit. → [[Errors and Failures]], [[GitHub Actions]]

### Commit authoring (Feb 19)

Three attribution modes, set at user level across all repos and all task types (scheduled, suggested, manual):

- **Jules** — sole author of all commits (default, prior behaviour)
- **Co-authored** — Jules + you, or you + Jules, crediting both
- **User only** — you are the sole author; Jules applies changes under your identity

Set it in **Settings → Commit Authoring**; it applies to future sessions. The motivation: your GitHub contribution graph previously didn't reflect work you initiated. → [[Reviewing Code Changes]]

### MCP support (Feb 2)

API-key based connections to a vetted list of servers. Jules calls their tools when it detects a need. → [[MCP Servers]]

### Planning Critic (Jan 26)

A secondary agent critiques and refines every plan that doesn't get human review, before any code is written. Slightly longer planning, **9.5% reduction in task failure rates**, higher-quality execution paths. → [[Planning and Approval]]

### REST API additions (Jan 26)

- **Repoless sessions** — one API call spawns an ephemeral dev environment with Node, Python, Rust, Bun and other runtimes, driven only by your prompt. → [[Repoless Sessions]]
- **Full file outputs** — the completed session's whole change set, in git patch format, covering additions, modifications, deletions. → [[Activities Endpoint]]
- **`createTime` activity filter** — activities are immutable and event-sourced, so a timestamp cursor lets you fetch only what's new and cache the rest. → [[API Quickstart]]

**Source:** <https://jules.google/docs/changelog/>
