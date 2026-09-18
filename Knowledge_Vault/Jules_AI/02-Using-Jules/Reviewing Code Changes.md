---
title: Reviewing Code Changes
type: guide
section: Using Jules
tags: [jules/tasks, jules/github]
source: https://jules.google/docs/code/
captured: 2026-09-12
aliases: [Diff viewer, Activity feed]
---

# Reviewing Code Changes

## Activity feed

A real-time log of each step completed, what Jules did, outputs and errors encountered, and any requests for your input. It's the window into its decision-making.

## Code diffs

- A **mini diff** appears inline in the feed as files change
- The right pane holds a full-screen **diff editor** across all files
- Only modified or added files appear there
- Drag the sidebar to resize, or expand to full screen
- **Download** and **copy** icons sit top-right of the diff panel — copying yields only the updated code, not the full diff

Related shipped improvements: stacked (vertical) diff layout with a toggle back to tabs (Sept 2025), rendered images inside diffs (Aug 2025), and side-by-side diffs in the CLI TUI (Nov 2025).

## Interactive feedback

Through the chat box, in real time: revise logic or naming, request extra tests or cleanup, or give corrections like "return an empty string instead of None".

## Task summary

On completion: files changed, total runtime, lines added/changed, branch name, and commit message.

## Pushing to GitHub

Click **Publish branch** or **Publish PR**.

- Jules appears as commit author on the branch; if you create the PR manually you're the PR author
- If Jules publishes the PR, Jules is the PR creator
- Since Aug 2025 you can export **at any time** mid-task via the GitHub icon, not just at the end
- Commit attribution is configurable since Feb 2026: Jules-only, co-authored, or you-only (Settings → Commit Authoring)

Once published you can keep editing the branch, review it as a PR, or delete it.

**Source:** <https://jules.google/docs/code/>
