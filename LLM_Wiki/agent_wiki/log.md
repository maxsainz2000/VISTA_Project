# Agent Wiki — Log

Chronological record of all agent contributions to the Agent Wiki.

<!-- Append new entries at the top. Format: ## [YYYY-MM-DD] agent | action | description -->

## [2026-05-22] claude-code | extended error-fix | EF Core 10 MigrateAsync() on scratch VB.NET context also silently skips migrations (ACC-test-6) — use raw-SQL SetupScratchSchema helper instead
## [2026-05-22] claude-code | added pattern | WPF Application.Current.MainWindow points to LoginView (first window shown), not the shell — iterate Application.Current.Windows to find a window by ViewModel type
## [2026-05-20] claude-code | added error-fix | EF Core 10 VB.NET ToListAsync silently returns empty for full entity queries — use raw SqliteConnection workaround
## [2026-05-20] claude-code | added antipattern | VB.NET parameter name shadows property of same name (case-insensitive) — clears input list silently; rename parameter to avoid collision
## [2026-05-15] claude-code | added antipattern | VB.NET `cstr` variable name collides with `CStr()` conversion keyword (BC30183) — use `connStr` or any non-conversion-function name
## [2026-05-15] claude-code | added antipattern | `Console.WriteLine` resolves to `Microsoft.Extensions.Logging.Console` when MEL is imported — qualify as `System.Console.WriteLine`
## [2026-05-11] claude-code | added antipattern | WPF VB.NET `clr-namespace` in xmlns must include the RootNamespace prefix (MC3074) — discovered during ACC-14 VatPayableTile placement
## [2026-05-09] claude-code | added error-fix | EF Core 10 CLI cannot discover VB.NET migration classes — use manual migration files + DatabaseInitializer workaround
## [2026-05-04] claude-code | added antipattern | VB.NET lambda parameter name conflicts with local variable in same method causes BC36641 — use distinct names for lambda params
## [2026-05-04] claude-code | added pattern | classlib ViewModel auto-refresh: use System.Timers.Timer + captured SynchronizationContext instead of DispatcherTimer (not available in classlib)
## [2026-05-04] claude-code | added antipattern | VB.NET List(Of T).Count property shadows LINQ Count(predicate) extension — causes BC32016; use Enumerable.Count(list, predicate) instead
## [2026-05-03] claude-code | added antipattern | VB.NET leading dot on continuation lines causes BC30157 outside With blocks — move dot to end of preceding line
## [2026-05-02] claude-code | added antipattern | VB.NET loop variable named `entry` shadows inherited `DbContext.Entry()` method — use `dbEntry` instead
## [2026-05-02] claude-code | added antipattern | VB.NET RootNamespace doubles prefix when Namespace declarations use fully-qualified names — use relative suffixes only
