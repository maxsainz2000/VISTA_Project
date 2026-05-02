---
type: system
title: "Workflow — Contradictions (The Forked View)"
last-updated: 2026-05-02
---

# Workflow: Contradictions (The Forked View)

When two sources contradict each other on a factual claim, methodology, or design decision, follow this workflow.

## Rules

- **DO NOT FLATTEN** the contradiction into a general "balanced" summary.
- **DO NOT JUDGE** which source is correct. The wiki is a compendium, not an arbiter.
- Both positions must be preserved with full attribution.

## Checklist

1. **Go to the Entity/Concept page** where the contradiction appears.
   - Append a `> [!conflict]` callout that:
     - States the claim in dispute
     - Cites the new source that contradicts the existing claim
     - Links to the Forked View page (created in step 2)

2. **Create a Forked View page** in `wiki/_views/`:
   - Filename: `topic-claim1-vs-claim2.md`
   - Include YAML frontmatter:
     ```yaml
     ---
     type: analysis
     title: "Topic — Claim A vs Claim B"
     sources: [source-a, source-b]
     related: [entity-or-concept-page]
     last-updated: YYYY-MM-DD
     ---
     ```
   - Add a side-by-side comparison with **direct quotes** from each source.
   - Present both positions neutrally. Do not editorialize.

3. **Update `wiki/index.md`** — add the new Forked View page.

4. **Return to your original workflow** (Ingest, Query, or Lint) and continue from where you left off.

## Example Callout

```markdown
> [!conflict] Contradicting claim on FIFO implementation
> The system plan states FIFO is applied at the batch level per product.
> However, `Sources/Accounting-Module_AcademicPaper.md` describes FIFO at
> the transaction level. See [[fifo-batch-vs-transaction]] for full comparison.
```
