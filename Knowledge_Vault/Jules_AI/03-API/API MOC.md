---
title: API MOC
type: index
section: API
tags: [jules, moc, jules/api, alpha]
captured: 2026-09-12
---

# REST API

> [!warning] Alpha
> The Jules REST API is an **alpha** release. Google states specifications, API keys, and definitions may change; the plan is to eventually maintain one stable and one experimental version.

- [[API Overview]] — base URL, pagination, resource names, error codes
- [[API Quickstart]] — first call, end to end with curl
- [[Authentication]] — API keys and troubleshooting
- [[Sessions Endpoint]] — create, list, get, delete, message, approve
- [[Activities Endpoint]] — monitor progress, read artifacts
- [[Sources Endpoint]] — list connected repositories
- [[Types Reference]] — every data type and enum

Consumers of the API: [[GitHub Actions]] · [[Jules Tools CLI]]

**Base URL:** `https://jules.googleapis.com/v1alpha`
**Auth header:** `x-goog-api-key: $JULES_API_KEY`
