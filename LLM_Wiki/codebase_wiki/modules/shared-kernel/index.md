---
type: module-index
module: MerchSys.SharedKernel
last-updated: 2026-05-10
last-audited: 2026-05-10
plans-completed: [INFRA-02, INFRA-05, INFRA-06, INFRA-07, INT-05, POS-13]
file-count: 42
---

# MerchSys.SharedKernel — Module Index

This is the root index for the **MerchSys.SharedKernel** module. It contains all the common interfaces, base types, MediatR events, and queries shared across the entire modular monolith.

## Overview
- **Project File:** `src/MerchSys.SharedKernel/MerchSys.SharedKernel.vbproj`
- **Dependencies:** `MediatR`, `Microsoft.EntityFrameworkCore`
- **Primary Responsibility:** Providing the cross-module communication contracts and base entity types.

## Layer Manifests
- [[entities\|Entities & Base Types]]
- [[events-queries\|Events and Queries]]
- [[interfaces\|Shared Interfaces & Enums]]
