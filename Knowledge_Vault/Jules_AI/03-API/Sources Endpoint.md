---
title: Sources Endpoint
type: api
section: API
tags: [jules/api, jules/github, alpha]
source: https://jules.google/docs/api/reference/sources
captured: 2026-09-12
aliases: [Sources API, ListSources]
---

# Sources Endpoint

Sources are repositories connected to Jules — currently GitHub only. They're created when you connect a repo through the web interface; **the API reads sources, it can't create them**.

## List

**`GET /v1alpha/sources`**

- `pageSize` — 1–100, default **30**
- `pageToken`
- `filter` — AIP-160 filter expression, e.g. `name=sources/source1 OR name=sources/source2`

```bash
curl -H "x-goog-api-key: $JULES_API_KEY" \
  "https://jules.googleapis.com/v1alpha/sources?pageSize=10"
```

```json
{
  "sources": [
    {
      "name": "sources/github-myorg-myrepo",
      "id": "github-myorg-myrepo",
      "githubRepo": {
        "owner": "myorg",
        "repo": "myrepo",
        "isPrivate": false,
        "defaultBranch": { "displayName": "main" },
        "branches": [
          { "displayName": "main" },
          { "displayName": "develop" },
          { "displayName": "feature/auth" }
        ]
      }
    }
  ],
  "nextPageToken": "eyJvZmZzZXQiOjEwfQ=="
}
```

### Filtering

```bash
# One source
curl -H "x-goog-api-key: $JULES_API_KEY" \
  "https://jules.googleapis.com/v1alpha/sources?filter=name%3Dsources%2Fgithub-myorg-myrepo"

# Several
curl -H "x-goog-api-key: $JULES_API_KEY" \
  "https://jules.googleapis.com/v1alpha/sources?filter=name%3Dsources%2Fsource1%20OR%20name%3Dsources%2Fsource2"
```

## Get

**`GET /v1alpha/sources/{sourceId}`** — path `name`, format `sources/{source}`, pattern `^sources/.*$`. Returns the full `Source` including its branch list.

## Using a source in a session

```bash
curl -X POST \
  -H "x-goog-api-key: $JULES_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Add unit tests for the auth module",
    "sourceContext": {
      "source": "sources/github-myorg-myrepo",
      "githubRepoContext": { "startingBranch": "develop" }
    }
  }' \
  https://jules.googleapis.com/v1alpha/sessions
```

Workflow: **List Sources** to discover names → **Get Source** to see branches → create the session.

> [!note] Two id shapes appear in the docs
> The reference pages use `sources/github-myorg-myrepo`; the quickstart and Google-for-Developers page use `sources/github/bobalover/boba`. Always take the `name` verbatim from a List Sources response rather than constructing it.

**Source:** <https://jules.google/docs/api/reference/sources>
