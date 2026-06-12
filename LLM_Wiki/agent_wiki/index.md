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
| [wpf-dynamicresource-on-condition-binding](errors/wpf-dynamicresource-on-condition-binding.md) | error-fix | Infrastructure | wpf, xaml, vb-net, triggers, reduced-motion, skeleton-loaders | claude-code | 2026-06-05 | active |
| [wpf-vsm-foreground-on-non-control-template-root](errors/wpf-vsm-foreground-on-non-control-template-root.md) | error-fix | MerchSys.App | wpf, xaml, vb-net, visual-state-manager, storyboard, listbox, datagrid, command-palette, runtime-error | claude-code | 2026-06-05 | active |
| [mariadb-raw-sql-sqlite-dialect-leakage](errors/mariadb-raw-sql-sqlite-dialect-leakage.md) | error-fix | Infrastructure | ef-core, mariadb, sqlite, vb-net, sql-dialect, post-pivot-regression, runtime-error | antigravity | 2026-05-29 | active |
| [owner-readonly-kpi-write-on-read](errors/owner-readonly-kpi-write-on-read.md) | error-fix | MerchSys.Accounting | vb-net, ef-core, role-enforcement, owasp-da5, kpi, dashboard, write-on-read, runtime-error | claude-code | 2026-06-01 | active |
| [readonly-client-blocked-by-startup-ddl-bootstrap](errors/readonly-client-blocked-by-startup-ddl-bootstrap.md) | error-fix | MerchSys.App | mariadb, schema-bootstrap, ddl, grants, least-privilege, read-only, tailscale, startup, runtime-error | claude-code | 2026-06-02 | resolved (Schema:RunBootstrap gate + scoped UPDATE grant) |
| [sslmode-none-invalid-oracle-mysql-efcore-provider](errors/sslmode-none-invalid-oracle-mysql-efcore-provider.md) | error-fix | MerchSys.App | mariadb, mysqlconnector, mysql-efcore, oracle-provider, sslmode, connection-string, enum-parse, dual-provider, runtime-error | claude-code | 2026-06-02 | resolved (SslMode=Preferred) |
| [wpf-dynamicresource-brush-into-color-property](antipatterns/wpf-dynamicresource-brush-into-color-property.md) | antipattern | MerchSys.App | wpf, xaml, theming, dynamicresource, solidcolorbrush, color, runtime-error | claude-code | 2026-06-03 | active |
| [wpf-setter-targets-clr-property-not-dependencyproperty](antipatterns/wpf-setter-targets-clr-property-not-dependencyproperty.md) | antipattern | MerchSys.App | wpf, xaml, theming, controltemplate, setter, scrollbar, track, dependencyproperty, runtime-error | claude-code | 2026-06-03 | active |
| [wpf-vista-theming-conventions](patterns/wpf-vista-theming-conventions.md) | pattern | MerchSys.App | wpf, xaml, theming, design-tokens, design-system | antigravity | 2026-06-03 | active |
| [wpf-vista-iconography](patterns/wpf-vista-iconography.md) | pattern | MerchSys.App | wpf, xaml, theming, icons, geometry, design-system, dynamicresource | claude-code | 2026-06-03 | active |
| [wpf-vista-dashboard-layout](patterns/wpf-vista-dashboard-layout.md) | pattern | MerchSys.App | wpf, xaml, dashboard, layout, overflow | antigravity | 2026-06-03 | active |
| [wpf-vista-trend-indicators](patterns/wpf-vista-trend-indicators.md) | pattern | MerchSys.App | wpf, xaml, dashboard, trend-indicators, delta-indicator, sparkline | antigravity | 2026-06-03 | active |
| [wpf-vista-purchasing-dashboard](patterns/wpf-vista-purchasing-dashboard.md) | pattern | MerchSys.Purchasing | wpf, xaml, vb-net, mvvm, purchasing, dashboard | antigravity | 2026-06-03 | active |
| [wpf-vista-state-feedback](patterns/wpf-vista-state-feedback.md) | pattern | MerchSys.App | wpf, mvvm, vb-net, state, feedback, busy, empty, error, concurrency, validation, notifications | claude-code / antigravity | 2026-06-04 (updated UX-19) | active |
| [wpf-vista-command-palette](patterns/wpf-vista-command-palette.md) | pattern | MerchSys.App | wpf, xaml, mvvm, command-palette, navigation, search, debouncing | antigravity | 2026-06-03 | active |
| [wpf-vista-keyboard-focus](patterns/wpf-vista-keyboard-focus.md) | pattern | Infrastructure | wpf, xaml, vb-net, keyboard-focus, accessibility | antigravity | 2026-06-04 | active |
| [wpf-vista-confirmation-presenter](patterns/wpf-vista-confirmation-presenter.md) | pattern | MerchSys.App | wpf, xaml, mvvm, confirmation, dialogs, danger-styling | antigravity | 2026-06-04 | active |
| [wpf-vista-formatting](patterns/wpf-vista-formatting.md) | pattern | MerchSys.App | wpf, xaml, vb-net, mvvm, formatting | antigravity | 2026-06-04 | active |
| [wpf-vista-tooltips](patterns/wpf-vista-tooltips.md) | pattern | MerchSys.App | wpf, xaml, tooltips, affordance, accessibility, design-system | claude-code | 2026-06-05 | active |
| [wpf-vista-ui-settings-persistence](patterns/wpf-vista-ui-settings-persistence.md) | pattern | MerchSys.App | wpf, vb-net, persistence, window-placement, ui-settings, json, theming | claude-code | 2026-06-05 | active |
| [wpf-vista-freshness-chip](patterns/wpf-vista-freshness-chip.md) | pattern | MerchSys.App | wpf, xaml, freshness-chip, data-freshness, RelativeTimeConverter, vb-net | antigravity | 2026-06-05 | active |
| [vbnet-nullable-trycast-value-type-compile-error](errors/vbnet-nullable-trycast-value-type-compile-error.md) | error-fix | MerchSys.App | vb-net, nullable, trycast, value-type, conversion, BC30792, build-error | antigravity | 2026-06-05 | active |
| [wpf-vista-motion](patterns/wpf-vista-motion.md) | pattern | MerchSys.App | wpf, xaml, motion, transitions, micro-interactions, reduced-motion, vb-net | antigravity | 2026-06-05 | active |
| [wpf-vista-skeleton-loaders](patterns/wpf-vista-skeleton-loaders.md) | pattern | MerchSys.App | wpf, xaml, skeleton-loaders, loading-states, shimmer, reduced-motion, vb-net | antigravity | 2026-06-05 | active |
| [wpf-vista-overlay-pattern](patterns/wpf-vista-overlay-pattern.md) | pattern | MerchSys.App | wpf, xaml, mvvm, overlay, modal, keyboard-shortcut, accessibility | antigravity | 2026-06-05 | active |
| [wpf-vista-filter-summary](patterns/wpf-vista-filter-summary.md) | pattern | MerchSys.App | wpf, xaml, vb-net, mvvm, search, filtering, chips, EmptyStatePanel | antigravity | 2026-06-05 | active |
| [wpf-vista-notification-undo](patterns/wpf-vista-notification-undo.md) | pattern | MerchSys.App | wpf, xaml, vb-net, mvvm, notifications, undo, soft-delete, closures | antigravity | 2026-06-05 | active |
| [wpf-vista-pos-fast-path](patterns/wpf-vista-pos-fast-path.md) | pattern | MerchSys.POS | wpf, xaml, keyboard-focus, fast-path, focus-discipline, mvvm | antigravity | 2026-06-05 | active |
| [wpf-vista-presentation-contracts](patterns/wpf-vista-presentation-contracts.md) | pattern | Infrastructure | vb-net, architecture, modular-monolith, sharedkernel, mvvm, presentation-contracts, boundaries | claude-code | 2026-06-05 | active |
| [wpf-vista-destructive-action-guard](patterns/wpf-vista-destructive-action-guard.md) | pattern | MerchSys.POS | wpf, vb-net, mvvm, user-experience, confirmation, undo, guardrail | antigravity | 2026-06-05 | active |
| [wpf-vista-accessibility](patterns/wpf-vista-accessibility.md) | pattern | Infrastructure | wpf, xaml, accessibility, wcag, mvvm, vb-net | antigravity | 2026-06-06 | active |
| [wpf-vista-personalization](patterns/wpf-vista-personalization.md) | pattern | MerchSys.App | wpf, vb-net, persistence, personalization, favorites, recents, navigation, command-palette, ui-settings, json | claude-code | 2026-06-06 | active |
| [wpf-vista-performance](patterns/wpf-vista-performance.md) | pattern | MerchSys.App | wpf, xaml, vb-net, virtualization, async-load, optimistic-ui, performance, debouncing | claude-code | 2026-06-06 | active |
| [wpf-vista-interactive-charts](patterns/wpf-vista-interactive-charts.md) | pattern | MerchSys.App | wpf, xaml, vb-net, mvvm, charts, tooltips, drill-down, period-selector, sparkline, interactive | claude-code | 2026-06-06 | active |
| [wpf-vista-print-export](patterns/wpf-vista-print-export.md) | pattern | MerchSys.App | wpf, xaml, vb-net, print, export, flowdocument, csv, bir, official-receipt, accounting | claude-code | 2026-06-07 | active |
| [wpf-vista-design-gallery](patterns/wpf-vista-design-gallery.md) | pattern | MerchSys.App | wpf, xaml, design-system, design-tokens, gallery, realization-check, developer-tools | claude-code | 2026-06-07 | active |
| [mariadb-keyset-pagination](patterns/mariadb-keyset-pagination.md) | pattern | Infrastructure | mariadb, mysqlconnector, vb-net, pagination, keyset, performance, raw-reader, viewmodel | claude-code | 2026-06-10 | active |
| [wpf-vista-comparative-report](patterns/wpf-vista-comparative-report.md) | pattern | MerchSys.Accounting | wpf, xaml, vb-net, mvvm, comparative-reports, delta-indicator, reporting | antigravity | 2026-06-10 | active |
| [wpf-vista-insight-banner](patterns/wpf-vista-insight-banner.md) | pattern | MerchSys.App | wpf, xaml, vb-net, mvvm, theming, design-tokens, wcag, accessibility, severity-banner, shared-kernel | antigravity | 2026-06-10 | active |
| [immutable-receipt-post-save-mutation](errors/immutable-receipt-post-save-mutation.md) | error-fix | MerchSys.POS | ef-core, vb-net, mariadb, interceptor, immutability, official-receipt, decorator, shared-dbcontext, runtime-error | claude-code | 2026-06-11 | active |
| [schema-drift-from-editing-applied-migration](errors/schema-drift-from-editing-applied-migration.md) | error-fix | Infrastructure | mariadb, schema-migration, schema-drift, sha256, startup, ddl, idempotent-migrations, runtime-error | claude-code | 2026-06-11 | active |
| [wpf-vista-animated-scene](patterns/wpf-vista-animated-scene.md) | pattern | MerchSys.App | wpf, xaml, vb-net, animation, visual-state-manager, animation-clock, lifecycle, reduced-motion, login, delight | claude-code | 2026-06-12 | active |


