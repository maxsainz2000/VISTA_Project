---
name: "jules-docs-vault"
description: "Answer questions about Jules (Google Labs' async AI coding agent) — setup, prompts, the REST API, the CLI, integrations, limits, or release history — by reading the local Obsidian vault at Documents/Jules_AI. Also use when adding to, refreshing, or auditing that vault."
---

# Jules Docs Vault

A local Obsidian vault holding the official Jules documentation, distilled and cross-linked. **Read it before searching the web** — it is faster, and it records where each fact came from.

**Vault root:** `c:\Users\maxsa\Documents\24_7_Agent\Knowledge_Vault\Jules_AI`
(In a Linux shell this is the mounted `Knowledge_Vault/Jules_AI` folder; check the session's path mapping.)

Jules is Google Labs' asynchronous coding agent: it works in a cloud VM against a GitHub repo and returns a branch or PR. Do not confuse it with unrelated products named "Jules AI".

## How to use this skill

1. **Route the question** with the table below.
2. **Read that note** with the Read tool. Notes are self-contained; most answers need one or two.
3. **If unsure of the note name**, grep the vault instead of guessing:
   `Grep(pattern: "<term>", path: "<vault root>", output_mode: "content", -i: true, -C: 2)`
4. **Answer from the note**, and cite the note's `source:` URL when the user may want the original page.
5. **Check freshness** for anything volatile (limits, model names, plans, preinstalled versions): compare the note's `captured:` date against today. If it's stale and the answer matters, say so and offer to verify against the live docs.

## Where things live

| Question is about | Read |
| --- | --- |
| What Jules is, the execution model | `01-Start-Here/What is Jules.md` |
| Signing up, connecting GitHub, first task | `01-Start-Here/Getting Started.md` |
| A complete setup pass, including safety | `01-Start-Here/Setup Checklist.md` |
| Task quotas, pricing tiers, concurrency | `01-Start-Here/Plans and Limits.md` |
| Security, languages, privacy, "does it train on my repo" | `01-Start-Here/FAQ.md` |
| Setup scripts, the VM, snapshots | `02-Using-Jules/Environment Setup.md` |
| Teaching Jules a repo's conventions | `02-Using-Jules/AGENTS.md File.md` |
| Repo-level config values | `02-Using-Jules/Environment Variables.md` |
| Jules remembering corrections | `02-Using-Jules/Memory and Knowledge.md` |
| Prompts, repo/branch, image upload, pausing | `02-Using-Jules/Running Tasks.md` |
| What a good prompt looks like, prompt library | `02-Using-Jules/Prompting Jules.md` |
| Plan review, approval, critic agents | `02-Using-Jules/Planning and Approval.md` |
| Diffs, activity feed, publishing branches/PRs | `02-Using-Jules/Reviewing Code Changes.md` |
| Parallel tasks, deleting, repo access | `02-Using-Jules/Managing Tasks and Repos.md` |
| Per-repository workspace | `02-Using-Jules/Repo View.md` |
| Recurring prompts | `02-Using-Jules/Scheduled Tasks.md` |
| Jules proposing its own work (proactivity) | `02-Using-Jules/Suggested Tasks.md` |
| Tasks with no repo attached | `02-Using-Jules/Repoless Sessions.md` |
| The automation loop as a whole | `02-Using-Jules/Continuous AI.md` |
| Failures, retries, debugging | `02-Using-Jules/Errors and Failures.md` |
| Base URL, pagination, error codes | `03-API/API Overview.md` |
| First API call end to end | `03-API/API Quickstart.md` |
| API keys, headers, auth errors | `03-API/Authentication.md` |
| Create/list/get/delete session, sendMessage, approvePlan, states | `03-API/Sessions Endpoint.md` |
| Polling progress, artifacts, git patches | `03-API/Activities Endpoint.md` |
| Listing connected repos and branches | `03-API/Sources Endpoint.md` |
| Any field name, type, or enum value | `03-API/Types Reference.md` |
| Installing and using the `jules` command | `04-CLI/Jules Tools CLI.md` |
| Scripting and piping with the CLI | `04-CLI/CLI Examples.md` |
| How integrations and their permissions work | `05-Integrations/Integrations Overview.md` |
| Auto-fixing failed deploys | `05-Integrations/Render Integration.md` |
| Triggering Jules from CI | `05-Integrations/GitHub Actions.md` |
| The `jules` issue label | `05-Integrations/GitHub Issues Trigger.md` |
| Linear / Supabase / Neon / Context7 connections | `05-Integrations/MCP Servers.md` |
| "When did X ship", "what's new" | `06-Changelog/Release History 2026.md`, then `…2025.md` |
| What's installed on the VM, versions | `07-Reference/Preinstalled Tools.md` |
| A term you don't recognise | `07-Reference/Glossary.md` |
| An official URL | `07-Reference/Official Links.md` |
| Provenance of any note | `00-Meta/Source Index.md` |

`Home.md` is the entry point; each folder has a `… MOC.md` index.

## Conventions to respect when editing

- YAML properties on every note: `title`, `type` (guide/reference/api/changelog/index/meta), `section`, `tags`, `source`, `captured`, optional `aliases`.
- Tags are hierarchical under `#jules/…` — see `00-Meta/Tag Taxonomy.md`. Reuse existing tags rather than inventing near-duplicates.
- Cross-reference with Obsidian wikilinks, matching the exact note name or a declared alias.
- Facts, commands, endpoint paths, field names, enum values and code stay **verbatim**; explanatory prose is condensed and written in the vault's own words. Keep it that way — this is a reference vault, not a mirror of Google's site.
- End content notes with a **Source:** line carrying the URL.
- Never leave a broken wikilink. After editing, verify:
  ```
  grep -rho '\[\[[^]]*\]\]' <vault>/ --include='*.md' | sort -u
  ```
  and confirm each target exists as a note name or alias.

## Adding a new note

1. Put it in the folder matching its section; name it the way a person would search for it.
2. Copy the frontmatter shape from a sibling note.
3. Link it from the section's MOC **and** from at least one related note — no orphans.
4. Add a row to `00-Meta/Source Index.md`.

## Refreshing the vault

Jules ships several changelog entries a month, so this vault drifts.

1. Read the newest entry recorded in `06-Changelog/Release History 2026.md`.
2. Fetch <https://jules.google/docs/changelog/> — that single page contains every entry's full text, so one fetch covers the whole history.
3. For each newer entry: add a table row, and update whichever feature note it affects.
4. Re-verify the volatile pages, which change without changelog entries:
   - <https://jules.google/docs/usage-limits/> → `Plans and Limits.md`
   - <https://jules.google/docs/environment/> → `Preinstalled Tools.md`
   - <https://jules.google/docs/api/reference/types> → `Types Reference.md`
5. Bump `captured:` on every note touched and update `00-Meta/Source Index.md`.

## Known caveats recorded in the vault

- The REST API is **alpha** (`v1alpha`); Google may change specs, keys and definitions.
- Source resource names appear in two shapes across Google's own pages — always take `name` verbatim from a List Sources response.
- The scheduled-tasks page says tasks can't be edited; a later changelog entry adds edit/pause/resume. The changelog is newer.
- Task limits quoted in pre-August-2025 changelog entries are superseded by `Plans and Limits.md`.

When the vault genuinely doesn't cover something, say so, then check the live docs and offer to file the answer back into the vault.

