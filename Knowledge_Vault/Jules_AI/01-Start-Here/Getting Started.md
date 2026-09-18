---
title: Getting Started
type: guide
section: Start Here
tags: [jules/setup, jules/github]
source: https://jules.google/docs/
captured: 2026-09-12
---

# Getting Started

## 1. Sign in

Go to <https://jules.google.com>, sign in with a Google account, and accept the one-time privacy notice.

> [!warning] Age and account type
> Jules requires you to be **18 or older**. Paid tiers are currently tied to individual Google accounts (`@gmail.com`) — see [[Plans and Limits]].

## 2. Connect GitHub

1. Click **Connect to GitHub account** and complete the login flow.
2. Grant access to *all* repos or hand-pick them.
3. You're redirected back to Jules — refresh if it stalls.

Afterwards you get a **repo selector** plus the prompt box. Adding more repos later is covered in [[Managing Tasks and Repos]].

## 3. Run your first task

1. Pick a repository in the repo selector.
2. Pick the branch (defaults to the repo's default branch).
3. Write a specific prompt, e.g. ``Add a test for the `parseQueryString` function in utils.js``.
4. Optionally add environment setup scripts ([[Environment Setup]]).
5. Click **Give me a plan**.

Jules returns a plan for review before touching any code ([[Planning and Approval]]).

> [!tip] Repoless start
> Since Nov 2025 you can clear the repo selection (click the **X** next to the repo) and run a **repoless session** — an ephemeral dev environment with no repository at all. Good for prototypes and scripts.

## 4. Add an AGENTS.md

Jules automatically looks for `AGENTS.md` in the repo root and uses it to produce better plans. See [[AGENTS.md File]].

## 5. Turn on notifications

Enable browser notifications when prompted, or toggle them later under **Settings → Notifications**. You'll be pinged when the task completes or needs input.

## Next

- [[Running Tasks]] — the full walkthrough
- [[Environment Setup]] — make Jules smarter about your project
- [[Planning and Approval]] — approving and iterating
- [[Prompting Jules]] — a prompt library

**Source:** <https://jules.google/docs/>
