---
title: Continuous AI
type: guide
section: Using Jules
tags: [jules/automation, jules/integration]
source: https://jules.google/docs/guides/continuous-ai-overview
captured: 2026-09-12
---

# Continuous AI

Google's name for the shift from Jules as *a tool you use* to *a system that works for you*. Three features combine into a loop.

## 1. Suggested Tasks

Toggle **Suggested Task** on and Jules analyses the whole codebase for floating `// TODO` comments (and, since Jan 2026, performance issues), surfacing them as actionable tasks on the dashboard — each with context, a rationale, and a confidence score for acting independently. Run several in parallel and watch the list shrink. → [[Suggested Tasks]]

> [!tip] From the Jules team
> No suggestions means your code is too organised. Ask Jules to analyse the repo and raise specific inline TODOs; once committed, they surface automatically.

## 2. Scheduled Tasks

Write a prompt, set it to run daily, weekly, or monthly. The CI/CD angle is the point: run builds on a schedule, and when one breaks Jules fixes it. Google's example — a nightly NPM release for a library, where Jules caught the break, worked out the fix, and published under the nightly tag. → [[Scheduled Tasks]]

## 3. Render integration

The feedback loop that closes it. Render creates preview deployments for PRs; when a build fails, the failure is reported back to Jules, which analyses the error, writes a fix, and commits to the PR — triggering a fresh redeploy without manual intervention. → [[Render Integration]]

## The combination

Together these turn an assistant into a continuous system for your apps, websites, and libraries: work is discovered, scheduled, executed, and repaired without a human in each loop. The Feb 2026 **CI Fixer** extends the same idea to GitHub Actions failures — see [[Release History 2026]].

**Source:** <https://jules.google/docs/guides/continuous-ai-overview>
