---
type: module-index
module: MerchSys.App
last-updated: 2026-05-15
last-audited: 2026-05-16
plans-completed: [INFRA-01, INFRA-05, INFRA-06, INFRA-08, INFRA-09, INFRA-10, INFRA-11, ACC-07, ACC-08, ACC-09, ACC-11, ACC-15, INT-01, INT-02, INT-04, INT-05, INT-06, INT-07, INT-10, INT-12, INT-13, POS-17]
file-count: 69
---

# MerchSys.App — Module Index

This is the root index for the **MerchSys.App** module (the WPF startup project).

## Overview
- **Project File:** `src/MerchSys.App/MerchSys.App.vbproj`
- **Dependencies:** All other MerchSys modules
- **Primary Responsibility:** Application startup, Dependency Injection composition root, and holding all UI Views (`.xaml`).

## Layer Manifests
- [[ui|UI (Views and Resources)]]
- [[services|Services (Infrastructure Implementations)]]

## Operations
- [Production Deployment Runbook](file:///c:/Users/Admin/Documents/VISTA_Project/Plans/VISTA_Modules/Infrastructure/runbooks/01-production-deployment.md) — Step-by-step guide for deploying to MariaDB 11.4+.
