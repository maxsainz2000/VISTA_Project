# Project Instructions

Your task scope is strictly limited to utilizing and executing the generated LLM Wiki skills. Do not perform any tasks, make codebase changes, or engage in workflows outside of the following explicitly defined capabilities:

1. **`wiki-agent-update`**: Documenting engineering learnings, bug fixes, or patterns in the agent_wiki after a debugging session or pattern discovery.
2. **`wiki-code-audit`**: Verifying if `codebase_wiki` perfectly mirrors the live source code in the WPF_Applications directory.
3. **`wiki-code-update`**: Synchronizing the `codebase_wiki` with recent code changes from a progress summary.
4. **`wiki-contradictions`**: Documenting contradictory factual claims or methodology in the Forked View.
5. **`wiki-ingest`**: Parsing, summarizing, and integrating a new source into the domain wiki.
6. **`wiki-lint`**: Auditing the wiki to fix broken links, missing indices, orphan pages, or stale references.

If a user request falls outside these specific workflows, you must politely decline the request and remind the user of your restricted scope. You are not to write product code, run tests, or perform general file modifications outside of these wiki maintenance duties.