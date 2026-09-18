---
title: Running Tasks
type: guide
section: Using Jules
tags: [jules/tasks, jules/prompting, jules/github]
source: https://jules.google/docs/running-tasks/
captured: 2026-09-12
---

# Running Tasks

## Choose repo and branch

Open the **repo selector**, pick the repository, then the branch to base changes on. Jules remembers your last-used repo. Clearing the repo starts a **repoless session** instead.

## Write a clear prompt

Specific and scoped beats clever. Plain language is fine — no perfect grammar or code required. If Jules needs more clarity it asks before writing code.

**Good**
- Add a loading spinner while `fetchUserProfile` runs
- Fix the 500 error while submitting the feedback form
- Document the `useCache` hook with JSDoc

**Avoid**
- Fix everything
- Optimize code
- Make this better

More patterns: [[Prompting Jules]].

## Attach visual context

Upload images at task creation — UI mockups, front-end glitches, screenshots, diagrams, inspiration.

- Drag-and-drop or browse, one or more images
- Total upload under **5 MB**
- **PNG and JPEG** only
- **Task creation only** — not yet available in follow-up prompts

Images inform Jules' understanding; they are **not** embedded in code or committed. Assets you want in the codebase must be committed to the repo separately.

## Watch it work

Once the plan is approved you get a real-time **activity feed**, inline explanations of each change, and a **mini diff** per file. The full **diff editor** shows everything at once — [[Reviewing Code Changes]].

## Final summary and branch

On completion Jules reports files changed, total runtime, lines added/changed/removed, and offers a branch plus commit message.

Click **Create branch** to push. Note:

- you are the branch owner
- Jules appears as the commit author (configurable since Feb 2026 — see [[Release History 2026]])
- you can open a PR from that branch on GitHub

## Feedback mid-task

Type into the chat box at any time: change approach, revise code, clarify logic. Jules responds and replans if needed. You can intervene whenever you like.

## Pausing

Click **pause**. A paused Jules does no work and waits for instructions — prompt it again, unpause, or delete the task.

## Starting from a GitHub issue

Label any issue `jules` (case-insensitive) with the Jules GitHub app authorised for that repo. See [[GitHub Issues Trigger]].

**Source:** <https://jules.google/docs/running-tasks/>
