---
title: Glossary
type: reference
section: Reference
tags: [jules]
captured: 2026-09-12
---

# Glossary

**Activity** — a single event inside a session (plan generated, message, progress update, completion). Immutable and event-sourced. → [[Activities Endpoint]]

**AGENTS.md** — repo-root file Jules reads for conventions and context. → [[AGENTS.md File]]

**Artifact** — output attached to an activity: a change set (git patch), bash output, or media. → [[Types Reference]]

**Automation mode** — session setting; `AUTO_CREATE_PR` opens a PR automatically when changes are ready.

**Continuous AI** — Google's term for suggested + scheduled tasks plus deploy-failure repair working as one loop. → [[Continuous AI]]

**Critic / Planning Critic** — internal reviewer agents that challenge proposed code and plans before execution. → [[Planning and Approval]]

**Environment snapshot** — saved VM state produced by **Run and Snapshot**, reused to speed up later tasks. → [[Environment Setup]]

**Interactive Plan** — planning mode where Jules asks clarifying questions before proposing steps.

**Jules Tools** — the `@google/jules` CLI and its TUI. → [[Jules Tools CLI]]

**MCP server** — external tool server Jules can call mid-session (Linear, Supabase, …). → [[MCP Servers]]

**Memory / Knowledge** — per-repository store of your corrections and preferences. → [[Memory and Knowledge]]

**Proactivity** — the per-repo toggle that enables Suggested Tasks. → [[Suggested Tasks]]

**Reactive Mode** — setting that limits Jules to PR comments mentioning `@Jules`.

**Repoless session** — a task with no repository attached, running in an ephemeral environment. → [[Repoless Sessions]]

**Session** — a continuous unit of work in one context, started from a prompt. The core API resource. → [[Sessions Endpoint]]

**Source** — a connected repository, as the API sees it. → [[Sources Endpoint]]

**Setup script** — the commands Jules runs to prepare the VM (install, lint, test). → [[Environment Setup]]

**Suggested task** — work Jules proposes on its own from TODOs and performance findings. → [[Suggested Tasks]]
