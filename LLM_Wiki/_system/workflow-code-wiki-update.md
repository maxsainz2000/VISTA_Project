# Workflow: Codebase Wiki Update

**Trigger:** The user asks Antigravity to "Update the Codebase Wiki for [PLAN-ID]" or similar.

This workflow is used to synchronize the `codebase_wiki/` with recent code changes made by a coding agent. This workflow ensures that the codebase wiki remains a highly accurate, pre-digested map of the codebase for agents.

## Prerequisites
- The coding agent has already written code and generated a progress summary (e.g., `Progress/VISTA_Modules/<Module>/<PLAN-ID>-summary.md`).
- You (Antigravity) are stepping in to process that summary.

## Steps

### Step 1: Read the Progress Summary
- Read the corresponding `-summary.md` file in the `Progress/` folder.
- Identify all files that were created, modified, or deleted during this task.

### Step 2: Read the Changed Source Files
- Read **only** the source files (`.vb`, `.xaml`) mentioned in the progress summary.
- Extract class names, properties, method signatures, base classes, interfaces, dependencies (DI), and file paths.

### Step 3: Update Layer Manifests
- Determine which layer the changed files belong to (e.g., `entities.md`, `services.md`, `ui.md`).
- Open the relevant layer manifests in `codebase_wiki/modules/<module>/`.
- Insert or update the tables with the extracted details from Step 2.
- Update the `last-updated` frontmatter field.

### Step 4: Update the Module Index
- Open the relevant `codebase_wiki/modules/<module>/index.md`.
- Update the `plans-completed` list to include this plan.
- Update the `file-count` frontmatter field by recounting the files if new ones were added.
- Ensure any links to layer manifests are correct.

### Step 5: Update Cross-Cutting Registries (if applicable)
- **Contracts:** If a MediatR Event or Query was added or a Handler was implemented, update `codebase_wiki/modules/shared-kernel/events-queries.md`.
- **Schemas:** If an EF Core Entity was added/changed, update `codebase_wiki/schemas/database.md`.
- **DI:** If a new service was created and registered, update `codebase_wiki/schemas/di-registry.md`.

### Step 6: Update the Master Index & Log
- Open `codebase_wiki/index.md`. Update any global file counts or plan status indicators.
- Open `codebase_wiki/log.md`. Add a new timestamped entry specifying which plan was synced and what changed.

## Module Routing

| Plan prefix | Wiki module folder(s) | Notes |
|---|---|---|
| PUR-XX | `modules/purchasing/` | Single module |
| INV-XX | `modules/inventory/` | Single module |
| POS-XX | `modules/pos/` | Single module |
| ACC-XX | `modules/accounting/` | Single module |
| INFRA-XX | `modules/shared-kernel/` or `modules/app/` | Depends on deliverables |
| INT-XX | Multiple — read plan `## Deliverables` | Cross-module: update every module folder that received new files |

> **INT plans are cross-cutting.** A single INT plan may add SharedKernel contracts, Inventory handlers, POS handlers, and App-layer registrations. You must update layer manifests in *every* affected module folder, not just one.

## Constraints
- **Never guess:** If a file's contents aren't clear from the summary, read the actual file. Do not invent properties or signatures.
- **Keep it brief:** Layer manifests should serve as a quick-reference index (e.g., tables), not a full copy of the code.
