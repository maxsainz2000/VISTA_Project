# Testing Session Protocol

> **Purpose:** This document defines the mandatory rules for any debugging session during the VISTA testing phase.
> Every coding agent MUST read and follow this protocol before making ANY code changes to fix a bug found during operator verification testing.
>
> **Scope:** Applies whenever working on items from the `Operator/*-verification-checklist.md` files.

---

## Pre-Flight Checklist (before ANY code change)

Before touching a single line of code, you MUST complete all four steps:

1. **Create a debug branch**
   ```powershell
   git checkout -b debug/<CHECKLIST-ID>-test-<N>
   ```
   Example: `git checkout -b debug/POS-test-2`

2. **Create or open the session log**
   ```
   Operator/debug-logs/<CHECKLIST-ID>-test-<N>.md
   ```
   Use the template at `Operator/debug-logs/_template.md`.

3. **Record the starting state** in the session log:
   - Current commit hash (`git rev-parse --short HEAD`)
   - Current build status (clean / N errors)
   - The exact error message or unexpected behavior

4. **Identify the scope boundary** — list which files you are allowed to touch for this fix.
   Write them in the "Allowed Files" section of the session log.
   Rule of thumb:
   - POS bug → only `MerchSys.POS/` and `MerchSys.App/Views/POS/`
   - Purchasing bug → only `MerchSys.Purchasing/` and `MerchSys.App/Views/Purchasing/`
   - Never touch `SharedKernel` entities unless the bug is specifically in a shared contract
   - Never touch other modules' services or handlers

---

## The One-Fix Rule

This is the most important rule. It prevents cascading damage.

### For each fix attempt:

1. **Hypothesize** — Write in the session log what you think the problem is and what you plan to change
2. **Change ONE thing** — Edit at most 2 files. A "thing" is a single logical change (e.g., fix a null check, correct a query, add a missing DI registration)
3. **Build** — Run `dotnet build WPF_Applications/MerchSys/MerchSys.slnx` immediately
4. **Record** — Write the result in the session log:
   - What happened (build output, runtime behavior)
   - Verdict: ✅ fixed / ❌ failed / ⚠️ partial
5. **Decide:**
   - ✅ **Fixed** → Commit: `git commit -am "fix(<TEST-ID>): <description>"`
   - ❌ **Failed** → Revert immediately: `git checkout -- .`
   - ⚠️ **Partial** → Commit the partial fix, then start the next attempt for the remaining issue

### What counts as "one thing"?
- ✅ Adding a missing null check in one method
- ✅ Fixing a LINQ query and its corresponding test assertion
- ✅ Registering a missing DI service in the startup file
- ❌ Changing 3 service classes and a ViewModel "because they're all related"
- ❌ Refactoring a method while also fixing a bug in it

---

## Hard Stop After 5 Attempts

If you have made **5 failed fix attempts** for the same test item:

1. **STOP making changes**
2. Write a summary in the session log covering all 5 attempts
3. Ensure all changes are reverted (`git checkout -- .`)
4. **Ask the operator what to do next**

Do NOT continue making speculative changes past this limit. The operator will decide whether to:
- Give you a hint or new direction
- Bring in a different debugging approach
- Defer the test item

---

## Forbidden Actions During Debugging

| ❌ Forbidden | ✅ Do Instead |
|-------------|--------------|
| Editing more than 2 files in one attempt | Break the fix into smaller steps |
| Making a second attempt without logging the first | Record the result, then proceed |
| Continuing after 5 failed attempts | Stop and escalate to operator |
| Changing code unrelated to the current test | Create a separate issue/note |
| Refactoring while debugging | Fix the bug first, refactor later |
| Changing `SharedKernel` entities for a module bug | Check if the contract is actually wrong first |
| Deleting or renaming files speculatively | Only delete if you are certain it's the fix |

---

## Session End Checklist

When the debugging session is complete (whether fixed or escalated):

### If fixed:
1. Verify the build is clean: 0 errors, 0 warnings
2. Merge the debug branch:
   ```powershell
   git checkout main
   git merge debug/<branch-name>
   git branch -d debug/<branch-name>
   ```
3. Update the relevant Operator checklist — check the box and write what you saw
4. If the fix revealed a new pattern or antipattern:
   - Log it in `LLM_Wiki/agent_wiki/` following `LLM_Wiki/_system/workflow-agent-wiki-update.md`

### If escalated (5 attempts exhausted):
1. Ensure all changes are reverted on the debug branch
2. Keep the debug branch for reference: `git checkout main` (do NOT delete the branch yet)
3. The session log in `Operator/debug-logs/` serves as the handoff document
4. Report to the operator with a summary of what was tried

---

## Quick Reference

```
# Start debugging
git checkout -b debug/POS-test-2
# Create session log from template
# Record starting state

# Attempt loop (max 5)
  1. Write hypothesis in log
  2. Change ≤2 files
  3. dotnet build
  4. Record result in log
  5. If ❌ → git checkout -- .
     If ✅ → git commit -am "fix(POS-test-2): ..."

# End
git checkout main
git merge debug/POS-test-2
# Update checklist
```
