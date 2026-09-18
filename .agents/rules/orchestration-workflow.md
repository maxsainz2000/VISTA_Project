# Orchestration Workflow with Jules AI

This rule defines the standard operating procedure (SOP) for Antigravity when asked to orchestrate tasks with Jules AI. 

## The Central Knowledge Vault
All documentation, rules, and skills reside in the central `Knowledge_Vault` folder. Jules' documentation is located in `Knowledge_Vault/Jules_AI`.

## The Orchestration Loop

Whenever the user asks you to delegate or orchestrate a task based on a spec to Jules AI, you MUST follow these steps exactly using the `jules-manager` skill:

1. **Spec Analysis & Roadmap Generation:**
   - Check if `VISTA_Project\Roadmap.md` exists and contains uncompleted tasks. If it does, **SKIP** to Step 2 (Autonomous Task Selection).
   - Otherwise, read all specs located in `VISTA_Project\Specs`.
   - Group interconnected specs, identify priorities, and establish a clear run order.
   - Generate or update the `VISTA_Project\Roadmap.md` file to track this prioritized build plan.
   - Read relevant Rules from `Knowledge_Vault\Rules\` to ensure the task follows the "Genuinely Correct" guidelines.

2. **Autonomous Task Selection:**
   - Identify the next uncompleted priority task from `VISTA_Project\Roadmap.md`.

3. **Task Delegation:**
   - Use the `jules-manager` skill (via `jules remote new --repo . --session "<prompt>"`) to spawn a new session for the selected spec.
   - Ensure your prompt is detailed, includes the exact spec details, and enforces the rules found in the Knowledge Vault.

4. **Monitoring:**
   - Terminate your current process/session. You do not need to wait. The event-driven webhook listener will automatically spawn a new Antigravity instance when Jules completes the task and opens a PR.

5. **Strict Verification:**
   - Pull the remote branch/PR using `gh pr checkout <pr_number>` (the PR number is provided by the webhook payload).
   - Inspect the code changes strictly.
   - Verify it against the target spec and our rules. Try building or running the relevant commands to ensure it is perfect.

6. **Feedback Loop:**
   - **If NOT Perfect:** Update the `Knowledge_Vault\Rules` with the corrected pattern to ensure this mistake never happens again. Then run `jules remote new` again to fix the issues, referring to the new rule.
   - **If Perfect:** Extract any new reusable patterns or architectural decisions and document them in `Knowledge_Vault\Rules`.

7. **Roadmap Update & Closure:**
   - Mark the current task as completed in `VISTA_Project\Roadmap.md`.
   - Confirm completion with the user and propose starting the next task from the Roadmap.

By following this workflow, Antigravity acts as a strict project manager and Jules acts as the developer, with the `Knowledge_Vault` acting as a growing "Brain" that ensures we never make the same mistake twice.
