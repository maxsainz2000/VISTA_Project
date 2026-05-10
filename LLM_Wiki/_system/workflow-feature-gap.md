# LLM Wiki — Workflow: Feature Gap Analysis

This workflow dictates how to verify alignment between the foundational requirements in `Sources/`, the domain knowledge in `wiki/`, and the implementation architecture in `codebase_wiki/`.

## Trigger Phrase
"What features are present in the @[LLM_Wiki/wiki] but not implemented in the @[LLM_Wiki/codebase_wiki]"

## Audit Execution Rules
When triggered, follow these strict phases in order:

### Phase 1: Source-to-Wiki Verification
1. **Context Gathering (Sources):** Scan the `Sources/` directory, specifically looking for the Academic paper or project proposal documents. Extract key features, capabilities, and business rules documented there.
2. **Context Gathering (Domain Wiki):** Review `wiki/entities/` and `wiki/concepts/` to see what features are currently tracked.
3. **Synchronization:** If there are features described in `Sources/` that are missing from `wiki/`, **update the `wiki/` first** before proceeding to the next phase. Create or update the necessary concept/entity pages to ensure the domain wiki fully represents the original requirements.

### Phase 2: Wiki-to-Codebase Verification
1. **Context Gathering (Codebase Wiki):** Read `codebase_wiki/index.md` and module-specific manifests in `codebase_wiki/modules/` (such as services, views, and data access layer maps).
2. **Cross-Examination:** Systematically compare the complete feature list from `wiki/` against the implemented structures in `codebase_wiki/`. Look for requirements, business logic, or UI workflows that have no corresponding classes, methods, or views in the codebase.

### Phase 3: Report Generation
1. **Compile Findings:** For every missing feature identified in Phase 2, document it as an actionable gap. Categorize them by module (e.g., Inventory, POS, Purchasing).
2. **Output Location:** Generate a static markdown report in the `Plans/Pending_Tasks/` directory.
   - **Naming Convention:** `Feature-Gap-Audit-YYYY-MM-DD.md`
3. **Format Requirements:** The report must be a static markdown document (no automatic stub creation for the missing codebase files). Include clear descriptions of the missing features and references back to the source/wiki page that defines them.

*Always prioritize accuracy. Never assume a feature is implemented if there is no explicit trace of it in the `codebase_wiki` layer manifests.*
