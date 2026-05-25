---
name: codebase-audit
description: VISTA codebase audit — scans all VB.NET source files under WPF_Applications/MerchSys/src/ against every agent-wiki rule and writes a report. Shape-aware detectors for Rules 3, 7, 12, 14, 19 were rewritten in INFRA-18 to eliminate the ~45% false-positive rate from the 2026-05-24 baseline run.
---

# VISTA Codebase Audit Skill

This skill performs a full audit of all `.vb` files in `WPF_Applications/MerchSys/src/`.
It checks every rule in `LLM_Wiki/agent_wiki/` and writes a report to
`Operator/debug-logs/agent-wiki-verification-report.md`.

**Read the detector specs below before scanning any file.** These replace the
literal-grep approach used prior to INFRA-18.

---

## Pre-flight

1. Use `Glob` with `WPF_Applications/MerchSys/src/**/*.vb` to build the full file list.
2. Read `LLM_Wiki/agent_wiki/index.md` for the current rule list and wiki entry paths.
3. For each rule, apply the detector described in this skill. Do not fall back to
   plain substring search for Rules 3, 7, 12, 14, or 19.

---

## Detector: Rule 3 — ToListAsync full-entity materialisation

**Wiki:** `agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md`

### Algorithm

For each `.vb` file, gather all logical lines (join continuation lines ending in `_`
into a single chain string). For each chain that contains `.ToListAsync`:

1. **Tokenise the chain** by splitting on `.` after stripping leading whitespace and
   line-continuation characters (`_`). This gives the ordered call sequence.
2. **Check the terminator:** the last token must start with `ToListAsync`. If not, skip.
3. **Check for Select projection (negative gate):** scan backward through the tokens
   from the `ToListAsync` position. If a `Select(Function(` token appears **after the
   last** `Where`, `Include`, `ThenInclude`, `AsNoTracking`, `IgnoreQueryFilters`,
   `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`, `Take`, `Skip`, or
   bare `_db.` DbSet reference, **do NOT flag** — the chain contains an anonymous,
   scalar, or DTO projection.
4. **Check for GroupBy (negative gate):** if any token in the chain contains
   `GroupBy(`, **do NOT flag**.
5. **Flag** if all of the following hold:
   - The chain ends with `.ToListAsync(...)`.
   - The token immediately before `ToListAsync` is one of:
     `Where(`, `OrderBy(`, `OrderByDescending(`, `ThenBy(`, `ThenByDescending(`,
     `Include(`, `ThenInclude(`, `AsNoTracking(`, `IgnoreQueryFilters(`,
     `Take(`, `Skip(`, or a bare DbSet reference (e.g., `_db.Vendors`).
   - No `Select(Function(` token appears between the last `Where`/`Include`/`DbSet`
     position and the `ToListAsync` position.
   - No `GroupBy(` token appears anywhere in the chain.

### Known false-positive shapes (never flag)

```vb
' Anonymous projection — last step before ToListAsync is .Select(Function(g) New With {...})
.GroupBy(...).Select(Function(g) New With { Key .Year = g.Key.Year, ... }).ToListAsync()

' Scalar projection — Select returns a single member
.Select(Function(x) x.Id).ToListAsync()

' OrderByDescending after Select(...New With{...}) — still a projection chain
.Select(Function(g) New With {...}).OrderByDescending(Function(g) g.Revenue).ToListAsync()
```

### Known true-positive shapes (always flag)

```vb
' Bare DbSet
_db.Vendors.ToListAsync()

' Where only
_db.Vendors.AsNoTracking().IgnoreQueryFilters().ToListAsync()

' Where + OrderBy (no Select)
_db.StockBatches.Where(Function(b) b.ProductId = id).OrderBy(Function(b) b.ReceiptDate).ToListAsync()

' Include chain (full entity + navigation)
_db.Products.Include(Function(p) p.StockBatches).ToListAsync()

' Multiple Include
_db.Products.Where(...).Include(Function(p) p.Category).Include(Function(p) p.StockBatches).OrderBy(...).ToListAsync()
```

---

## Detector: Rule 7 — Console namespace shadow

**Wiki:** `agent_wiki/antipatterns/vbnet-console-namespace-shadow.md`

### Algorithm

For each `.vb` file:

1. **Build the import list:** collect every line matching `^\s*Imports\s+` (case-
   insensitive). Extract the imported namespace string.
2. **Check for exact MEL import (gate):** the import list must contain EXACTLY
   `Microsoft.Extensions.Logging` (no suffix). Do **not** match:
   - `Microsoft.Extensions.Logging.Abstractions`
   - `Microsoft.Extensions.Logging.Console`
   - `Microsoft.Extensions.Logging.Configuration`
   - Any other `Microsoft.Extensions.Logging.*` sub-namespace.
   If this import is absent, skip the file entirely — zero findings.
3. **Strip comments:** for each non-import line, remove everything from the first
   unquoted `'` to end-of-line before searching.
4. **Check for Console. usage:** search the comment-stripped lines for a match of
   `Console\.` (the literal dot ensures it is a member access, not the word "Console"
   alone).
5. **Flag** every line that matches `Console\.` in the stripped text, but only if
   the gate in step 2 fired.

### Do NOT flag

```vb
' File has only Imports Microsoft.Extensions.Logging.Abstractions — NOT the exact bare import
Imports Microsoft.Extensions.Logging.Abstractions
Console.WriteLine("debug")    ' NOT flagged — .Abstractions does not bring Console class into scope
```

```vb
' File has the bare import but Console. only appears in a comment
Imports Microsoft.Extensions.Logging
' Console.WriteLine was removed during refactor
Dim x = 1     ' NOT flagged — no Console. in executable code
```

### Flag

```vb
Imports Microsoft.Extensions.Logging   ' exact bare import — gate fires
...
Console.WriteLine("done")              ' FLAGGED — Console resolves to MEL class
```

---

## Detector: Rule 12 — List.Count predicate

**Wiki:** `agent_wiki/antipatterns/vbnet-list-count-property-shadows-linq-extension.md`

### Algorithm

For each `.vb` file:

1. **Find receiver declarations:** collect all `Dim` / `As` declarations in the current
   scope. Record names and declared types. A declaration matches `List(Of T)` if the
   `As` clause contains `List(Of` (case-insensitive). Also match `As New List(Of`.
2. **Find predicate-Count calls:** search for the pattern
   `<identifier>\.Count\(Function\s*\(` (case-insensitive). Extract the identifier.
3. **Resolve receiver type:** look up the identifier in the scope's declaration map.
4. **Flag** only if the resolved type is `List(Of T)` or `New List(Of T)`.
5. **Skip** if the resolved type is any of:
   - Array type: `T()`, `As T()`, `As Array`, `{...}` literal initialiser, tuple arrays
   - `IEnumerable(Of T)`, `IQueryable(Of T)`, `ICollection(Of T)`
   - A method return whose type cannot be resolved from local scope

### Do NOT flag

```vb
' Receiver is a tuple array — array has no Count property
Dim checks = {("a", 1), ("b", 2), ("c", 3)}
Dim n = checks.Count(Function(t) t.Item2 > 1)   ' NOT flagged — array, not List(Of T)
```

```vb
' Receiver is IEnumerable(Of T)
Dim items As IEnumerable(Of String) = GetItems()
Dim n = items.Count(Function(x) x.Length > 3)   ' NOT flagged — LINQ extension unambiguous
```

### Flag

```vb
' Receiver is explicitly List(Of T)
Dim myList As List(Of Product) = New List(Of Product)()
Dim active = myList.Count(Function(p) p.IsActive)   ' FLAGGED — resolves to Count property, not LINQ
```

---

## Detector: Rule 14 — Parameter shadows property

**Wiki:** `agent_wiki/antipatterns/vbnet-parameter-shadows-property.md`

### Algorithm

For each `.vb` file:

1. **Build the type map:** for each `Class` or `Structure` block, collect:
   - Instance `Property` declarations (not `Shared Property`)
   - Instance `Dim` field declarations (not `Shared`)
   - Record the enclosing type name and the property/field names.
2. **Find method declarations:** within each class/structure, find every `Sub`,
   `Function`, or `Sub New` declaration. For each method:
   a. **Shared gate:** if the declaration includes `Shared`, skip — shared methods have
      no instance scope.
   b. **Module gate:** if the enclosing container is a `Module` (not a `Class` or
      `Structure`), skip — modules have no instance state.
   c. **Extract parameter names** from the parameter list.
   d. **Match against instance properties/fields:** case-insensitively compare each
      parameter name against the collected instance property/field names for the
      enclosing class/structure.
   e. **Flag** any parameter that case-insensitively matches an instance property or
      non-shared field of the same class/structure.

### Do NOT flag

```vb
' Shared method — no instance scope
Public Shared Function Classify(avgDailySales As Decimal) As String
    ' avgDailySales doesn't shadow any instance property because Shared has no instance
End Function

' Module member — modules have no instance properties
Friend Module PasswordHashHelper
    Public Function Verify(password As String, storedHash As String) As Boolean
        ' 'password' and 'storedHash' have no instance properties to shadow
    End Function
End Module
```

### Flag

```vb
' Instance method with parameter matching a property (case-insensitive)
Public Class VatReturnLockedException
    Public ReadOnly Property ReturnId As Integer
    Public ReadOnly Property Year As Integer

    Public Sub New(returnId As Integer, year As Integer, ...)
        ' 'returnId' shadows ReturnId, 'year' shadows Year — FLAGGED
        Me.ReturnId = returnId   ' mitigated, but still flagged as fragile
        Me.Year = year
    End Sub
End Class
```

---

## Detector: Rule 19 — MainWindow not shell window

**Wiki:** `agent_wiki/patterns/wpf-mainwindow-not-shell-window.md`

### Algorithm

For each `.vb` file:

1. **Strip comments:** for each line, remove everything from the first unquoted `'`
   to end-of-line. The result is the executable-code portion of the line.
2. **Skip assignments:** if the stripped line contains
   `Application\.Current\.MainWindow\s*=` (an assignment setting the property), skip
   that line — this is the recommended remediation pattern, not the antipattern.
3. **Flag** any line whose stripped text contains `Application\.Current\.MainWindow`
   (a read/dereference of the property).

### Do NOT flag

```vb
' The reference appears only in a comment
' Application.Current.MainWindow is the LoginView, not the shell.   ' NOT flagged

' Assignment — this is the recommended remediation (Prevention option 3)
Application.Current.MainWindow = _shellWindow   ' NOT flagged
```

### Flag

```vb
' Read/dereference in executable code
Dim mainWindow = TryCast(Application.Current.MainWindow?.DataContext, MainWindowViewModel)   ' FLAGGED
```

---

## Rules 1–2, 4–6, 8–11, 13, 15–18 (unchanged detectors)

These rules use direct pattern matching and have no known false-positive issues.
Apply them as documented in `LLM_Wiki/agent_wiki/` — check for the literal error
patterns, reserved keyword usage, namespace declaration format, etc.

---

## Output Format

Write findings to `Operator/debug-logs/agent-wiki-verification-report.md` using the
same format as the 2026-05-24 baseline report. For each rule, record:

- Status: ✅ PASS (0 findings) or ❌ FAIL (N findings)
- A table with columns: File | Line | Code | Details

After writing the report, compare Rule 3/7/12/14/19 counts against the
2026-05-24 baseline in
`Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-report.md`
and note the delta in a `## Count Delta` section at the end of the report.

---

## Corpus Self-Test (run before live scan)

Before scanning the live codebase, validate the detectors against the corpus in
`Operator/audit-tests/`. Any misclassification against the corpus is a detector
defect — fix the detector, not the corpus.

Run the self-test with:
```
For each rule-NN directory in Operator/audit-tests/:
  Apply the Rule NN detector to every file in known-good/ — must produce 0 findings.
  Apply the Rule NN detector to every file in known-bad/ — must produce ≥1 finding per file.
Report PASS or FAIL per rule.
```
