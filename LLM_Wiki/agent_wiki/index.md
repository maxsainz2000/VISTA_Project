# Agent Wiki — Index

All engineering learnings from debugging and error-fixing sessions. Check this index before debugging — the fix may already be documented.

## How to Use

1. **Before debugging:** Scan the table below for your error code, module, or tags.
2. **After fixing a bug:** Add an entry to the appropriate directory (`errors/`, `patterns/`, `antipatterns/`) using the template in `_templates/`, then add a row here.

## Entries

| Entry | Type | Module | Tags | Agent | Date | Status |
|---|---|---|---|---|---|---|
| [mariadb-pure-client-server-architecture](patterns/mariadb-pure-client-server-architecture.md) | pattern | Infrastructure | mariadb, ef-core, client-server, concurrency, architecture | claude-code | 2026-05-28 | **authoritative** |
| [efcore10-vbnet-migration-discovery-bug](errors/efcore10-vbnet-migration-discovery-bug.md) | error-fix | Infrastructure | ef-core, vb-net, migrations, sqlite, mariadb | claude-code | 2026-05-09 | partially-historical (post-pivot note added) |
| [vbnet-rootnamespace-relative-declarations](patterns/vbnet-rootnamespace-relative-declarations.md) | antipattern | Infrastructure | vb-net, namespace, build-error | claude-code | 2026-05-02 | active |
| [vbnet-loop-variable-shadows-dbcontext-method](antipatterns/vbnet-loop-variable-shadows-dbcontext-method.md) | antipattern | Infrastructure | vb-net, ef-core, dbcontext, build-error | claude-code | 2026-05-02 | active |
| [vbnet-leading-dot-fluent-chains](antipatterns/vbnet-leading-dot-fluent-chains.md) | antipattern | Infrastructure | vb-net, fluent-api, ef-core, build-error | claude-code | 2026-05-03 | active |
| [vbnet-list-count-property-shadows-linq-extension](antipatterns/vbnet-list-count-property-shadows-linq-extension.md) | antipattern | MerchSys.Inventory | vb-net, linq, list, BC32016, build-error | claude-code | 2026-05-04 | active |
| [vbnet-lambda-param-shadows-local-variable](antipatterns/vbnet-lambda-param-shadows-local-variable.md) | antipattern | MerchSys.Purchasing | vb-net, lambda, BC36641, build-error | claude-code | 2026-05-04 | active |
| [classlib-viewmodel-auto-refresh-timer](patterns/classlib-viewmodel-auto-refresh-timer.md) | pattern | MerchSys.Inventory | wpf, mvvm, viewmodel, classlib, timer, threading, vb-net | claude-code | 2026-05-04 | active |
| [vbnet-reserved-keyword-enum-member](errors/vbnet-reserved-keyword-enum-member.md) | error-fix | MerchSys.Inventory | vb-net, enum, reserved-keyword, BC31001, BC30201, build-error | claude-code | 2026-05-09 | active |
| [vbnet-err-builtin-shadows-loop-variable](antipatterns/vbnet-err-builtin-shadows-loop-variable.md) | antipattern | MerchSys.POS | vb-net, reserved-keyword, for-each, BC30068, BC30311, build-error | claude-code | 2026-05-11 | active |
| [vbnet-cstr-keyword-collision](antipatterns/vbnet-cstr-keyword-collision.md) | antipattern | MerchSys.App | vb-net, reserved-keyword, BC30183, build-error | claude-code | 2026-05-15 | active |
| [vbnet-console-namespace-shadow](antipatterns/vbnet-console-namespace-shadow.md) | antipattern | MerchSys.App | vb-net, namespace, BC30456, imports, build-error | claude-code | 2026-05-15 | active |
| [efcore-hasdefaultvalue-enum-type-mismatch](errors/efcore-hasdefaultvalue-enum-type-mismatch.md) | error-fix | MerchSys.Purchasing | ef-core, enum, vb-net, runtime-error, configuration | claude-code | 2026-05-20 | active |
| [efcore-vbnet-tolistasync-entity-empty](errors/efcore-vbnet-tolistasync-entity-empty.md) | error-fix | MerchSys.Purchasing | ef-core, vb-net, sqlite, mariadb, runtime-error, materialization | claude-code | 2026-05-20 | partially-historical (post-pivot note added) |
| [vbnet-parameter-shadows-property](antipatterns/vbnet-parameter-shadows-property.md) | antipattern | MerchSys.Purchasing | vb-net, case-insensitive, parameter, property, shadowing, logic-bug | claude-code | 2026-05-20 | active |
| [wpf-mainwindow-not-shell-window](patterns/wpf-mainwindow-not-shell-window.md) | pattern | MerchSys.App | wpf, navigation, login, mainwindow, runtime-bug | claude-code | 2026-05-22 | active |
| [sync-transmit-delete-no-payload](patterns/sync-transmit-delete-no-payload.md) | antipattern | Infrastructure | sync, mariadb, sqlite, json, transmitter, delete, payload | claude-code | 2026-05-23 | **historical** (sync layer removed) |
| [efcore-temp-key-sync-journal-payload](errors/efcore-temp-key-sync-journal-payload.md) | error-fix | MerchSys.SharedKernel | ef-core, sync, journal, temp-key, insert, duplicate-key, idempotency, vb-net | claude-code | 2026-05-23 | **historical** (sync layer removed) |
| [sqlite-trigger-no-temp-reference](errors/sqlite-trigger-no-temp-reference.md) | error-fix | MerchSys.App | sqlite, triggers, temp-table, BC30456, runtime-error | claude-code | 2026-05-15 | **historical** (SQLite removed) |
| [mysqlconnector-tinyint1-boolean-cint-vbnet](errors/mysqlconnector-tinyint1-boolean-cint-vbnet.md) | error-fix | MerchSys.App | mariadb, mysqlconnector, tinyint, boolean, vb-net, authentication, logic-bug | claude-code | 2026-05-28 | active |
| [efcore-inherited-rowversion-unmapped-column](errors/efcore-inherited-rowversion-unmapped-column.md) | error-fix | MerchSys.Purchasing | ef-core, mariadb, vb-net, concurrency, rowversion, inheritance, runtime-error | claude-code | 2026-05-29 | active |
| [efcore-softdelete-unique-key-collision](errors/efcore-softdelete-unique-key-collision.md) | error-fix | Infrastructure | ef-core, mariadb, vb-net, soft-delete, unique-key, duplicate-key, runtime-error | Truly Agentic AI | 2026-05-29 | active |
| [efcore-vat-ledger-columns-missing-central-schema](errors/efcore-vat-ledger-columns-missing-central-schema.md) | error-fix | Infrastructure | ef-core, mariadb, vb-net, vat, schema-migration, post-pivot-regression, runtime-error | claude-code | 2026-05-29 | active |
| [wpf-datagrid-binds-vm-rowitem-missing-property-blank-cell](errors/wpf-datagrid-binds-vm-rowitem-missing-property-blank-cell.md) | error-fix | MerchSys.Inventory | wpf, mvvm, data-binding, datagrid, viewmodel, dto, silent-bug, vb-net | claude-code | 2026-05-29 | active |
| [mariadb-raw-sql-sqlite-dialect-leakage](errors/mariadb-raw-sql-sqlite-dialect-leakage.md) | error-fix | Infrastructure | ef-core, mariadb, sqlite, vb-net, sql-dialect, post-pivot-regression, runtime-error | antigravity | 2026-05-29 | active |
| [owner-readonly-kpi-write-on-read](errors/owner-readonly-kpi-write-on-read.md) | error-fix | MerchSys.Accounting | vb-net, ef-core, role-enforcement, owasp-da5, kpi, dashboard, write-on-read, runtime-error | claude-code | 2026-06-01 | active |
| [readonly-client-blocked-by-startup-ddl-bootstrap](errors/readonly-client-blocked-by-startup-ddl-bootstrap.md) | error-fix | MerchSys.App | mariadb, schema-bootstrap, ddl, grants, least-privilege, read-only, tailscale, startup, runtime-error | claude-code | 2026-06-02 | resolved (Schema:RunBootstrap gate + scoped UPDATE grant) |
| [sslmode-none-invalid-oracle-mysql-efcore-provider](errors/sslmode-none-invalid-oracle-mysql-efcore-provider.md) | error-fix | MerchSys.App | mariadb, mysqlconnector, mysql-efcore, oracle-provider, sslmode, connection-string, enum-parse, dual-provider, runtime-error | claude-code | 2026-06-02 | resolved (SslMode=Preferred) |
| [wpf-dynamicresource-brush-into-color-property](antipatterns/wpf-dynamicresource-brush-into-color-property.md) | antipattern | MerchSys.App | wpf, xaml, theming, dynamicresource, solidcolorbrush, color, runtime-error | claude-code | 2026-06-03 | active |
| [wpf-setter-targets-clr-property-not-dependencyproperty](antipatterns/wpf-setter-targets-clr-property-not-dependencyproperty.md) | antipattern | MerchSys.App | wpf, xaml, theming, controltemplate, setter, scrollbar, track, dependencyproperty, runtime-error | claude-code | 2026-06-03 | active |
| [wpf-vista-theming-conventions](patterns/wpf-vista-theming-conventions.md) | pattern | MerchSys.App | wpf, xaml, theming, design-tokens, design-system | antigravity | 2026-06-03 | active |
| [wpf-vista-iconography](patterns/wpf-vista-iconography.md) | pattern | MerchSys.App | wpf, xaml, theming, icons, geometry, design-system, dynamicresource | claude-code | 2026-06-03 | active |
