---
name: "jules-teamwork"
description: "Executes a daily supervised sequential burst of up to 100 Jules AI sessions. Antigravity orchestrates, delegates API polling to a monitor subagent, strictly verifies via DeepInvestigator, and manages docs; Jules strictly codes."
---

# Jules Teamwork Workflow

This skill executes the "Daily Sprint" architecture for Jules AI. Since the user's laptop does not run 24/7, this skill acts as a rapid-fire sequential loop (a "Supervised Sprint") that consumes up to 100 Jules AI sessions while the laptop is open.

## Core Architecture Roles
- **Jules AI**: The sole developer. It only writes code in the cloud.
- **Antigravity (Main Agent)**: The Tech Lead and Product Owner. You orchestrate tasks and manage the specs/Roadmap. You do NOT poll the API, wait, or write application code.
- **JulesMonitor Subagent**: A dedicated subagent invoked by the Main Agent to handle the API dispatch and all background polling.
- **DeepInvestigator Subagent**: The strict QA agent. Mandatorily invoked by the Main Agent to verify all Jules PRs against objective criteria.

## The Daily Sprint Workflow

When the user types `/jules-teamwork`, execute the following strict sequence autonomously:

### Phase 1: Autonomous Resume
1. Do not ask for a goal unless the backlog is empty.
2. Read the existing `Knowledge_Vault`, `Specs`, and `Roadmap.md`. (If you need a refresher on Jules CLI syntax, refer to the `jules-manager` skill).
3. Identify the next uncompleted atomic task from the roadmap. Ensure the task has objective acceptance criteria (a forcing function).

> [!IMPORTANT]
> **ROADMAP INTEGRITY CONSTRAINT**:
> DO NOT auto-complete tasks in `Roadmap.md` based on your own static analysis of the codebase. The user is actively migrating the architecture (WPF to WinForms MVP), so legacy code existing does NOT mean the task is complete. Only mark a task as `[x]` AFTER a Jules session specifically completes it and you merge the PR during Phase 2.

### Phase 2: Supervised Sequential Burst (The Loop)
Execute this loop up to 100 times (or until the roadmap is complete/user interrupts):

#### 1. Pre-Launch Sync (CRITICAL)
- Jules runs in the cloud and pulls from the remote repository.
- Before delegating, the Main Agent MUST autonomously `git commit` and `git push` any updates made to the Knowledge Vault, Specs, or Roadmap.

#### 2. Dispatch to Monitor Subagent
- The Main Agent MUST NOT hit the Jules API directly or poll in its own context.
- Instead, the Main Agent invokes a subagent (using `invoke_subagent` with `TypeName='self'`, assigned the role of `JulesMonitor`).
- Pass the following prompt to the `JulesMonitor`:
  "Send a POST request to the Jules API to start the following task: <detailed_prompt_including_specs_and_rules>. Use the REST API via `curl` (Read API key from `~/.jules/api_key`). After launching, use the `schedule` tool to actively poll the session status. Resolve any `AWAITING_USER_FEEDBACK` prompts. Do not report back to me until the PR is pushed and the session is Completed."
- The Main Agent then stops and waits for the Monitor's response.

#### 3. Mandatory QA via DeepInvestigator
- Once the `JulesMonitor` subagent reports the PR is ready, the Main Agent MUST invoke the `DeepInvestigator` subagent to verify the PR.
- Pass strict, objective acceptance criteria from the Roadmap to `DeepInvestigator`. 
- `DeepInvestigator` will pull the PR locally, run tests/checks against the rubric, and return a binary Pass or Fail report.

#### 4. Resolution
- **Outcome - Perfect (Pass):**
  - The Main Agent merges the PR locally and executes `git push`.
  - Mark the task as `[x]` in `Roadmap.md`.
  - Loop to Phase 2, Step 1 for the next task.
- **Outcome - Failed (Fail Fast & Document):**
  - **DO NOT fix the code locally.**
  - Reject the PR entirely.
  - Analyze exactly why Jules failed based on `DeepInvestigator`'s report.
  - Update the `Knowledge_Vault` or the specific `Spec` to clarify the ambiguity or missing constraint that caused the failure.
  - (These doc changes will be committed and pushed at the start of the next loop iteration).
  - Burn a new session: re-assign the failed task to Jules as a completely fresh session.

### Phase 3: Finalization
Once the 100 session limit is reached, or all roadmap tasks are complete:
1. Cease sending tasks to Jules.
2. Review and finalize the contents of `Roadmap.md`, `Specs`, and `Knowledge_Vault` to ensure they perfectly reflect the current state of the codebase.
3. Commit and push all final documentation changes.
4. Output a summary report for the user, confirming the workspace is prepped for tomorrow's `/jules-teamwork` command.
