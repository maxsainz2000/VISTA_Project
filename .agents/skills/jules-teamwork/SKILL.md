---
name: "jules-teamwork"
description: "Executes a daily supervised sequential burst of up to 100 Jules AI sessions. Antigravity orchestrates, verifies, and manages docs; Jules strictly codes."
---

# Jules Teamwork Workflow

This skill executes the "Daily Sprint" architecture for Jules AI. Since the user's laptop does not run 24/7, this skill acts as a rapid-fire sequential loop (a "Supervised Sprint") that consumes up to 100 Jules AI sessions while the laptop is open.

## Core Architecture Roles
- **Jules AI**: The sole developer. It only writes code in the cloud.
- **Antigravity (You & Your Subagents)**: The Tech Lead, QA, and Product Owner. You orchestrate tasks, strictly verify PRs, and manage the specs/Knowledge Vault. You do not write application code.

## The Daily Sprint Workflow

When the user types `/jules-teamwork`, execute the following strict sequence autonomously:

### Phase 1: Autonomous Resume
1. Do not ask for a goal unless the backlog is empty.
2. Read the existing `Knowledge_Vault`, `Specs`, and `Roadmap.md`. (If you need a refresher on Jules CLI syntax, refer to the `jules-manager` skill).
3. Identify the next uncompleted atomic task from the roadmap.

> [!IMPORTANT]
> **ROADMAP INTEGRITY CONSTRAINT**:
> DO NOT auto-complete tasks in `Roadmap.md` based on your own static analysis of the codebase. The user is actively migrating the architecture (WPF to WinForms MVP), so legacy code existing does NOT mean the task is complete. Only mark a task as `[x]` AFTER a Jules session specifically completes it and you merge the PR during Phase 2.

### Phase 2: Supervised Sequential Burst (The Loop)
Execute this loop up to 100 times (or until the roadmap is complete/user interrupts):

1. **Pre-Launch Sync (CRITICAL)**: 
   - Jules runs in the cloud and pulls from the remote repository.
   - Therefore, before delegating to Jules, you MUST autonomously `git commit` and `git push` any updates you made to the Knowledge Vault, Specs, or Roadmap.
2. **Launch**: 
   - Since we want to bypass manual plan approval, you MUST use the REST API via `curl` instead of the `jules` CLI.
   - Dispatch the task to Jules via API (Read API key from `~/.jules/api_key`):
   ```bash
   $apiKey = Get-Content ~/.jules/api_key
   $body = @{
       prompt = "<detailed_prompt_including_specs_and_rules>"
       sourceContext = @{
           source = "sources/github/maxsainz2000/VISTA_Project"
           githubRepoContext = @{ startingBranch = "master" }
       }
   } | ConvertTo-Json -Depth 10

   curl.exe -X POST -H "x-goog-api-key: $apiKey" -H "Content-Type: application/json" -d $body https://jules.googleapis.com/v1alpha/sessions
   ```
3. **Monitor (Active Polling)**: 
   - **CRITICAL**: You MUST use the `schedule` tool (e.g., `DurationSeconds=120`) to actively monitor the Jules session's progress in the background.
   - Do NOT end your turn waiting for the user to prompt you. This is a fully automated loop. You must autonomously wake yourself up, run `jules remote list --session`, and repeat the timer until the session status is `Completed`.
   - If the session status changes to `AWAITING_USER_FEEDBACK` (or `Awaiting User F`), you MUST read Jules's messages via the activities API (`curl -H "x-goog-api-key: $apiKey" https://jules.googleapis.com/v1alpha/sessions/<ID>/activities?pageSize=5`). 
   - Reply to Jules using the `:sendMessage` API endpoint to unblock it. (Do NOT let it auto-complete Roadmap tasks; instruct it to submit the PR if work is done).
   - Once Jules completes the task, proceed immediately to Verification.
4. **Strict Verification**: 
   - Pull the PR locally.
   - Delegate verification to your `DeepInvestigator` subagent or perform rigorous checks yourself (run tests, check edge cases, verify against specs).
5. **Outcome - Perfect**:
   - Merge the PR locally and `git push`.
   - Mark the task as `[x]` in `Roadmap.md`.
   - Loop to the next task.
6. **Outcome - Failed (Fail Fast & Document)**:
   - **DO NOT fix the code locally.**
   - Reject the PR entirely.
   - Analyze exactly why Jules failed.
   - Update the `Knowledge_Vault` or the specific `Spec` to clarify the ambiguity or missing constraint that caused the failure.
   - (Remember to commit and push these doc changes at the start of the next loop iteration).
   - Re-assign the failed task to Jules as a completely fresh session (burning another session).

### Phase 3: Finalization
Once the 100 session limit is reached, or all roadmap tasks are complete:
1. Cease sending tasks to Jules.
2. Review and finalize the contents of `Roadmap.md`, `Specs`, and `Knowledge_Vault` to ensure they perfectly reflect the current state of the codebase.
3. Commit and push all final documentation changes.
4. Output a summary report for the user, confirming the workspace is prepped for tomorrow's `/jules-teamwork` command.
