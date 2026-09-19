---
type: system
title: "LLM Wiki — Core Rules"
last-updated: 2026-05-07
---

# Core Rules

These rules are absolute and unbreakable. Every agent interacting with this wiki must follow them.

## Three Immutable Laws

1. **Never fabricate sources.** Every factual claim in `wiki/` must trace back to a file in `Sources/`. If a claim cannot be sourced, it must be explicitly marked as inference or removed.
2. **Never modify files in `Sources/`.** The `Sources/` directory is strictly read-only. Raw source documents are human-provided and must never be altered, reformatted, or appended to by any agent.
3. **Always maintain link integrity.** No broken `[[wikilinks]]`. Every internal link must resolve to an existing file. When creating or renaming pages, update all inbound links.

## Agent Boundaries

| Wiki | Path | Who Writes | When |
|---|---|---|---|
| Domain Wiki | `wiki/` | Antigravity only | On ingest, query, lint |
| Agent Wiki | `agent_wiki/` | All agents | After debugging/error-fixing sessions only |
| Codebase Wiki | `codebase_wiki/` | Antigravity only | Before every git commit |
| Sources | `Sources/` | Humans only | When adding new raw documents |
| System | `_system/` | Antigravity only | When updating rules or workflows |

## Vault Schema

```
LLM_Wiki/
├── Sources/          # Raw documents — READ ONLY
├── _system/          # Rules, workflows, conventions
├── wiki/             # Domain knowledge — Antigravity writes
│   ├── index.md      # Master catalog
│   ├── log.md        # Activity log
│   ├── sources/      # Source summaries
│   ├── entities/     # People, orgs, modules
│   ├── concepts/     # Abstract topics
│   ├── analysis/     # Cross-source synthesis
│   └── _views/       # Contradiction pages
├── agent_wiki/       # Engineering learnings — all agents write
│   ├── index.md      # Master index
│   ├── log.md        # Activity log
│   ├── _templates/   # Entry templates
│   ├── errors/       # Error fixes
│   ├── patterns/     # Proven approaches
│   └── antipatterns/ # Traps to avoid
├── codebase_wiki/    # Codebase map — Antigravity writes ONLY
└── llm-wiki.md       # Original idea doc
```

## YAML Frontmatter Requirements

Every file in `wiki/`, `agent_wiki/`, and `codebase_wiki/` (except index.md and log.md) MUST have YAML frontmatter.

### Domain Wiki (`wiki/`)

```yaml
---
type: entity | concept | source-summary | analysis
title: "Human-readable title"
aliases: []
sources: []
related: []
last-updated: YYYY-MM-DD
---
```

### Agent Wiki (`agent_wiki/`)

```yaml
---
type: error-fix | pattern | antipattern
module: MerchSys.Purchasing | MerchSys.Inventory | MerchSys.POS | MerchSys.Accounting | Integration | Infrastructure
agent: antigravity | claude-code | codex | other
date: YYYY-MM-DD
tags: []
---
```

### Codebase Wiki (`codebase_wiki/`)

```yaml
---
type: module-index | layer-manifest | contracts-registry | schema-map | dependency-map
last-updated: YYYY-MM-DD
---
```

## Wikilink Conventions

- Use Obsidian-style `[[wikilinks]]` for all internal cross-references.
- Link to pages by filename without extension: `[[module-pos]]` not `[[module-pos.md]]`.
- For display text: `[[module-pos|POS Module]]`.
- Cross-references between domain wiki and agent wiki use full relative paths when needed.

## Workflow Triggers

| User Says | Workflow |
|---|---|
| "Ingest X" | Follow `_system/workflow-ingest.md` |
| "Lint" or "Audit" | Follow `_system/workflow-lint.md` |
| Source contradicts existing claim | Follow `_system/workflow-contradictions.md` |
| "Does codebase_wiki directly mirrors..." | Follow `_system/workflow-code-wiki-audit.md` |
| "What features are present in the @[LLM_Wiki/wiki] but not implemented in the @[LLM_Wiki/codebase_wiki]" | Follow `_system/workflow-feature-gap.md` |
