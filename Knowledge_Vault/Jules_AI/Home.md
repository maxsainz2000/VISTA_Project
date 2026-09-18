---
title: Home
type: index
section: Meta
tags: [jules, moc]
captured: 2026-09-12
---

# Jules — Documentation Vault

A local, linked knowledge base for **Jules**, Google Labs' asynchronous AI coding agent.
Everything here is distilled from the official docs; each note links back to its source page.

> [!tip] New here? Start with [[What is Jules]] → [[Getting Started]] → [[Setup Checklist]].

## Maps of content

| Section | Start at | Covers |
| --- | --- | --- |
| Start here | [[Start Here MOC]] | What Jules is, first task, plans, FAQ |
| Using Jules | [[Using Jules MOC]] | Environment, prompts, plans, review, automation |
| REST API | [[API MOC]] | Endpoints, auth, types, recipes |
| CLI | [[CLI MOC]] | `jules` command, TUI, scripting |
| Integrations | [[Integrations MOC]] | Render, GitHub Actions, MCP servers |
| Changelog | [[Changelog MOC]] | Every shipped feature, newest first |
| Reference | [[Reference MOC]] | Preinstalled tools, glossary, links |

## The 60-second version

- Jules works **asynchronously in a cloud VM** — it clones your GitHub repo, installs dependencies, edits code, runs tests, and opens a PR. See [[What is Jules]].
- You steer it with a **prompt**, it answers with a **plan**, you approve, it codes. See [[Running Tasks]] and [[Planning and Approval]].
- Three surfaces: the **web app**, the **[[Jules Tools CLI]]**, and the **[[API Overview|REST API]]**.
- Three ways it starts work by itself: [[Scheduled Tasks]], [[Suggested Tasks]], and [[GitHub Actions]] / issue labels.

## Common questions

- How do I make Jules understand my project? → [[AGENTS.md File]] and [[Environment Setup]]
- Why did my task fail? → [[Errors and Failures]]
- How many tasks do I get? → [[Plans and Limits]]
- What changed recently? → [[Release History 2026]]

## Vault conventions

See [[Vault Guide]] for folder layout, tags, and how to refresh this vault against the live docs.
Every note's `source:` property is the exact page it came from — see [[Source Index]].
