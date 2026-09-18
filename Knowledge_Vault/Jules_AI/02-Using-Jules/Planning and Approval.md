---
title: Planning and Approval
type: guide
section: Using Jules
tags: [jules/tasks]
source: https://jules.google/docs/review-plan/
captured: 2026-09-12
aliases: [Review plan, Plan approval, Interactive plan]
---

# Planning and Approval

Before writing any code, Jules produces a **plan** — your chance to redirect it cheaply.

## Reviewing the plan

After setup (cloning, VM init, dependency install) you'll see a natural-language description of the intended direction, a step-by-step breakdown, and any assumptions or setup steps.

1. Expand each step and read it
2. Give feedback in the chat input if something's off
3. Click **approve plan** to start execution

> [!note] Auto-approval
> If you navigate away, Jules eventually **auto-approves** the plan on a timer. No babysitting required — but it also means a bad plan can proceed if you walk away.

## Giving feedback

At any point, use the chat box to ask Jules to revise a step, point out something missed, clarify the original request, or answer its questions. It responds and updates the plan.

## Related planning features

- **Interactive Plan** (Aug 2025) — choose it from the dropdown when starting a task; Jules brainstorms and asks clarifying questions instead of jumping to a solution.
- **Critic Agent** (Aug 2025) — an internal adversarial reviewer that challenges every proposed change before completion; its reasoning is visible in the UI.
- **Planning Critic** (Jan 2026) — a second agent that critiques *auto-approved* plans before execution. Google reports a **9.5% reduction in task failure rates** at the cost of slightly longer planning.
- **API equivalent** — set `requirePlanApproval: true` when creating a session, then call `:approvePlan`. See [[Sessions Endpoint]].

**Source:** <https://jules.google/docs/review-plan/>
