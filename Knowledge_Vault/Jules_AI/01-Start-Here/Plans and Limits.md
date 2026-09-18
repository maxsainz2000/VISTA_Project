---
title: Plans and Limits
type: reference
section: Start Here
tags: [jules/limits]
source: https://jules.google/docs/usage-limits/
captured: 2026-09-12
aliases: [Usage limits, Pricing, Quotas]
---

# Plans and Limits

Paid Jules access rides on a **Google AI plan** subscription rather than being billed separately.

## Tier comparison

| | Jules (free) | Jules in Pro | Jules in Ultra |
| --- | --- | --- | --- |
| Best for | Evaluating Jules on real work | Daily coding, higher intensity | Power users, agent-heavy workflows |
| Daily tasks (rolling 24 h) | **15** | **100** | **300** |
| Concurrent tasks | **3** | **15** | **60** |
| Model access | Gemini 2.5 Pro | Higher access to the latest model (from Gemini 3 Pro) | Priority access to the latest model (from Gemini 3 Pro) |

> [!note] Model lines move
> The table is as published on the limits page. Later changelog entries put **Gemini 3 Flash** as the base model for all tiers (Jan 2026) and **Gemini 3.1 Pro** for Pro users (Mar 2026) — see [[Release History 2026]]. Re-check the live page before quoting numbers.

## Upgrading

- **Jules in Pro** ships with the Google AI Pro plan (<https://one.google.com/ai>)
- **Jules in Ultra** ships with the Google AI Ultra plan

## Rules worth knowing

- Paid plans currently require an **individual Google account** (`@gmail.com`). Workspace/enterprise upgrade paths were still in progress; business power users are pointed at an interest form.
- **Limits are per person, not pooled** — a shared family plan does not share Jules quota.
- **18+ only.** A family-plan member under 18 won't get Jules benefits even on an active plan.
- Hitting the daily cap disables the **new task** button with an explanatory tooltip. Existing tasks, history, and feedback stay fully accessible.
- Limits are on a **rolling 24-hour window**, not a calendar day.
- Google reserves the right to adjust limits and features as usage patterns emerge.

Related: [[Suggested Tasks]] is capped at **5 repositories**; [[Jules Tools CLI]] `--parallel` is capped at **5** simultaneous sessions per prompt.

**Source:** <https://jules.google/docs/usage-limits/>
