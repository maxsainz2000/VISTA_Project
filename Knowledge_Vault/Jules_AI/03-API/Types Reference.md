---
title: Types Reference
type: api
section: API
tags: [jules/api, alpha]
source: https://jules.google/docs/api/reference/types
captured: 2026-09-12
aliases: [Session type, Activity type, Artifact, GitPatch, SourceContext]
---

# Types Reference

Every data type in the Jules REST API. `v1alpha` — treat as a snapshot, not a contract.

## Session

A contiguous amount of work within the same context.

| Field | Type | Notes |
| --- | --- | --- |
| `name` | string | Output only. `sessions/{session}` |
| `id` | string | Output only |
| `prompt` | string | **Required.** Task description |
| `title` | string | Generated if omitted |
| `state` | SessionState | Output only |
| `url` | string | Output only. Link to the session in the web app |
| `sourceContext` | SourceContext | Required per the type page; optional for repoless sessions |
| `requirePlanApproval` | boolean | Input only |
| `automationMode` | AutomationMode | Input only |
| `outputs` | SessionOutput[] | Output only |
| `createTime` | string (google-datetime) | Output only |
| `updateTime` | string (google-datetime) | Output only |

### SessionState

`STATE_UNSPECIFIED` · `QUEUED` · `PLANNING` · `AWAITING_PLAN_APPROVAL` · `AWAITING_USER_FEEDBACK` · `IN_PROGRESS` · `PAUSED` · `FAILED` · `COMPLETED`

### AutomationMode

| Value | Meaning |
| --- | --- |
| `AUTOMATION_MODE_UNSPECIFIED` | No automation (default) |
| `AUTO_CREATE_PR` | Automatically create a pull request when code changes are ready |

## Activity

A single event within a session.

| Field | Type | Notes |
| --- | --- | --- |
| `name` | string | `sessions/{session}/activities/{activity}` |
| `id` | string | Output only |
| `originator` | string | `user`, `agent`, or `system` |
| `description` | string | Output only |
| `createTime` | string (google-datetime) | Output only |
| `artifacts` | Artifact[] | Output only |
| `planGenerated` | PlanGenerated | A plan was generated |
| `planApproved` | PlanApproved | A plan was approved |
| `userMessaged` | UserMessaged | The user posted a message |
| `agentMessaged` | AgentMessaged | Jules posted a message |
| `progressUpdated` | ProgressUpdated | A progress update occurred |
| `sessionCompleted` | SessionCompleted | The session completed |
| `sessionFailed` | SessionFailed | The session failed |

## Source

| Field | Type | Notes |
| --- | --- | --- |
| `name` | string | `sources/{source}` |
| `id` | string | Output only |
| `githubRepo` | GitHubRepo | Repository details |

## Plans

**Plan** — `id` (output only), `steps` (PlanStep[]), `createTime`.
**PlanStep** — `id`, `index` (int32, 0-based), `title`, `description`. All output only.

## Artifacts

**Artifact** — exactly one of `changeSet`, `bashOutput`, `media`.

**ChangeSet** — `source` (`sources/{source}`), `gitPatch`.

**GitPatch** — `baseCommitId` (commit the patch applies to), `unidiffPatch` (unified diff), `suggestedCommitMessage`.

**BashOutput** — `command`, `output` (combined stdout+stderr), `exitCode` (int32).

**Media** — `mimeType` (e.g. `image/png`), `data` (base64-encoded bytes).

## GitHub types

**GitHubRepo** — `owner`, `repo`, `isPrivate` (boolean), `defaultBranch` (GitHubBranch), `branches` (GitHubBranch[]).
**GitHubBranch** — `displayName`.
**GitHubRepoContext** — `startingBranch` (**required**).

## Context

**SourceContext** — `source` (**required**, `sources/{source}`), `githubRepoContext`.

## Outputs

**SessionOutput** — `pullRequest`.
**PullRequest** — `url`, `title`, `description`.

## Activity event types

| Type | Fields |
| --- | --- |
| `PlanGenerated` | `plan` (Plan) |
| `PlanApproved` | `planId` |
| `UserMessaged` | `userMessage` |
| `AgentMessaged` | `agentMessage` |
| `ProgressUpdated` | `title`, `description` |
| `SessionCompleted` | *(no properties)* |
| `SessionFailed` | `reason` |

## Request / response types

| Type | Shape |
| --- | --- |
| `SendMessageRequest` | `prompt` (**required**) |
| `SendMessageResponse` | Empty on success |
| `ApprovePlanRequest` | Empty body |
| `ApprovePlanResponse` | Empty on success |
| `ListSessionsResponse` | `sessions[]`, `nextPageToken` |
| `ListActivitiesResponse` | `activities[]`, `nextPageToken` |
| `ListSourcesResponse` | `sources[]`, `nextPageToken` |

**Source:** <https://jules.google/docs/api/reference/types>
