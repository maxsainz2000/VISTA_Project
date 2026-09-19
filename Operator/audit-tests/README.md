# VISTA Audit-Tests Corpus

**Created:** INFRA-18  
**Purpose:** Regression corpus for the five shape-aware detectors rewritten in INFRA-18.

---

## Directory Layout

```
Operator/audit-tests/
├── rule-03/
│   ├── known-good/   ← detector MUST NOT flag these files
│   │   ├── 01-anonymous-projection-groupby.vb   GroupBy+Select New With before ToListAsync
│   │   ├── 02-select-anonymous-top-products.vb  Select New With + OrderBy + Take before ToListAsync
│   │   ├── 03-scalar-projection.vb              Select(Function(x) x.Id).ToListAsync — scalar
│   │   ├── 04-select-orderby-anonymous.vb       GroupBy → Select New With → OrderByDesc → ToListAsync
│   │   └── 05-select-movement-projection.vb     GroupBy → Select New With → ToListAsync
│   └── known-bad/    ← detector MUST flag these files (≥1 finding each)
│       ├── 01-bare-dbset.vb                     _db.Vendors.ToListAsync() — bare DbSet
│       ├── 02-where-tolistasync.vb              .Where().OrderBy().ToListAsync()
│       ├── 03-include-tolistasync.vb            .Include().ToListAsync()
│       ├── 04-asnotracking-tolistasync.vb       .AsNoTracking().IgnoreQueryFilters().ToListAsync()
│       └── 05-multiple-include-tolistasync.vb   Multiple .Include() chains ending ToListAsync
│
├── rule-07/
│   ├── known-good/
│   │   ├── 01-abstractions-only.vb              Only Imports ...Abstractions; Console.Write in code
│   │   ├── 02-no-console-usage.vb               Bare MEL import but no Console. in executable code
│   │   └── 03-console-in-comment-only.vb        Bare MEL import + Console. only inside '...comment
│   └── known-bad/
│       ├── 01-bare-import-console-usage.vb      Exact bare Imports MEL + Console.WriteLine
│       ├── 02-bare-import-console-in-method.vb  Bare MEL + Console.Write in instance method
│       └── 03-bare-import-with-system-and-console.vb  Multiple imports including bare MEL + Console.
│
├── rule-12/
│   ├── known-good/
│   │   ├── 01-tuple-array-receiver.vb           .Count(pred) on tuple array literal
│   │   ├── 02-ienumerable-receiver.vb           .Count(pred) on IEnumerable(Of T)
│   │   └── 03-typed-array-receiver.vb           .Count(pred) on String() array
│   └── known-bad/
│       ├── 01-list-of-t-predicate-count.vb      Explicit As List(Of T) parameter receiver
│       ├── 02-new-list-of-t-predicate-count.vb  As New List(Of T) local variable receiver
│       └── 03-list-field-predicate-count.vb     Class field typed As List(Of T)
│
├── rule-14/
│   ├── known-good/
│   │   ├── 01-shared-method.vb                  Shared method — no instance scope
│   │   ├── 02-module-member.vb                  Method inside Module — no instance state
│   │   └── 03-param-no-matching-property.vb     Instance method but param has no matching property
│   └── known-bad/
│       ├── 01-constructor-shadows-property.vb   Sub New params shadow ReadOnly properties
│       ├── 02-navigation-group-constructor.vb   Constructor params shadow Properties
│       └── 03-instance-method-shadows-property.vb  Instance Sub param shadows ObservableCollection
│
└── rule-19/
    ├── known-good/
    │   ├── 01-mainwindow-in-comment.vb          Application.Current.MainWindow inside '... comment
    │   ├── 02-mainwindow-assignment.vb          Application.Current.MainWindow = x (assignment)
    │   └── 03-no-mainwindow-reference.vb        File has no MainWindow reference at all
    └── known-bad/
        ├── 01-mainwindow-dereference.vb         .MainWindow?.DataContext dereference
        ├── 02-mainwindow-trycast.vb             TryCast(Application.Current.MainWindow, ...)
        └── 03-mainwindow-property-read.vb       .MainWindow.Title property read
```

---

## Corpus Contract

| Directory | Expected detector result |
|-----------|--------------------------|
| `<rule>/known-good/` | **0 findings** — detector must not flag any line in the file |
| `<rule>/known-bad/`  | **≥1 finding** — detector must flag at least one line in the file |

Each sample file includes a comment header explaining why it is good or bad, and
which real source file it traces back to when sourced from a verified finding.

---

## How to Run the Corpus Self-Test

The corpus self-test is part of the `/vista-audit` skill workflow (see
`.claude/skills/vista-audit/SKILL.md`, section "Corpus Self-Test"). Run it before
every live codebase scan:

```
Invoke the vista-audit skill and request a corpus self-test only.
The skill will:
  1. Apply each corrected detector to every file in known-good/ — assert 0 findings.
  2. Apply each corrected detector to every file in known-bad/  — assert ≥1 finding.
  3. Report PASS or FAIL per rule before proceeding to the live scan.
```

Any FAIL means a detector is still incorrect. Fix the detector (in
`.claude/skills/vista-audit/SKILL.md`) before running the live scan.

---

## Regression Rule

> A detector change that misclassifies **any** corpus file is rejected.

When modifying a detector:
1. Run the corpus self-test first.
2. If a known-good file is now flagged → the new detector has a false-positive regression.
3. If a known-bad file is no longer flagged → the new detector has a false-negative regression.
4. Fix the detector or the corpus (if the corpus file was wrong), not the source code.

---

## Adding New Corpus Samples

When a new false-positive or false-negative is discovered in a live scan:

1. Create a minimal reproducer in the appropriate `known-good/` or `known-bad/` directory.
2. Name it `<NN>-<short-description>.vb` using the next available sequence number.
3. Add a comment header explaining why it is good/bad and which live file it traces to.
4. Update the directory tree table above.
5. Re-run the corpus self-test to confirm it classifies correctly.

---

## Baseline Counts (2026-05-24 audit → INFRA-18 corrected)

| Rule | Before (baseline) | After (corrected detector) | Delta |
|------|-------------------|---------------------------|-------|
| 3 — ToListAsync entity empty | 63 | ~35–40 | −23 to −28 |
| 7 — Console shadow           | 8  | 0       | −8         |
| 12 — List.Count predicate    | 1  | 0       | −1         |
| 14 — Param shadows property  | 20 | ~9–11   | −9 to −11  |
| 19 — MainWindow not shell    | 1  | 0       | −1         |
| **Total**                    | **93** | **~44–51** | **−42 to −49** |

After counts are sample-extrapolated from the 2026-05-24 verification analysis.
The live scan following INFRA-18 will produce the authoritative after-counts.
