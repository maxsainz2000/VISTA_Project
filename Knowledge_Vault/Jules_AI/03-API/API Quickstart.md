---
title: API Quickstart
type: api
section: API
tags: [jules/api, alpha]
source: https://jules.google/docs/api/reference/
captured: 2026-09-12
---

# API Quickstart

End-to-end first session with curl. Assumes `JULES_API_KEY` is exported — see [[Authentication]].

## 1. List your sources

Find the name of the repo to work with.

```bash
curl -H "x-goog-api-key: $JULES_API_KEY" \
  https://jules.googleapis.com/v1alpha/sources
```

```json
{
  "sources": [
    {
      "name": "sources/github/bobalover/boba",
      "id": "github/bobalover/boba",
      "githubRepo": { "owner": "bobalover", "repo": "boba" }
    }
  ],
  "nextPageToken": "github/bobalover/boba-web"
}
```

## 2. Create a session

```bash
curl 'https://jules.googleapis.com/v1alpha/sessions' \
  -X POST \
  -H "Content-Type: application/json" \
  -H "x-goog-api-key: $JULES_API_KEY" \
  -d '{
    "prompt": "Create a boba app!",
    "sourceContext": {
      "source": "sources/github/bobalover/boba",
      "githubRepoContext": { "startingBranch": "main" }
    },
    "automationMode": "AUTO_CREATE_PR",
    "title": "Boba App"
  }'
```

`automationMode` is optional — by default **no PR is created automatically**.

The immediate response echoes the session with its `name` and `id`. Poll with `GetSession` or `ListSessions`; once a PR exists it appears under `outputs`:

```json
"outputs": [
  {
    "pullRequest": {
      "url": "https://github.com/bobalover/boba/pull/35",
      "title": "Create a boba app",
      "description": "This change adds the initial implementation of a boba app."
    }
  }
]
```

> [!note] Plans auto-approve by default via the API
> Sessions created through the API have their plans **automatically approved**. Set `requirePlanApproval: true` to require an explicit approval step.

## 3. List sessions

```bash
curl 'https://jules.googleapis.com/v1alpha/sessions?pageSize=5' \
  -H "x-goog-api-key: $JULES_API_KEY"
```

## 4. Approve a plan

```bash
curl 'https://jules.googleapis.com/v1alpha/sessions/SESSION_ID:approvePlan' \
  -X POST \
  -H "Content-Type: application/json" \
  -H "x-goog-api-key: $JULES_API_KEY"
```

## 5. Interact with the agent

List activities:

```bash
curl 'https://jules.googleapis.com/v1alpha/sessions/SESSION_ID/activities?pageSize=30' \
  -H "x-goog-api-key: $JULES_API_KEY"
```

Send a message:

```bash
curl 'https://jules.googleapis.com/v1alpha/sessions/SESSION_ID:sendMessage' \
  -X POST \
  -H "Content-Type: application/json" \
  -H "x-goog-api-key: $JULES_API_KEY" \
  -d '{ "prompt": "Can you make the app corgi themed?" }'
```

The response is empty — the agent replies in the **next activity**, so list activities again to read it.

## Polling pattern

Activities are immutable and follow an event-sourcing pattern, so they can be cached aggressively. Use the `createTime` filter as a range cursor to fetch only what's new:

```bash
ENDPOINT="https://jules.googleapis.com/v1alpha/sessions/1234567/activities"
TIMESTAMP="2026-01-17T00:03:53.137240Z"

curl -H "x-goog-api-key: $JULES_API_KEY" "$ENDPOINT?createTime=$TIMESTAMP"
```

Next: [[Sessions Endpoint]] · [[Activities Endpoint]] · [[Types Reference]]

**Sources:** <https://jules.google/docs/api/reference/> · <https://developers.google.com/jules/api>
