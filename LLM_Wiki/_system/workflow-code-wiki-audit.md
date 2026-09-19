---
type: system
title: "Workflow — Codebase Wiki Audit"
last-updated: 2026-05-07
---

# Workflow: Codebase Wiki Audit

**Trigger:** The user asks: "Does codebase_wiki directly mirrors what's in the WPF_Applications [Module Name]?" (or similar).

This workflow is used to strictly verify that the `codebase_wiki` perfectly mirrors the actual, live source code in the `WPF_Applications` directory.

## Phase 1: Context Gathering

1. **Identify the Target Module:** Determine which module is being audited (e.g., `Purchasing`, `Inventory`, `POS`, `Accounting`, `SharedKernel`, `App`, `Integration`).
2. **Scan the Source Code:** 
   - Read the actual `.vb` and `.xaml` files in `WPF_Applications/MerchSys/src/MerchSys.[Module]/` (and related views in `MerchSys.App/Views/` if applicable).
3. **Load the Wiki Manifests:**
   - Read all layer manifests for that module located in `LLM_Wiki/codebase_wiki/modules/[module]/` (e.g., `entities.md`, `services.md`, `data-access.md`, `views.md`).
   - Read the module's `index.md`.

## Phase 2: The Cross-Examination (The Audit)

Systematically compare the live code against the wiki manifests:

1. **File Counts:** 
   - Does the `file-count` in the wiki's `index.md` match the exact number of source files in the live project directory?
2. **Class & Interface Matching:** 
   - Does every interface, entity class, and view model in the source code have a corresponding row in the respective wiki manifest? 
   - Are there "ghost" entries in the wiki for classes that were deleted or renamed?
3. **Signature Validation:** 
   - Do the method signatures, properties, injected dependencies, and base classes described in the wiki perfectly match the actual VB.NET code?
4. **Cross-Cutting Registries:** 
   - Are any new services missing from `codebase_wiki/schemas/di-registry.md`? 
   - Are any EF Core entities or `DbSet` properties missing from `codebase_wiki/schemas/database.md`?
   - Are any MediatR events/queries missing from `events-queries.md`?

## Phase 3: Resolution

1. **Discrepancies Found:**
   - If there are mismatches, document them in an `audit-report.md` artifact.
   - Explain exactly what is missing, extra, or outdated.
   - **Ask the user for permission** to execute an automated correction (updating the wiki to match the live code). DO NOT update the wiki until the user approves.
2. **No Discrepancies Found:**
   - If the wiki perfectly mirrors the code, declare the wiki 100% synchronized.
   - Update the `last-audited` timestamp in the module's `index.md`.
