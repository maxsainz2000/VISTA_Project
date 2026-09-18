---
title: Managing Tasks and Repos
type: guide
section: Using Jules
tags: [jules/tasks, jules/github]
source: https://jules.google/docs/tasks-repos/
captured: 2026-09-12
---

# Managing Tasks and Repos

## Starting tasks

Three routes to a new task:

1. Click the Jules icon to return home to a blank prompt
2. The **+** button in the top navigation of the task view
3. **New task** from within a [[Repo View|repo view]]

You can run **multiple tasks simultaneously** (subject to your concurrency cap — [[Plans and Limits]]). Starting a task while viewing another loads it in the background; navigate to it to check progress or approve its plan.

Each task has its **own VM**, logs, environment setup, and code changes.

## Pausing and deleting

- **Pause:** click **pause**
- **Delete:** hover the task in the repo view, click the **trash icon**

> Paused tasks can be resumed later; deleted tasks are removed permanently.

## Managing repositories

Jules only reaches repositories you explicitly allow through GitHub. (Other version-control systems are a future item.)

**Selecting a repo**
- Use the dropdown when starting a task
- Enroll a new one from the left sidebar
- Search via the repo selector

**Granting access to more repos**
1. Go to <https://github.com>
2. Profile photo → **Settings**
3. Sidebar → **Applications**
4. Find **Google Labs Jules** → **Configure**
5. Under **Repository access**, select additional repos
6. **Save**

Or, inside Jules: open the repo selector, scroll to the bottom, click **+ Add repository** — it sends you to GitHub for the same flow.

**Source:** <https://jules.google/docs/tasks-repos/>
