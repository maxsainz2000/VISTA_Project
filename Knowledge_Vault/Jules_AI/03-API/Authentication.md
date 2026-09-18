---
title: Authentication
type: api
section: API
tags: [jules/api, jules/security, alpha]
source: https://jules.google/docs/api/reference/authentication
captured: 2026-09-12
aliases: [API key, JULES_API_KEY]
---

# Authentication

## Getting a key

1. Go to <https://jules.google.com/settings> (API section: `#api`)
2. Find the **API Key** section
3. Click **Generate API Key**, or copy an existing one
4. Store it securely — it won't be shown again

You can hold at most **3 API keys** at a time.

## Using it

Every request carries the key in the `x-goog-api-key` header:

```bash
curl -H "x-goog-api-key: YOUR_API_KEY" \
  https://jules.googleapis.com/v1alpha/sessions
```

Recommended — keep it in an environment variable:

```bash
export JULES_API_KEY="your-api-key-here"

curl -H "x-goog-api-key: $JULES_API_KEY" \
  https://jules.googleapis.com/v1alpha/sessions
```

For CI, store it as a secret (e.g. `JULES_API_KEY` in GitHub Actions) — see [[GitHub Actions]].

> [!warning] Exposed keys are auto-disabled
> Don't share keys or embed them in public code. Google automatically disables API keys found publicly exposed.

## Example: create a session

```bash
curl -X POST \
  -H "x-goog-api-key: $JULES_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Add unit tests for the utils module",
    "sourceContext": {
      "source": "sources/github-owner-repo",
      "githubRepoContext": { "startingBranch": "main" }
    }
  }' \
  https://jules.googleapis.com/v1alpha/sessions
```

## Troubleshooting

**"API key not valid"**
- Verify the whole key was copied, with no extra spaces
- Check it hasn't been revoked in settings
- Generate a new one

**"Permission denied"**
- Verify your account has access to Jules
- Verify you have access to the requested sessions or sources

**"Quota exceeded"**
- You may have hit rate limits — see [[Plans and Limits]]

**Source:** <https://jules.google/docs/api/reference/authentication>
