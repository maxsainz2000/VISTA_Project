---
title: MCP Servers
type: guide
section: Integrations
tags: [jules/integration, jules/security]
source: https://jules.google/docs/changelog/2026-02-02
captured: 2026-09-12
aliases: [MCP, Model Context Protocol]
---

# MCP Servers

Since February 2026 Jules can call **MCP (Model Context Protocol) servers** during a session, giving it tools beyond the repository.

## Supported at launch

**Linear · Stitch · Neon · Tinybird · Context7 · Supabase**

## Setup

Authentication is by API key:

1. Get the service's API key (Linear, Stitch, …)
2. Open the Jules **Settings** page
3. Click the **MCP** section
4. Paste the API key for that service
5. Start a new session

Jules triggers the MCP server's tools **when it detects the need for a tool call** — you don't invoke them explicitly.

## Why the list is short

Google deliberately hand-picked the initial servers, security first, because Jules runs in a cloud environment connected to your GitHub repositories. Vetting lets them:

- **Validate data flow** — each server meets data-handling standards that keep source code and API keys isolated
- **Audit tool permissions** — no over-privileged access to the environment
- **Ensure stability** — fewer connection drops, more predictable behaviour

They plan to expand the list, and the settings panel has a place to request servers.

**Source:** <https://jules.google/docs/changelog/2026-02-02>
