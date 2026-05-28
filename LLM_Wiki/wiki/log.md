# Domain Wiki — Activity Log

Chronological record of all wiki maintenance actions.

<!-- Append new entries at the top. Format: ## [YYYY-MM-DD] action | description -->

## [2026-05-28] amend | Architecture pivot — SQLite removed, pure client-server adopted

- New source: `Sources/system_plan_amendment_2026-05-28.md` (AMD-2026-05-28-01) — authoritative amendment supersedes `system_plan.md` §5.3, §5.4 (Local DB row), §10 "Data Sync Conflict" row, §11 "Data Sync Conflict" mitigation, §12 offline-first bullet.
- Pages created:
  - `wiki/sources/system-plan-amendment-2026-05-28.md` (source summary)
  - `wiki/concepts/centralized-database-architecture.md` (new authoritative concept)
- Pages updated:
  - `wiki/sources/system-plan.md` — added supersession banner, flagged superseded rows in tech stack / constraints / risk register, refreshed `related` and `last-updated`
  - `wiki/concepts/offline-first-sync.md` — marked **superseded**, added `status` and `superseded-by` frontmatter, retained historical content
  - `wiki/concepts/client-server-wpf.md` — removed SQLite row, repointed `related` to `centralized-database-architecture`, added history note
  - `wiki/analysis/tech-stack-reference.md` — removed SQLite row, added Pomelo MySQL provider, noted optimistic-concurrency + `SELECT ... FOR UPDATE` design
  - `wiki/index.md` — added amendment to Source Summaries, added Centralized Database Architecture under Architecture & Infrastructure, marked Offline-First Sync as superseded, refreshed counts
- Rule note: Project Owner explicitly granted permission to add the new amendment file to `Sources/`. Original `system_plan.md` and the four academic papers in `Sources/` were **not** modified — historical baseline preserved per Law #2 spirit (auditability of original human-provided documents).
- Follow-ups not done in this pass (flagged for next session):
  - `CLAUDE.md` still references SQLite, offline-first, `Sync_Journal`, `merchsys.db`, and the EF Core VB.NET migration workaround. Needs revision when SQLite-removal implementation plans are authored.
  - `agent_wiki/` entries that document SQLite-specific patterns (e.g., `efcore10-vbnet-migration-discovery-bug.md`) remain valid as historical engineering record but should be annotated as no-longer-applicable when SQLite is removed from the codebase.
  - `codebase_wiki/` will require a full re-sync by Antigravity after the SQLite-removal refactor lands.
- Agent: claude-code (under Project Owner authorization)

## [2026-05-02] ingest | Initial build — all 5 sources

- Sources ingested: `system_plan.md`, `Purchasing-Module_AcademicPaper.md`, `Inventory-Module_AcademicPaper.md`, `POS-Module_AcademicPaper.md`, `Accounting-Module_AcademicPaper.md`
- Pages created: 5 source summaries, 8 entity pages, 14 concept pages, 3 analysis pages
- Agent: Antigravity
