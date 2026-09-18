---
title: Suggested Tasks
type: guide
section: Using Jules
tags: [jules/automation, jules/tasks]
source: https://jules.google/docs/suggested-tasks/
captured: 2026-09-12
aliases: [Proactivity, Proactive suggestions]
---

# Suggested Tasks

Jules scans your codebase on its own, finds work worth doing, and proposes it with a plan attached.

## Enabling it

Proactivity is **opt-in per repository**:

1. Go to **Codebases** in the left panel
2. Select the repository
3. Open the **Proactivity** tab
4. Toggle **Proactivity** to **On**

Scanning starts immediately; suggestions appear within minutes if anything resolvable is found.

> [!warning] Capped at 5 repositories
> At launch you can enable proactivity on up to **five** repos. The feature is experimental and was announced for Google AI Pro and Ultra subscribers.

## What it looks for

- **`#TODO` comments** that describe resolvable tasks (the launch scope)
- **Performance optimizations** — added Jan 2026, surfaced alongside TODOs

Each suggestion carries context, a rationale, and a confidence score for acting independently.

## Reviewing and running

1. Review the suggestion card on your dashboard (periodic emails notify you of new ones)
2. Click it to see context and rationale
3. Click **Start** to let Jules work on it

You can kick off several in parallel.

## Dismissing

Dismiss anything irrelevant or low priority — Jules uses that feedback to tune future suggestions.

> [!tip] No suggestions appearing?
> Google's own advice: that usually means your code is tidy. Prompt Jules to analyse the repo and raise specific inline TODOs; once committed, they surface as suggestions.

## Limits recap

- Scans run periodically
- Up to five repositories
- Current scope: `#TODO` comments plus performance opportunities

**Source:** <https://jules.google/docs/suggested-tasks/>
