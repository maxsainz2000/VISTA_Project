---
title: Activities Endpoint
type: api
section: API
tags: [jules/api, alpha]
source: https://jules.google/docs/api/reference/activities
captured: 2026-09-12
aliases: [Activities API, ListActivities]
---

# Activities Endpoint

Activities are the events inside a session. Use them to monitor progress, read messages, and pull artifacts such as code changes.

They are **immutable** and follow an event-sourcing pattern — once written they never change, so cache them aggressively and reconstruct a session from them.

## List

**`GET /v1alpha/sessions/{sessionId}/activities`**

- Path: `parent`, format `sessions/{session}`, pattern `^sessions/[^/]+$`
- Query: `pageSize` (1–100, default **50**), `pageToken`
- Query: `createTime` — timestamp filter acting as a range cursor, so you fetch only new activities

```bash
curl -H "x-goog-api-key: $JULES_API_KEY" \
  "https://jules.googleapis.com/v1alpha/sessions/1234567/activities?pageSize=20"
```

```bash
ENDPOINT="https://jules.googleapis.com/v1alpha/sessions/1234567/activities"
TIMESTAMP="2026-01-17T00:03:53.137240Z"

curl -H "x-goog-api-key: $JULES_API_KEY" "$ENDPOINT?createTime=$TIMESTAMP"
```

Each activity carries `name`, `id`, `originator` (`user` / `agent` / `system`), `description`, `createTime`, optional `artifacts[]`, and exactly one event field.

## Get

**`GET /v1alpha/sessions/{sessionId}/activities/{activityId}`** — path `name`, pattern `^sessions/[^/]+/activities/[^/]+$`.

## Activity event types

Exactly one of these is populated per activity.

```json
{ "planGenerated": { "plan": { "id": "plan1", "steps": [ { "id": "step1", "index": 0, "title": "Step title", "description": "Details" } ], "createTime": "2024-01-15T10:31:00Z" } } }
```

```json
{ "planApproved": { "planId": "plan1" } }
```

```json
{ "userMessaged": { "userMessage": "Please also add integration tests" } }
```

```json
{ "agentMessaged": { "agentMessage": "I've completed the unit tests. Would you like me to add integration tests as well?" } }
```

```json
{ "progressUpdated": { "title": "Writing tests", "description": "Creating test cases for login functionality" } }
```

```json
{ "sessionCompleted": {} }
```

```json
{ "sessionFailed": { "reason": "Unable to install dependencies" } }
```

## Artifacts

Outputs produced during execution, attached to an activity.

**Code changes (ChangeSet)** — a git patch you can apply locally:

```json
{
  "artifacts": [
    {
      "changeSet": {
        "source": "sources/github-myorg-myrepo",
        "gitPatch": {
          "baseCommitId": "a1b2c3d4e5f6",
          "unidiffPatch": "diff --git a/src/auth.js b/src/auth.js\n...",
          "suggestedCommitMessage": "Add authentication tests"
        }
      }
    }
  ]
}
```

**Bash output:**

```json
{ "artifacts": [ { "bashOutput": { "command": "npm test", "output": "All tests passed (42 passing)", "exitCode": 0 } } ] }
```

**Media** (e.g. front-end verification screenshots):

```json
{ "artifacts": [ { "media": { "mimeType": "image/png", "data": "base64-encoded-data..." } } ] }
```

Full field definitions: [[Types Reference]].

**Source:** <https://jules.google/docs/api/reference/activities>
