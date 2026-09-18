---
title: CLI Examples
type: guide
section: CLI
tags: [jules/cli, jules/automation]
source: https://jules.google/docs/cli/examples
captured: 2026-09-12
aliases: [CLI scripting]
---

# CLI Examples

Jules Tools is designed to be scriptable and to compose with other command-line tools.

## Create sessions from a TODO.md

Each line becomes its own session in the current repository.

```bash
cat TODO.md | while IFS= read -r line; do
  jules remote new --repo . --session "$line"
done
```

## Create a session from a GitHub issue

Pipes the title of the first issue assigned to you into a new session. Requires `gh` and `jq`.

```bash
gh issue list --assignee @me --limit 1 --json title \
  | jq -r '.[0].title' \
  | jules remote new --repo .
```

## Let Gemini pick the worst job and hand it to Jules

Uses the Gemini CLI to find the most tedious assigned issue and pipe its title into Jules.

```bash
gemini -p "find the most tedious issue, print it verbatim\n$(gh issue list --assignee @me)" \
  | jules remote new --repo .
```

## Patterns worth stealing

- `--parallel 3` on one prompt to get several independent attempts, then pick the best diff
- `jules remote pull --session <id>` to test work-in-progress locally before it's committed
- `jules remote list --session` piped through `jq` for status dashboards

Video walkthrough referenced by the docs: <https://www.youtube.com/embed/D_8ugbyW2eo>

**Source:** <https://jules.google/docs/cli/examples>
