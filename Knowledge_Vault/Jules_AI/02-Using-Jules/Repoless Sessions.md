---
title: Repoless Sessions
type: guide
section: Using Jules
tags: [jules/tasks, jules/api]
source: https://jules.google/docs/changelog/2025-11-20
captured: 2026-09-12
aliases: [Start from scratch, Repoless]
---

# Repoless Sessions

Start a task with **no repository attached** — Jules spins up an ephemeral cloud dev environment with Node, Python, Rust, Bun and other runtimes preloaded, and works from your prompt alone.

## In the web app

Click the **X** next to the selected repo to clear it, then prompt as normal. Shipped Nov 2025 to remove the "create an empty GitHub repo first" detour when prototyping or writing a quick script.

## Via the API

Repoless support reached the REST API in Jan 2026: `sourceContext` becomes optional on session creation, and you download the **file outputs** when the session finishes. Google's framing is that a single API call spawns a serverless dev environment that happens to contain a coding agent.

```bash
PROMPT_CONTENT=$(jq -Rs . < my_prompt.md)

curl 'https://jules.googleapis.com/v1alpha/sessions' \
  -X POST \
  -H "Content-Type: application/json" \
  -H "x-goog-api-key: $JULES_API_KEY" \
  -d "{\"prompt\": $PROMPT_CONTENT}"
```

Retrieving the work: the completed session's change set comes back as a **git patch** — see [[Activities Endpoint]] and the `GitPatch` type in [[Types Reference]].

## Good fits

- Prototypes and spikes you may never keep
- One-off scripts and data munging
- Scaffolding a new project before it has a home ([[Prompting Jules|"start from scratch" prompts]])
- Programmatic, repo-free compute with an agent attached

**Sources:** <https://jules.google/docs/changelog/2025-11-20> · <https://jules.google/docs/changelog/2026-01-26-4>
