---
title: Sessions Endpoint
type: api
section: API
tags: [jules/api, alpha]
source: https://jules.google/docs/api/reference/sessions
captured: 2026-09-12
aliases: [Sessions API, CreateSession]
---

# Sessions Endpoint

A session is a unit of work where Jules executes a coding task. It is the core resource.

## Create

**`POST /v1alpha/sessions`**

| Field | Type | Notes |
| --- | --- | --- |
| `prompt` | string | **Required.** The task description. |
| `title` | string | Optional; generated if omitted. |
| `sourceContext` | [SourceContext](Types%20Reference.md) | Repository and branch context. Optional for repoless sessions. |
| `requirePlanApproval` | boolean | `true` = plan needs explicit approval. Unset = auto-approved. |
| `automationMode` | string | `AUTO_CREATE_PR` to open a PR automatically when changes are ready. |

```bash
curl -X POST \
  -H "x-goog-api-key: $JULES_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Add comprehensive unit tests for the authentication module",
    "title": "Add auth tests",
    "sourceContext": {
      "source": "sources/github-myorg-myrepo",
      "githubRepoContext": { "startingBranch": "main" }
    },
    "requirePlanApproval": true
  }' \
  https://jules.googleapis.com/v1alpha/sessions
```

Returns the created `Session`:

```json
{
  "name": "sessions/1234567",
  "id": "abc123",
  "prompt": "Add comprehensive unit tests for the authentication module",
  "title": "Add auth tests",
  "state": "QUEUED",
  "url": "https://jules.google.com/session/abc123",
  "createTime": "2024-01-15T10:30:00Z",
  "updateTime": "2024-01-15T10:30:00Z"
}
```

## List

**`GET /v1alpha/sessions`** — query params `pageSize` (1–100, default **30**) and `pageToken`.

```bash
curl -H "x-goog-api-key: $JULES_API_KEY" \
  "https://jules.googleapis.com/v1alpha/sessions?pageSize=10"
```

Response contains `sessions[]` and `nextPageToken`.

## Get

**`GET /v1alpha/sessions/{sessionId}`** — path param `name`, format `sessions/{session}`, pattern `^sessions/[^/]+$`.

Returns the full `Session`, including `outputs` (e.g. the pull request) once complete.

## Delete

**`DELETE /v1alpha/sessions/{sessionId}`** — empty response on success.

```bash
curl -X DELETE \
  -H "x-goog-api-key: $JULES_API_KEY" \
  https://jules.googleapis.com/v1alpha/sessions/1234567
```

## Send a message

**`POST /v1alpha/sessions/{sessionId}:sendMessage`** — body `{ "prompt": "…" }` (required).

Use it for feedback, answers to questions, or extra instructions during an active session.

```bash
curl -X POST \
  -H "x-goog-api-key: $JULES_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{ "prompt": "Please also add integration tests for the login flow" }' \
  https://jules.googleapis.com/v1alpha/sessions/1234567:sendMessage
```

Returns an empty `SendMessageResponse` — the agent's reply arrives as the next activity.

## Approve a plan

**`POST /v1alpha/sessions/{sessionId}:approvePlan`** — only needed when `requirePlanApproval` was `true`.

```bash
curl -X POST \
  -H "x-goog-api-key: $JULES_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{}' \
  https://jules.googleapis.com/v1alpha/sessions/1234567:approvePlan
```

Returns an empty `ApprovePlanResponse`.

## Session states

| State | Meaning |
| --- | --- |
| `QUEUED` | Waiting to be processed |
| `PLANNING` | Analysing the task and creating a plan |
| `AWAITING_PLAN_APPROVAL` | Plan ready, waiting for approval |
| `AWAITING_USER_FEEDBACK` | Needs additional input |
| `IN_PROGRESS` | Actively working |
| `PAUSED` | Paused |
| `COMPLETED` | Completed successfully |
| `FAILED` | Failed to complete |

(`STATE_UNSPECIFIED` also exists in the enum — [[Types Reference]].)

Related: [[Activities Endpoint]] · [[Sources Endpoint]] · [[Repoless Sessions]]

**Source:** <https://jules.google/docs/api/reference/sessions>
