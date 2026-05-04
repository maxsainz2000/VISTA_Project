# Agent Wiki — Log

Chronological record of all agent contributions to the Agent Wiki.

<!-- Append new entries at the top. Format: ## [YYYY-MM-DD] agent | action | description -->

## [2026-05-04] claude-code | added antipattern | VB.NET lambda parameter name conflicts with local variable in same method causes BC36641 — use distinct names for lambda params
## [2026-05-04] claude-code | added pattern | classlib ViewModel auto-refresh: use System.Timers.Timer + captured SynchronizationContext instead of DispatcherTimer (not available in classlib)
## [2026-05-04] claude-code | added antipattern | VB.NET List(Of T).Count property shadows LINQ Count(predicate) extension — causes BC32016; use Enumerable.Count(list, predicate) instead
## [2026-05-03] claude-code | added antipattern | VB.NET leading dot on continuation lines causes BC30157 outside With blocks — move dot to end of preceding line
## [2026-05-02] claude-code | added antipattern | VB.NET loop variable named `entry` shadows inherited `DbContext.Entry()` method — use `dbEntry` instead
## [2026-05-02] claude-code | added antipattern | VB.NET RootNamespace doubles prefix when Namespace declarations use fully-qualified names — use relative suffixes only
