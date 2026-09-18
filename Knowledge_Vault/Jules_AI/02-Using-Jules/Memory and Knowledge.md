---
title: Memory and Knowledge
type: guide
section: Using Jules
tags: [jules/setup, jules/environment]
source: https://jules.google/docs/changelog/2025-09-30
captured: 2026-09-12
aliases: [Jules memory, Agentic memories]
---

# Memory and Knowledge

Jules learns from how you correct it, per repository. Shipped September 2025.

## How it works

- During a task, Jules saves your preferences, nudges, and corrections.
- On the next similar task **in that same repository**, it references those memories to anticipate your patterns — more accurate results with less steering.
- Toggle it in the repo settings page under **Knowledge**.

The Gemini 3 Pro upgrade (Nov 2025) described "agentic memories" using context more effectively so Jules adapts to coding preferences and project nuances more reliably over time.

## Memory vs AGENTS.md

| | [[AGENTS.md File]] | Memory |
| --- | --- | --- |
| Written by | You, deliberately | Jules, from your corrections |
| Scope | The repo, versioned in git | The repo, stored by Jules |
| Visible to teammates | Yes | No |
| Best for | Standing rules, build/test commands, conventions | Preferences you keep restating in chat |

Use both: put anything you'd want a human contributor to know in `AGENTS.md`, and let memory absorb the rest.

**Source:** <https://jules.google/docs/changelog/2025-09-30>
