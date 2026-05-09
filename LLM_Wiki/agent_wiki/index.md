# Agent Wiki — Index

All engineering learnings from debugging and error-fixing sessions. Check this index before debugging — the fix may already be documented.

## How to Use

1. **Before debugging:** Scan the table below for your error code, module, or tags.
2. **After fixing a bug:** Add an entry to the appropriate directory (`errors/`, `patterns/`, `antipatterns/`) using the template in `_templates/`, then add a row here.

## Entries

| Entry | Type | Module | Tags | Agent | Date |
|---|---|---|---|---|---|
| [efcore10-vbnet-migration-discovery-bug](errors/efcore10-vbnet-migration-discovery-bug.md) | error-fix | Infrastructure | ef-core, vb-net, migrations, sqlite | claude-code | 2026-05-09 |
| [vbnet-rootnamespace-relative-declarations](patterns/vbnet-rootnamespace-relative-declarations.md) | antipattern | Infrastructure | vb-net, namespace, build-error | claude-code | 2026-05-02 |
| [vbnet-loop-variable-shadows-dbcontext-method](antipatterns/vbnet-loop-variable-shadows-dbcontext-method.md) | antipattern | Infrastructure | vb-net, ef-core, dbcontext, build-error | claude-code | 2026-05-02 |
| [vbnet-leading-dot-fluent-chains](antipatterns/vbnet-leading-dot-fluent-chains.md) | antipattern | Infrastructure | vb-net, fluent-api, ef-core, build-error | claude-code | 2026-05-03 |
| [vbnet-list-count-property-shadows-linq-extension](antipatterns/vbnet-list-count-property-shadows-linq-extension.md) | antipattern | MerchSys.Inventory | vb-net, linq, list, BC32016, build-error | claude-code | 2026-05-04 |
| [vbnet-lambda-param-shadows-local-variable](antipatterns/vbnet-lambda-param-shadows-local-variable.md) | antipattern | MerchSys.Purchasing | vb-net, lambda, BC36641, build-error | claude-code | 2026-05-04 |
| [classlib-viewmodel-auto-refresh-timer](patterns/classlib-viewmodel-auto-refresh-timer.md) | pattern | MerchSys.Inventory | wpf, mvvm, viewmodel, classlib, timer, threading, vb-net | claude-code | 2026-05-04 |
