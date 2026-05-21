---
name: debug-test
description: Debugging protocol for VISTA operator verification testing. Use when the user asks to debug, fix, troubleshoot, or investigate a failing test from any Operator verification checklist (INFRA, POS, ACC, PUR, INT). Also use when the user mentions a test number, a checklist item, or says something is "not working" or "failing" during the testing phase.
---

# VISTA Debug-Test Protocol

This skill is the step-by-step playbook for debugging a failing item from any
`Operator/*-verification-checklist.md`. Follow it **exactly**. Do not improvise.

The full written protocol lives at `Operator/testing-session-protocol.md`. The
test definitions live in the `Operator/*-verification-checklist.md` files.

**BEFORE debugging**, check `LLM_Wiki/agent_wiki/index.md` (and the `errors/`
and `patterns/` directories) for an existing solution to the same symptom. If
one exists, use it instead of re-deriving the fix.

---

## Step 0 — Pre-Flight

1. **Identify the target.** Confirm which checklist (`INFRA`, `POS`, `ACC`,
   `PUR`, `INT`) and which test number is being debugged. If unclear, ask the
   user before doing anything else.
2. **Create the debug branch:**
   ```powershell
   git checkout -b debug/<CHECKLIST-ID>-test-<N>
   ```
   Example: `git checkout -b debug/POS-test-2`.
3. **Create the session log** by copying
   `Operator/debug-logs/_template.md` to
   `Operator/debug-logs/<CHECKLIST-ID>-test-<N>.md` and filling in the
   frontmatter (test id, branch, started timestamp).
4. **Record starting state** in the session log:
   - Commit hash: `git rev-parse --short HEAD`
   - Current build status (clean / N errors)
   - Exact error text or observed misbehavior
5. **List allowed and off-limits files** in the "Allowed Files" /
   "Off-limits" sections of the session log. Module scope rules:
   - POS bug → only `MerchSys.POS/` and `MerchSys.App/Views/POS/`
   - Purchasing bug → only `MerchSys.Purchasing/` and
     `MerchSys.App/Views/Purchasing/`
   - Inventory bug → only `MerchSys.Inventory/` and
     `MerchSys.App/Views/Inventory/`
   - Accounting bug → only `MerchSys.Accounting/` and
     `MerchSys.App/Views/Accounting/`
   - **Never touch `SharedKernel` entities** unless the bug is specifically
     in a shared contract.
   - Never touch other modules' services or handlers.

---

## Step 1 — The One-Fix Loop (max 5 iterations)

For each attempt:

1. **Hypothesize** in the session log: what you believe is wrong and what you
   will change.
2. **Change at most 2 files.** One logical change per attempt — no
   simultaneous refactors.
3. **Build:**
   ```powershell
   dotnet build WPF_Applications/MerchSys/MerchSys.slnx
   ```
4. **Record the result** in the session log with a verdict: ✅ fixed /
   ❌ failed / ⚠️ partial. Include build output and runtime observation.
5. **Decide:**
   - ❌ failed → `git checkout -- .` to revert, then move to the next attempt.
   - ✅ fixed → `git commit -am "fix(<TEST-ID>): <description>"`.
   - ⚠️ partial → commit the partial fix, then continue with a new attempt
     for the remaining issue.
6. **If 5 attempts complete and all are ❌**, proceed to Step 2.

What counts as "one thing":
- ✅ adding a null check in one method
- ✅ fixing a LINQ query plus its assertion
- ✅ registering one missing DI service
- ❌ editing 3 services and a ViewModel "because they're related"
- ❌ refactoring while debugging

---

## Step 2 — Hard Stop (only if 5 attempts exhausted)

1. **Stop making changes immediately.**
2. Write a summary of all 5 attempts in the Resolution section of the session
   log.
3. Ensure all uncommitted changes are reverted (`git checkout -- .`).
4. Tell the user, verbatim:
   > "I've exhausted 5 fix attempts. Here's what I tried: [summary]. What
   > would you like me to try next?"
5. Do **not** continue speculating.

---

## Step 3 — Session End

### If fixed
1. Verify the build is clean (0 errors, 0 warnings).
2. Merge to main:
   ```powershell
   git checkout main
   git merge debug/<CHECKLIST-ID>-test-<N>
   git branch -d debug/<CHECKLIST-ID>-test-<N>
   ```
3. Update the relevant `Operator/<CHECKLIST-ID>-verification-checklist.md`:
   check the box for the test and note what was found.
4. If the fix revealed a new pattern or antipattern, log it under
   `LLM_Wiki/agent_wiki/` following
   `LLM_Wiki/_system/workflow-agent-wiki-update.md` (use the templates in
   `agent_wiki/_templates/`).

### If escalated (5 attempts hit)
1. Keep the debug branch — do not delete it.
2. Leave the session log in place as the handoff document.
3. Wait for operator direction.

---

## Reminders

- The harness enforces this: a PreToolUse hook blocks edits to source files
  unless you are on a `debug/...` branch, and blocks `git commit` while on
  `main`. If you see "BLOCKED:" output, you skipped Step 0.
- File edits are auto-logged to the most recent session log via a
  PostToolUse hook. You still must write the structured Attempt entries
  yourself.
- Build must remain at 0 errors, 0 warnings at session end.
