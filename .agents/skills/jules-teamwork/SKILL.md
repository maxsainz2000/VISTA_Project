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

### Phase 2: Supervised Sequential Burst (The Loop)
Execute this loop up to 100 times (or until the roadmap is complete/user interrupts):

1. **Pre-Launch Sync (CRITICAL)**: 
   - Jules runs in the cloud and pulls from the remote repository.
   - Therefore, before delegating to Jules, you MUST autonomously `git commit` and `git push` any updates you made to the Knowledge Vault, Specs, or Roadmap.
2. **Launch**: 
   - Dispatch the task to Jules via CLI.
   ```bash
   jules remote new --repo . --session "<detailed_prompt_including_specs_and_rules>"
   ```
3. **Monitor**: 
   - Wait for Jules to complete the task and open a Pull Request.
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
