---
title: AGENTS.md File
type: guide
section: Using Jules
tags: [jules/setup, jules/prompting, jules/github]
source: https://jules.google/docs/
captured: 2026-09-12
aliases: [AGENTS.md, agents.md]
---

# AGENTS.md File

Jules automatically looks for a file named **`AGENTS.md` in the root of your repository** and uses it to generate more relevant plans and completions. Support landed with the June 2025 agent upgrade and has been reinforced since.

## What to put in it

The docs describe it as a place to document the agents or tools in your codebase — what they do, how to interact with them, and any input/output conventions. In practice it's the natural home for anything you'd tell a new contributor on day one:

- how to install, build, lint, and test
- directory layout and what lives where
- conventions the codebase follows (naming, error handling, commit style)
- things never to touch (generated files, vendored code, migrations)
- how to verify a change actually works

## Why it matters

`AGENTS.md` is read *before* Jules writes the plan, so it shapes the plan rather than correcting the code afterwards. It's the cheapest lever you have on output quality, and it helps your human teammates too.

> [!tip] Keep it current
> Google's own guidance: an out-of-date `AGENTS.md` hurts more than none. Treat it as part of the codebase.

Complements — not replaces — the [[Environment Setup|setup script]]: `AGENTS.md` is knowledge, the setup script is execution. See also [[Memory and Knowledge]] for corrections Jules learns on its own.

**Source:** <https://jules.google/docs/>
