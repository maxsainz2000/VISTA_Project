---
title: Scheduled Tasks
type: guide
section: Using Jules
tags: [jules/automation, jules/tasks]
source: https://jules.google/docs/scheduled-tasks/
captured: 2026-09-12
aliases: [Recurring tasks, Cron tasks]
---

# Scheduled Tasks

Recurring prompts, so routine maintenance and monitoring happen without re-prompting.

## Creating one

1. Go to the main **Task Input** field
2. Click the **Planning** dropdown (bottom right of the input area)
3. Select **Scheduled Task**
4. Configure **Frequency** and **Cadence** (daily, weekly, monthly) in the settings that appear above the input
5. Write the prompt and **Submit**

The task is active immediately and runs on the schedule.

## Managing them

The **Scheduled** tab sits directly under the text field on the dashboard. From there you can monitor status (last run, next run) and **delete**.

> [!note] Editing
> The docs page states editing isn't supported — delete and recreate. A Jan 2026 changelog entry then added **Edit, Pause, and Resume** from the menu. Trust the newer behaviour and verify in the UI. See [[Release History 2026]].

## What to schedule

Maintenance that needs consistency but little oversight. Google ships three templates modelled on how the **Stitch** team uses Jules:

| Template | Behaviour |
| --- | --- |
| **Performance** | A performance-obsessed agent making the codebase faster, one optimization at a time |
| **Design** | A UX-focused agent adding small touches of delight and accessibility |
| **Security** | A security-focused agent protecting the codebase from vulnerabilities |

Other proven uses: weekly dependency checks, nightly lint fixes, monthly cleanups, nightly releases, and scheduled builds where Jules repairs the break.

Pairs with [[Suggested Tasks]] and the [[Render Integration]] to form what Google calls **Continuous AI** — see [[Continuous AI]].

Prompt style for scheduled work: [[Prompting Jules]] · Equivalent via CI: [[GitHub Actions]].

**Source:** <https://jules.google/docs/scheduled-tasks/>
