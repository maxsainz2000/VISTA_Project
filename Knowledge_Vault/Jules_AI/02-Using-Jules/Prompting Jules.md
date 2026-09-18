---
title: Prompting Jules
type: guide
section: Using Jules
tags: [jules/prompting]
source: https://github.com/google-labs-code/jules-awesome-list
captured: 2026-09-12
aliases: [Prompt library, Awesome Jules prompts]
---

# Prompting Jules

## Principles

- **Scope it.** One outcome per task. "Fix everything" produces nothing useful.
- **Name the thing.** File, function, endpoint, error message — precision removes guesswork. A file selector exists in the UI for exactly this (Sept 2025).
- **Give a target it can measure.** Jules can run your benchmarks or tests and iterate until they pass. "Only open a PR if you achieve measurable gains" works.
- **State constraints.** "Don't change public API contracts", "keep changes under 100 lines", "all existing tests must pass".
- **Push context into the repo.** Standing rules belong in [[AGENTS.md File]], not re-typed every prompt.
- **Use images** for anything visual — [[Running Tasks]].

## Prompt library

Adapted from Google Labs' *Awesome Jules Prompts*. Replace `{braces}` with specifics.

**Everyday**
- Refactor `{file}` from `{x}` to `{y}`
- Add a test suite for `{module}`
- Add type hints to `{python function}`
- Generate mock data for `{schema}`
- Convert these CommonJS modules to ES modules
- Turn this callback-based code into async/await

**Debugging**
- Help me fix `{error}`
- Why is `{snippet}` slow?
- Trace why this value is undefined
- Diagnose this memory leak
- Add logging to help debug this issue
- Find race conditions in this async code

**Documentation**
- Write a README for this project
- Add comments to this code
- Write API docs for this endpoint
- Generate Sphinx-style docstrings for `{module}`

**Testing**
- Add integration tests for this API endpoint
- Write a test that mocks `fetch`
- Convert this test from Mocha to Jest
- Generate property-based tests for this function
- Write a test to ensure backward compatibility for this function

**Dependencies**
- Upgrade my linter and autofix breaking config changes
- Which dependencies can I safely remove?
- Check if these packages are still maintained
- Set up Renovate or Dependabot for auto-updates

**Codebase intelligence**
- Analyze this repo and generate 3 feature ideas
- Identify tech debt in this file
- Find duplicate logic across files
- Help me scope this issue so Jules can solve it
- Convert this function into a reusable plugin/module

**Context and reporting**
- Write a status update based on recent commits
- Summarize all changes in the last 7 days

**From scratch** (pairs well with repoless sessions)
- What's going on in this repo?
- Initialize a new Express app with CORS enabled
- Set up a monorepo using Turborepo and PNPM
- Bootstrap a Python project with Poetry and Pytest
- Create a starter template for a Chrome extension

## Prompts for automation

Scheduled and Actions-triggered prompts should read like a job description with rules — a persona, a prioritised list of what to look for, and hard constraints. See [[GitHub Actions]] and [[Scheduled Tasks]].

**Sources:** <https://github.com/google-labs-code/jules-awesome-list> · <https://jules.google/docs/running-tasks/>
