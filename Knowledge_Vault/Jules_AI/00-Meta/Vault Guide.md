---
title: Vault Guide
type: meta
section: Meta
tags: [jules, meta]
captured: 2026-09-12
---

# Vault Guide

How this vault is built, so you (or an AI assistant) can find things fast and keep it current.

## Folder layout

| Folder | Contents |
| --- | --- |
| `00-Meta` | This guide, [[Source Index]], [[Tag Taxonomy]] |
| `01-Start-Here` | Orientation: what Jules is, first task, plans, FAQ |
| `02-Using-Jules` | Day-to-day product usage in the web app |
| `03-API` | REST API: auth, endpoints, data types |
| `04-CLI` | `jules` command-line tool and scripting |
| `05-Integrations` | Render, GitHub Actions, MCP servers, GitHub issues |
| `06-Changelog` | Release history, newest first |
| `07-Reference` | Preinstalled toolchain, glossary, official links |

Each folder has a `… MOC` note (map of content) that indexes it. [[Home]] links to all of them.

## Note anatomy

Every note carries YAML properties:

```yaml
title:    Human-readable name
type:     guide | reference | api | changelog | index | meta
section:  Which MOC it belongs to
tags:     [jules/…]
source:   URL of the official page it was distilled from
captured: YYYY-MM-DD
```

Body rules used throughout:

- **Facts, commands, endpoints, field names, and code are reproduced exactly** — those must be literal to be useful.
- **Prose is rewritten and condensed.** This is a study/reference vault, not a mirror of Google's site.
- Cross-references use Obsidian wikilinks; the last line of most notes is a **Source** link.
- Callouts: `> [!warning]` for things that will bite you, `> [!tip]` for shortcuts.

## Searching this vault

- `Ctrl+O` — jump to a note by name (fastest route; note names match doc-page names).
- `Ctrl+Shift+F` — full-text search. Useful prefixes: `tag:#jules/api`, `path:06-Changelog`, `file:Types`.
- Click any tag in the tag pane to see every note on that theme — see [[Tag Taxonomy]].
- Open the **graph** to see how concepts connect; colour groups are set per folder.

## Keeping it current

Jules ships fast — the changelog averages several entries a month.

1. Check [[Release History 2026]] for the newest entry in this vault.
2. Compare against <https://jules.google/docs/changelog/>.
3. For each newer entry, add a row and update the affected feature note.
4. Re-check [[Plans and Limits]] and [[Preinstalled Tools]] — both drift silently.
5. Bump `captured:` on any note you touch and log it in [[Source Index]].

> [!warning] Alpha surfaces
> The REST API is an **alpha** release (`v1alpha`) — Google states specifications, keys, and definitions may change. Treat [[Types Reference]] as a snapshot, not a contract.
