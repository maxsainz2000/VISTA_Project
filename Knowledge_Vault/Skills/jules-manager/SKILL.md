---
name: "jules-manager"
description: "Teaches Antigravity how to manage Jules AI sessions, read progress, and review PRs using the @google/jules CLI."
---

# Jules Manager Skill

This skill allows Antigravity to act as the primary interface to Jules AI. You will use the `@google/jules` CLI to orchestrate Jules sessions on behalf of the user.

## Prerequisites
- The `@google/jules` CLI must be installed globally (`npm install -g @google/jules`).
- The CLI must be authenticated. If it fails due to authentication, ask the user to manually run `jules login` in their terminal.

## Core Workflows

### Starting a Task (Session)
When the user asks you to assign a task to Jules:
1. Ensure you are in the repository directory where you want to work (or pass `--repo <repo_name>`).
2. Run the command:
   ```bash
   jules remote new --repo . --session "<detailed_prompt_from_user>"
   ```
3. You can optionally use `--parallel <number>` (up to 5) if you want Jules to attempt multiple solutions at once.
4. Note the session ID returned so you can monitor it.

### Monitoring Tasks
To check on Jules' progress:
1. List active/past sessions:
   ```bash
   jules remote list --session
   ```
2. For specific output or parsing, you may need to parse the JSON or table output (if the CLI supports JSON output). Alternatively, just read the standard output and summarize it for the user.

### Reviewing Results
When Jules finishes a task:
1. You can pull the resulting code changes directly to your local workspace to review or test them:
   ```bash
   jules remote pull --session <session_id>
   ```
2. After pulling, inspect the diff (e.g. `git diff`) and report the changes to the user, or run tests to verify Jules' work.

## Integration Examples

- **Batch Tasks**: If the user provides a `TODO.md` file, you can script a loop to submit each line as a new Jules session.
- **Testing Before Commit**: If Jules creates a PR, you can `jules remote pull` its work locally, run the local test suite using Antigravity, and report back to the user whether the PR is solid.

## When to use this skill
- Any time the user mentions "ask Jules to..." or "check on Jules" or "did Jules finish?".
- When you encounter tasks that are better handled by a remote, asynchronous agent (e.g. large refactors while you help the user with something else).

## Rules Integration
**IMPORTANT**: When orchestrating tasks using this skill, you MUST follow the SOP defined in the `orchestration-workflow.md` rule. This includes checking for an existing `Roadmap.md` to pick up where you left off (or generating a new one based on the specs in `VISTA_Project\Specs`), autonomously selecting tasks, and keeping the Roadmap updated as tasks complete.
