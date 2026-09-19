# Orchestration Workflow with Jules AI

This rule defines the standard operating procedure (SOP) for Antigravity when asked to orchestrate tasks with Jules AI. 

## The Central Knowledge Vault
All documentation, rules, and skills reside in the central `Knowledge_Vault` folder. Jules' documentation is located in `Knowledge_Vault/Jules_AI`. However, project-specific domain knowledge and architecture rules MUST reside strictly within their respective project repositories (e.g., `VISTA_Project/docs/domain` and `VISTA_Project/docs/rules`) to maintain a single source of truth accessible to remote agents.

## The Orchestration Loop

Whenever the user asks you to delegate or orchestrate a task based on a spec to Jules AI, you MUST follow these steps exactly using the `jules-manager` skill:

1. **Spec Analysis & Roadmap Generation:**
   - Check if `VISTA_Project\Roadmap.md` exists and contains uncompleted tasks. If it does, **SKIP** to Step 2 (Autonomous Task Selection). **Do NOT mark tasks as completed simply because code exists; this is a revision/verification project and the roadmap strictly tracks the layer-by-layer verification progress.**
   - Otherwise, read all specs located in `VISTA_Project\Specs`.
   - Group interconnected specs, identify priorities, and establish a clear run order.
   - Generate or update the `VISTA_Project\Roadmap.md` file to track this prioritized build plan.
   - Read relevant global Rules from `Knowledge_Vault\Rules\` to ensure the task follows the "Genuinely Correct" guidelines.

2. **Autonomous Task Selection:**
   - Identify the next uncompleted priority task from `VISTA_Project\Roadmap.md`.

3. **Task Delegation:**
   - Use the `jules-manager` skill (via `jules remote new --repo . --session "<prompt>"`) to spawn a new session for the selected spec.
   - **CRITICAL CONTEXT BOUNDARY**: Jules AI runs remotely and only has access to the GitHub repository. It cannot access local directories outside the repo (like `Knowledge_Vault`).
   - Therefore, YOU (Antigravity) must read the relevant global rules from `Knowledge_Vault` locally and embed their actual contents/constraints directly into the `<prompt>`. For project-specific rules (like MVP architecture) and domain knowledge, simply instruct Jules to read them from the `docs/` folder within the repository.
   - Ensure any file paths mentioned in the prompt are relative to the repository root (e.g. `Specs/...` instead of `VISTA_Project/Specs/...`).

4. **Monitoring:**
   - Loop synchronously and wait for the session to complete. You do not need to terminate.

5. **Strict Verification:**
   - Pull the remote branch/PR using `jules remote pull --session <session_id>`. (The session ID is returned when you spawn the session).
   - Inspect the code changes strictly.
   - Verify it against the target spec and our rules. Try building or running the relevant commands to ensure it is perfect.

6. **Feedback Loop:**
   - **If NOT Perfect:** Update the `Knowledge_Vault\Rules` with the corrected pattern to ensure this mistake never happens again. Then run `jules remote new` again to fix the issues, referring to the new rule.
   - **If Perfect:** Extract any new reusable patterns or architectural decisions and document them in `Knowledge_Vault\Rules`.

7. **Roadmap Update & Closure:**
   - Mark the current task as completed in `VISTA_Project\Roadmap.md`.
   - Confirm completion with the user and propose starting the next task from the Roadmap.

By following this workflow, Antigravity acts as a strict project manager and Jules acts as the developer, with the `Knowledge_Vault` acting as a growing "Brain" that ensures we never make the same mistake twice.
