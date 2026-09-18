---
title: API Overview
type: api
section: API
tags: [jules/api, alpha]
source: https://jules.google/docs/api/reference/overview
captured: 2026-09-12
aliases: [REST API, Jules API]
---

# API Overview

Programmatically create and manage coding sessions, monitor progress, and retrieve results.

## Base URL

```
https://jules.googleapis.com/v1alpha
```

## Authentication

API keys, passed in a header. Get one from <https://jules.google.com/settings> (max 3).

```bash
curl -H "x-goog-api-key: $JULES_API_KEY" \
  https://jules.googleapis.com/v1alpha/sessions
```

Details: [[Authentication]].

## The three resources

| Resource | Meaning |
| --- | --- |
| **Source** | An input for the agent — a connected GitHub repository. Install the Jules GitHub app via the web app first. → [[Sources Endpoint]] |
| **Session** | A continuous unit of work in one context, like a chat session; initiated with a prompt (and usually a source). → [[Sessions Endpoint]] |
| **Activity** | A single unit of work inside a session — plan generated, message sent, progress updated. → [[Activities Endpoint]] |

## Pagination

List endpoints take `pageSize` and `pageToken`:

```bash
# First page
curl -H "x-goog-api-key: $JULES_API_KEY" \
  "https://jules.googleapis.com/v1alpha/sessions?pageSize=10"

# Next page, using the token from the previous response
curl -H "x-goog-api-key: $JULES_API_KEY" \
  "https://jules.googleapis.com/v1alpha/sessions?pageSize=10&pageToken=NEXT_PAGE_TOKEN"
```

## Resource names

Hierarchical, following Google API conventions:

- Sessions — `sessions/{sessionId}`
- Activities — `sessions/{sessionId}/activities/{activityId}`
- Sources — `sources/{sourceId}`

## Error handling

| Status | Meaning |
| --- | --- |
| `200` | Success |
| `400` | Bad request — invalid parameters |
| `401` | Unauthorized — invalid or missing token |
| `403` | Forbidden — insufficient permissions |
| `404` | Not found — resource doesn't exist |
| `429` | Rate limited — too many requests |
| `500` | Server error |

Error bodies carry details:

```json
{
  "error": {
    "code": 400,
    "message": "Invalid session ID format",
    "status": "INVALID_ARGUMENT"
  }
}
```

**Source:** <https://jules.google/docs/api/reference/overview>
