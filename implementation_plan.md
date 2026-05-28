# Implementation Plan: Centralized MariaDB Migration & Sidebar Refactor (INFRA-23 to INFRA-30)

This plan implements the authoritative 2026-05-28 architecture pivot for the **VISTA (Villon Integrated Supply and Trade Application)** system. It transitions VISTA from an offline-first SQLite-based architecture with a custom sync layer to a pure, centralized, and highly concurrent client-server architecture built against a single **MariaDB 11.4.x LTS** instance. Additionally, it restructures the flat sidebar navigation into a modern, low-cognitive-load **Master-Detail Activity Rail Sidebar**.

---

## User Review Required

> [!IMPORTANT]
> **Primary Decision — Oracle's Official EF Core 10 MySQL Provider**
> We have researched and verified that Oracle's official provider `MySql.EntityFrameworkCore` version `10.0.7` is fully available and compatible with .NET 10/EF Core 10. We will use this provider to switch all module DbContexts to MariaDB, bypassing the Pomelo 9 binary incompatibility with EF Core 10.

> [!WARNING]
> **Strict Continuity Plan**
> A network or host outage halts all mutating actions in client applications. Reconnection retries and visual indicators are added to manage these scenarios. A manual 30-minute recovery runbook is provided.

> [!IMPORTANT]
> **MediatR inter-module boundaries**
> Module boundaries remain strict. MediatR is still the exclusive vehicle for inter-module queries/events. `DbContext` separation remains intact at the schema level via module table prefixes (`Pur_`, `Inv_`, `Pos_`, `Acc_`).

---

## Open Questions

None. The architectural specifications are completely explicit in the plan files and the May 28 amendment.

---

## Proposed Changes

### Component 1: EF Core MariaDB Provider & Spike (INFRA-23)

We will evaluate the providers and pin `MySql.EntityFrameworkCore` 10.0.7.

#### [NEW] [provider-evaluation.md](file:///c:/Users/Admin/Documents/VISTA_Project/Plans/VISTA_Modules/Infrastructure/23/provider-evaluation.md)
#### [NEW] [MerchSys.ProviderSpike.vbproj](file:///c:/Users/Admin/Documents/VISTA_Project/Plans/VISTA_Modules/Infrastructure/23/spike/MerchSys.ProviderSpike.vbproj)
#### [NEW] [SpikeContext.vb](file:///c:/Users/Admin/Documents/VISTA_Project/Plans/VISTA_Modules/Infrastructure/23/spike/SpikeContext.vb)
#### [NEW] [Program.vb](file:///c:/Users/Admin/Documents/VISTA_Project/Plans/VISTA_Modules/Infrastructure/23/spike/Program.vb)
#### [NEW] [INFRA-23-summary.md](file:///c:/Users/Admin/Documents/VISTA_Project/Progress/VISTA_Modules/Infrastructure/INFRA-23-summary.md)

- Implement a disposable VB.NET spike project to verify Oracle's EF Core 10 provider works against the live local MariaDB database.
- Smoke-test basic CRUD, LINQ queries, `ToListAsync()`, transaction blocks, and `SELECT ... FOR UPDATE`.
- Document findings and update the central agent wiki.

---

### Component 2: MariaDB Schema Bootstrap (INFRA-24)

Replace the SQLite `DatabaseInitializer` with a raw-connection startup-time MariaDB bootstrap.

#### [NEW] [0001_initial_schema.sql](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Infrastructure/Data/Migrations/Central/0001_initial_schema.sql)
#### [NEW] [0002_seed_reference_data.sql](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Infrastructure/Data/Migrations/Central/0002_seed_reference_data.sql)
#### [NEW] [README.md](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Infrastructure/Data/Migrations/Central/README.md)
#### [NEW] [MariaDbSchemaInitializer.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Data/MariaDbSchemaInitializer.vb)
#### [MODIFY] [App.xaml.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/App.xaml.vb)
#### [MODIFY] [MerchSys.App.vbproj](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/MerchSys.App.vbproj)
#### [NEW] [INFRA-24-summary.md](file:///c:/Users/Admin/Documents/VISTA_Project/Progress/VISTA_Modules/Infrastructure/INFRA-24-summary.md)

- Translate the entire SQLite DDL from `DatabaseInitializer.vb` into MariaDB syntax (`ENGINE=InnoDB`, `DECIMAL(18, 4)`, `TINYINT(1)`, `RowVersion TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6)`).
- Create a metadata `__SchemaMigrations` table to track applied scripts and their SHA-256 hashes.
- Write `MariaDbSchemaInitializer.vb` using raw `MySqlConnector` to run embedded `.sql` scripts lexicographically. It aborts startup if script contents are tampered with (Sha256 mismatch).

---

### Component 3: DbContext Conversion (INFRA-25)

Convert all module DbContexts from SQLite to Oracle's MariaDB provider.

#### [MODIFY] [PurchasingDbContext.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Data/PurchasingDbContext.vb)
#### [MODIFY] [InventoryDbContext.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Data/InventoryDbContext.vb)
#### [MODIFY] [PosDbContext.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Data/PosDbContext.vb)
#### [MODIFY] [AccountingDbContext.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Data/AccountingDbContext.vb)
#### [MODIFY] [PurchasingDbContextFactory.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Data/PurchasingDbContextFactory.vb)
#### [MODIFY] [InventoryDbContextFactory.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Data/InventoryDbContextFactory.vb)
#### [MODIFY] [PosDbContextFactory.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Data/PosDbContextFactory.vb)
#### [MODIFY] [AccountingDbContextFactory.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Data/AccountingDbContextFactory.vb)
#### [MODIFY] [PurchasingRegistration.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Startup/PurchasingRegistration.vb)
#### [MODIFY] [InventoryRegistration.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Startup/InventoryRegistration.vb)
#### [MODIFY] [PosRegistration.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Startup/PosRegistration.vb)
#### [MODIFY] [AccountingRegistration.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Startup/AccountingRegistration.vb)
#### [MODIFY] [appsettings.json](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/appsettings.json)
#### [MODIFY] [SyncConfig.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Startup/SyncConfig.vb)
#### [MODIFY] [MerchSys.Purchasing.vbproj](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/MerchSys.Purchasing.vbproj)
#### [MODIFY] [MerchSys.Inventory.vbproj](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/MerchSys.Inventory.vbproj)
#### [MODIFY] [MerchSys.POS.vbproj](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/MerchSys.POS.vbproj)
#### [MODIFY] [MerchSys.Accounting.vbproj](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/MerchSys.Accounting.vbproj)
#### [MODIFY] [SyncableRepositoryCore.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Persistence/SyncableRepositoryCore.vb)
#### [NEW] [INFRA-25-summary.md](file:///c:/Users/Admin/Documents/VISTA_Project/Progress/VISTA_Modules/Infrastructure/INFRA-25-summary.md)

- Add NuGet package `MySql.EntityFrameworkCore` version `10.0.7` to all 4 module libraries and remove `Microsoft.EntityFrameworkCore.Sqlite`.
- Repoint all DbContexts and design-time factories to use MariaDB.
- Comment out the `SyncWorker` hosted service registration in `SyncConfig.vb` to disable sync behavior.
- Temporarily modify `SyncableRepositoryCore.SaveChangesWithJournalAsync` to act as a pass-through to `SaveChangesAsync()` to prevent compile/runtime crashes until INFRA-27 deletes the sync layer.

---

### Component 4: Concurrency Tokens & Pessimistic Locks (INFRA-26)

Wire optimistic concurrency tokens and pessimistic row locks on critical inventory paths.

#### [NEW] [ConcurrencyAwareEntity.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Entities/ConcurrencyAwareEntity.vb)
#### [MODIFY] [AuditableEntity.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Entities/AuditableEntity.vb)
#### [MODIFY] [Entity Configurations in all modules] (e.g., `StockBatchConfiguration.vb` etc.)
#### [MODIFY] [SaleCompletedHandler.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Handlers/SaleCompletedHandler.vb)
#### [MODIFY] [ShrinkageHandler.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Handlers/ShrinkageHandler.vb)
#### [MODIFY] [BaseViewModel.vb or helper class]
#### [NEW] [INFRA-26-summary.md](file:///c:/Users/Admin/Documents/VISTA_Project/Progress/VISTA_Modules/Infrastructure/INFRA-26-summary.md)

- Implement `ConcurrencyAwareEntity` base class containing `RowVersion` bytes/timestamp, and map it with `.IsRowVersion()` in mutable entities' configurations.
- Rewrite FIFO inventory decrement logic (`DecrementFifoAsync`) to wrap in a transaction and run a raw `SELECT ... FOR UPDATE` SQL query to serialize concurrent batch writes.
- Introduce base ViewModel helper `ExecuteWithConcurrencyRetryAsync` to catch `DbUpdateConcurrencyException`, trigger a UI warning toast, reload fresh state, and prompt a retry.

---

### Component 5: Decommission Sync Layer & Delete SQLite (INFRA-27)

Tear out all obsolete SQLite and Sync Layer scaffolding from the repository.

#### [DELETE] Obsolete SQLite & Sync Files (40+ files)
- Delete all files in `MerchSys.SharedKernel/Sync/` and `MerchSys.SharedKernel/Persistence/ISyncableRepository.vb`, `SyncableRepositoryCore.vb`.
- Delete `MerchSys.App/Services/SyncOrchestrator.vb`, `SyncWorker.vb`, `MariaDbSyncContext.vb`, `MariaDbSyncTransmitter.vb`, etc.
- Delete obsolete SQLite migration folders (`Migrations/` in each module) and `DatabaseInitializer.vb` / `DatabaseConfig.vb`.
#### [MODIFY] Module handlers calling `SaveChangesWithJournalAsync` (refactored to `SaveChangesAsync`)
#### [MODIFY] [appsettings.json](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/appsettings.json)
#### [MODIFY] [CLAUDE.md](file:///c:/Users/Admin/Documents/VISTA_Project/CLAUDE.md)
#### [MODIFY] Wiki and checklist files
#### [NEW] [INFRA-27-summary.md](file:///c:/Users/Admin/Documents/VISTA_Project/Progress/VISTA_Modules/Infrastructure/INFRA-27-summary.md)

- Perform clean deletion of all files related to SQLite DB paths, Sync Journals, Outboxes, conflict resolution, sync UI, and old migrations.
- Refactor all module handlers to use normal entity framework context writes (`_db.SaveChangesAsync()`).
- Clean up configuration blocks and project reference sheets.

---

### Component 6: Connection Status UI & Client Configuration (INFRA-28)

Provide continuous monitoring and user-facing badge of LAN connectivity.

#### [NEW] [ConnectionHealthMonitor.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Services/ConnectionHealthMonitor.vb)
#### [NEW] [IConnectionHealthMonitor.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Services/IConnectionHealthMonitor.vb)
#### [NEW] [ConnectionStatusViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/ViewModels/Shell/ConnectionStatusViewModel.vb)
#### [NEW] [ConnectionStatusIndicator.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/ConnectionStatusIndicator.xaml)
#### [NEW] [ConnectionStatusIndicator.xaml.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/ConnectionStatusIndicator.xaml.vb)
#### [NEW] [ConnectionConfig.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Startup/ConnectionConfig.vb)
#### [NEW] [DisableOnOfflineBehavior.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Behaviors/DisableOnOfflineBehavior.vb)
#### [NEW] [appsettings.Example.json](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/appsettings.Example.json)
#### [MODIFY] [MainWindow.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/MainWindow.xaml)
#### [NEW] [INFRA-28-summary.md](file:///c:/Users/Admin/Documents/VISTA_Project/Progress/VISTA_Modules/Infrastructure/INFRA-28-summary.md)

- Implement periodic DB health-ping monitor backing a visual badge (Online, Reconnecting, Offline) with exponential backoff on retries.
- Attach WPF `DisableOnOfflineBehavior` to all data-mutating buttons ("Save PO", "Issue OR", etc.) to disable them when disconnected.
- Create `appsettings.Example.json` specifying workstations and connection configurations.

---

### Component 7: Operational Runbook & Backups (INFRA-29)

Generate full deploy instructions, nightly backup automation, and failover runbooks.

#### [NEW] [01-host-laptop-setup.md](file:///c:/Users/Admin/Documents/VISTA_Project/Plans/VISTA_Modules/Infrastructure/runbooks/01-host-laptop-setup.md)
#### [NEW] [02-client-laptop-setup.md](file:///c:/Users/Admin/Documents/VISTA_Project/Plans/VISTA_Modules/Infrastructure/runbooks/02-client-laptop-setup.md)
#### [NEW] [03-nightly-backup.md](file:///c:/Users/Admin/Documents/VISTA_Project/Plans/VISTA_Modules/Infrastructure/runbooks/03-nightly-backup.md)
#### [NEW] [04-host-failover.md](file:///c:/Users/Admin/Documents/VISTA_Project/Plans/VISTA_Modules/Infrastructure/runbooks/04-host-failover.md)
#### [NEW] [backup-mysqldump.ps1](file:///c:/Users/Admin/Documents/VISTA_Project/Plans/VISTA_Modules/Infrastructure/runbooks/scripts/backup-mysqldump.ps1)
#### [NEW] [vista-nightly-backup.xml](file:///c:/Users/Admin/Documents/VISTA_Project/Plans/VISTA_Modules/Infrastructure/runbooks/scripts/vista-nightly-backup.xml)
#### [NEW] [INFRA-29-summary.md](file:///c:/Users/Admin/Documents/VISTA_Project/Progress/VISTA_Modules/Infrastructure/INFRA-29-summary.md)

- Create professional markdown runbooks detailing system setup, Windows firewall settings, XAMPP config, LAN IP reservations, and UPS configurations.
- Write PowerShell backup script featuring compression, daily/weekly/monthly file rotation, and size safety verification.
- Author a 30-minute RTO database failover runbook checklist.

---

### Component 8: Master-Detail Activity Rail Sidebar (INFRA-30)

Redesign sidebar navigation to a space-efficient Master-Detail Activity Rail layout.

#### [MODIFY] [MainWindow.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/MainWindow.xaml)
#### [NEW] [ActivityRail.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/ActivityRail.xaml)
#### [NEW] [ActivityRail.xaml.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/ActivityRail.xaml.vb)
#### [NEW] [ModuleDetailPanel.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/ModuleDetailPanel.xaml)
#### [NEW] [ModuleDetailPanel.xaml.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/ModuleDetailPanel.xaml.vb)
#### [NEW] [PurchasingPanel.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/Modules/PurchasingPanel.xaml)
#### [NEW] [InventoryPanel.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/Modules/InventoryPanel.xaml)
#### [NEW] [PosPanel.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/Modules/PosPanel.xaml)
#### [NEW] [AccountingPanel.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/Modules/AccountingPanel.xaml)
#### [NEW] [DeveloperToolsPanel.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/Modules/DeveloperToolsPanel.xaml)
#### [MODIFY] [MainWindowViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/ViewModels/MainWindowViewModel.vb)
#### [NEW] [ActivityRailViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/ViewModels/Shell/ActivityRailViewModel.vb)
#### [NEW] [INFRA-30-summary.md](file:///c:/Users/Admin/Documents/VISTA_Project/Progress/VISTA_Modules/Infrastructure/INFRA-30-summary.md)

- Implement a 60px Activity Rail containing 4 module icons (Purchasing, Inventory, POS, Accounting) and a 5th debug-only icon (Developer Tools).
- Create a 220px detail panel containing sub-view selectors, replacing the flat list of 22 items.
- Support key bindings (`Ctrl+1` through `Ctrl+4`, `Ctrl+0`) for rapid switching.
- Incorporate role-aware restrictions, ensuring the Owner only accesses read-only KPI views.

---

## Verification Plan

### Automated Tests
- Run `dotnet build WPF_Applications/MerchSys/MerchSys.slnx` and verify **0 errors, 0 warnings**.
- Run the disposable provider spike executable `MerchSys.ProviderSpike.exe` and confirm all test scenarios return `OK`.

### Manual Verification
1. App launch executes `MariaDbSchemaInitializer` automatically. Verification: tables exist on the local MariaDB, seeding (20 products, 4 categories, 3 vendors) succeeds.
2. Tamper verification: alter a byte in `0001_initial_schema.sql` and run VISTA. Verification: application crashes cleanly at startup with schema drift message.
3. Concurrency verification: launch two instances of VISTA. Initiate a PO update on both simultaneously. Verification: first succeeds, second crashes gracefully showing the refresh notification.
4. Outage verification: stop MySQL service. Verification: indicator goes Reconnecting -> Offline, mutating buttons disable. Restart MySQL service and click retry: indicator goes Online, buttons re-enable.
5. Navigation verification: click rail icons and use key bindings (`Ctrl+1` to `Ctrl+4`). Verification: panel details switch, landing views match user roles.
