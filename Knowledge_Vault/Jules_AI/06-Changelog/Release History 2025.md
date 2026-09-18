---
title: Release History 2025
type: changelog
section: Changelog
tags: [jules/changelog]
source: https://jules.google/docs/changelog/
captured: 2026-09-12
---

# Release History 2025

Newest first, from launch in May to the Continuous AI release in December.

## Q4

| Date | Release | In one line |
| --- | --- | --- |
| Dec 10 | [Suggested tasks](https://jules.google/docs/changelog/2025-12-10) | Jules scans repos for `#TODO`s and proposes fixes — Pro/Ultra, up to 5 repos |
| Dec 10 | [Scheduled tasks](https://jules.google/docs/changelog/2025-12-101) | Daily/weekly/monthly recurring prompts, available to all users |
| Dec 10 | [Render auto-fixes](https://jules.google/docs/changelog/2025-12-102) | Detect failed deploy builds, analyse logs, push fixes to its own PRs |
| Nov 20 | [Start from scratch](https://jules.google/docs/changelog/2025-11-20) | Repoless sessions — no GitHub repo required |
| Nov 19 | [Gemini 3 Pro](https://jules.google/docs/changelog/2025-11-19) | Coherent multi-step planning, visual verification, agentic memories |
| Nov 10 | [CLI updates](https://jules.google/docs/changelog/2025-11-10) | Side-by-side diffs, repo inference, `--parallel`, WSL/Arch auth fixes |
| Oct 3 | [Jules API](https://jules.google/docs/changelog/2025-10-03) | Programmatic sessions for ChatOps, Linear/Jira, CI/CD |
| Oct 2 | [Jules Tools CLI](https://jules.google/docs/changelog/2025-10-02) | `npm install -g @google/jules`; scriptable, with a TUI |
| Oct 1 | [Environment variables](https://jules.google/docs/changelog/2025-10-01) | Repo-level config, enabled per task |

## Q3

| Date | Release | In one line |
| --- | --- | --- |
| Sep 30 | [Memory](https://jules.google/docs/changelog/2025-09-30) | Jules remembers your corrections per repository |
| Sep 29 | [File selector](https://jules.google/docs/changelog/2025-09-29) | Point Jules at exact files to tighten context |
| Sep 23 | [PR feedback](https://jules.google/docs/changelog/2025-09-23) | Reads review comments, 👀 to acknowledge, pushes fixes; Reactive Mode for `@Jules` only |
| Sep 19 | [Talk Like a Pirate Day](https://jules.google/docs/changelog/2025-09-19) | One-day novelty voice; same engine underneath |
| Sep 9 | [Image upload](https://jules.google/docs/changelog/2025-09-09) | PNG/JPEG under 5 MB total, at task creation |
| Sep 4 | [Stacked diff](https://jules.google/docs/changelog/2025-09-04) | Vertical multi-file diff by default, tabbed view still available |
| Sep 3 | [Improved critic](https://jules.google/docs/changelog/2025-09-03) | Critic's real-time reasoning visible; more contextual judgements |
| Sep 2 | [Sample prompts](https://jules.google/docs/changelog/2025-09-02) | One-click starter prompts on the home page |
| Aug 22 | [Images in diffs](https://jules.google/docs/changelog/2025-08-22) | Generated charts, diagrams, and screenshots render inline |
| Aug 15 | [Export at any time](https://jules.google/docs/changelog/2025-08-15) | Publish a branch or PR mid-task via the GitHub icon |
| Aug 15 | [20 GB VM disk](https://jules.google/docs/changelog/2025-08-151) | Room for large dependencies and build artifacts |
| Aug 8 | [Web search](https://jules.google/docs/changelog/2025-08-081) | Finds current library docs and code examples; technical queries only |
| Aug 8 | [Interactive Plan](https://jules.google/docs/changelog/2025-08-082) | Jules brainstorms and asks clarifying questions before planning |
| Aug 8 | [Critic Agent](https://jules.google/docs/changelog/2025-08-083) | Adversarial internal review of every proposed change |
| Aug 7 | [Front-end verification](https://jules.google/docs/changelog/2025-08-07) | Renders the site and returns a screenshot; Playwright in the base image |
| Aug 6 | [Out of beta](https://jules.google/docs/changelog/2025-08-06) | 140k+ public commits; Pro/Ultra plans; Gemini 2.5 thinking for plans |
| Aug 5 | [Environment snapshots](https://jules.google/docs/changelog/2025-08-05) | Setup scripts snapshot the VM for faster future tasks |
| Aug 4 | [Open a PR from Jules](https://jules.google/docs/changelog/2025-08-04) | PR creation straight from the UI after a task |

## Q2 — launch quarter

| Date | Release | In one line |
| --- | --- | --- |
| Jul 18 | [Bun support](https://jules.google/docs/changelog/2025-07-18) | Bun works out of the box, no extra setup |
| Jul 3 | [Task controls & UI polish](https://jules.google/docs/changelog/2025-07-03) | Pause, resume, delete, copy task URLs from sidebar and repo view |
| Jun 26 | [GitHub issue labels](https://jules.google/docs/changelog/2025-06-26) | Label an issue `jules` to start a task |
| Jun 20 | [Agent upgrade](https://jules.google/docs/changelog/2025-06-20) | Reads `AGENTS.md`, faster, less punting, more reliable setup, better tests |
| Jun 18 | [Modernised base image](https://jules.google/docs/changelog/2025-06-18) | Newer Rust/Node/Python, multiple runtimes, version pinning |
| Jun 6 | [Customization & efficiency](https://jules.google/docs/changelog/2025-06-06) | Copy/download code, task modals, adjustable code panel |
| May 30 | [Reliability & capacity](https://jules.google/docs/changelog/2025-05-30) | 60 tasks/day, 5 concurrent at the time; GitHub sync fixed; ⅓ the failures |
| May 22 | [Stability](https://jules.google/docs/changelog/2025-05-22) | Better queuing, Publish Branch surfaced in the summary UI |
| May 19 | [Jules launches](https://jules.google/docs/changelog/2025-05-19) | Fix bugs, bump dependencies, migrate code, ship scoped features, open PRs |

## Notes on the arc

- **Aug 6, 2025** is the beta exit and the arrival of paid tiers. Anything quoted before that date (e.g. "60 tasks per day") is superseded by [[Plans and Limits]].
- The **critic** line of work — Critic Agent (Aug), Improved Critic (Sep), Planning Critic (Jan 2026) — is Google's main lever on quality. → [[Planning and Approval]]
- Autonomy compounds through the year: issue labels (Jun) → memory (Sep) → API/CLI (Oct) → scheduled + suggested tasks (Dec). → [[Continuous AI]]

**Source:** <https://jules.google/docs/changelog/>
