---
title: What is Jules
type: guide
section: Start Here
tags: [jules, jules/setup]
source: https://jules.google/docs/
captured: 2026-09-12
aliases: [Jules, Jules agent]
---

# What is Jules

Jules is an autonomous coding agent from **Google Labs**. You describe a task against a GitHub repository; Jules does the work in the background and hands back reviewable code.

## The execution model

Each task gets its **own fresh virtual machine**. Inside it Jules:

1. Clones your repository at the branch you picked
2. Installs dependencies (inferred, or from your [[Environment Setup|setup script]])
3. Reads the codebase — plus `AGENTS.md` and `README.md` for context ([[AGENTS.md File]])
4. Proposes a **plan** for your approval ([[Planning and Approval]])
5. Edits files, runs tests, and can browse the web for library docs
6. Produces a diff you can review, then a branch or pull request ([[Reviewing Code Changes]])

Because the work is asynchronous, you can close the tab. Notifications tell you when a plan is ready or the task is done.

## What it's good at

Per Google's own framing: bug fixes with test-verified patches, dependency and version bumps, scoped code transformations, migrations across languages or frameworks, isolated features, and opening PRs with runnable code and test results.

## Three surfaces

| Surface | Use it for | Note |
| --- | --- | --- |
| Web app (`jules.google.com`) | Everyday work, plan review, diffs | [[Running Tasks]] |
| CLI (`@google/jules`) | Terminal workflow, scripting, piping | [[Jules Tools CLI]] |
| REST API (`v1alpha`) | Custom automation, ChatOps, CI | [[API Overview]] |

## Autonomy levels

Jules can also start work without you typing a prompt:

- [[Scheduled Tasks]] — recurring prompts on a daily/weekly/monthly cadence
- [[Suggested Tasks]] — it scans the repo for `TODO`s and performance wins and proposes work
- [[GitHub Issues Trigger]] — label an issue `jules`
- [[GitHub Actions]] — fire on any workflow event
- [[Render Integration]] — repair its own failed deploy builds

## Status and model

Jules left beta in **August 2025**. The base model for all tiers became **Gemini 3 Flash** (Jan 2026), with **Gemini 3.1 Pro** for paid tiers (Mar 2026). See [[Release History 2026]].

**Source:** <https://jules.google/docs/>
