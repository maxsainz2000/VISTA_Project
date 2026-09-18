# POS Specification

## Feature: POS-01

### Overview
Implemented all six POS domain entity classes as specified in POS-01. Each entity inherits from the appropriate SharedKernel base class and carries XML doc comments on all public members.

### Requirements
- **MerchSys.POS/Entities/SalesTransaction.vb**: core transaction entity inheriting `SoftDeletableEntity`; holds payment method, amounts, void flag, and navigation to lines/receipt/credit account
- **MerchSys.POS/Entities/SalesTransactionLine.vb**: per-product line inheriting `AuditableEntity`; denormalizes product name and unit price at time of sale
- **MerchSys.POS/Entities/OfficialReceipt.vb**: BIR-compliant receipt inheriting `AuditableEntity`; OR-YYYY-XXXX numbering documented in XML summary; stores serialized line items for reprint
- **MerchSys.POS/Entities/CreditAccount.vb**: informal credit (utang) account inheriting `SoftDeletableEntity`; `IsBlocked` is the hard-blocking field (maintained as `CurrentBalance > 0`); XML comment states the non-negotiable rule
- **MerchSys.POS/Entities/CreditPayment.vb**: payment record inheriting `AuditableEntity`; `PaymentMethod` XML comment explicitly prohibits `Credit` as a payment method
- **MerchSys.POS/Entities/SalesReturn.vb**: return record inheriting `AuditableEntity`; `Reason` is required; `IsRestocked` signals service layer to restore inventory

## Feature: POS-02

### Overview
Configured `POSDbContext` with all six EF Core entity configurations, `Pos_` table prefixes, precision/index settings, and seed data for three sample credit accounts.

### Requirements
- **Data/POSDbContext.vb**: added six `DbSet` properties (SalesTransactions, SalesTransactionLines, OfficialReceipts, CreditAccounts, CreditPayments, SalesReturns)
- **Data/Configurations/SalesTransactionConfiguration.vb**: `Pos_SalesTransactions` table; TransactionNumber required max 20 unique; TotalAmount and monetary columns precision(18,2); index on TransactionDate; one-to-many Lines (cascade), one-to-one Receipt, optional shadow FK CreditAccountId
- **Data/Configurations/SalesTransactionLineConfiguration.vb**: `Pos_SalesTransactionLines` table; ProductName required max 200; decimal columns precision(18,2)
- **Data/Configurations/OfficialReceiptConfiguration.vb**: `Pos_OfficialReceipts` table; ReceiptNumber required max 20 unique; decimal columns precision(18,2)
- **Data/Configurations/CreditAccountConfiguration.vb**: `Pos_CreditAccounts` table; CustomerName required max 200; decimal columns precision(18,2); index on IsBlocked; seeds three sample accounts
- **Data/Configurations/CreditPaymentConfiguration.vb**: `Pos_CreditPayments` table; PaymentAmount precision(18,2); CreditAccountId FK required
- **Data/Configurations/SalesReturnConfiguration.vb**: `Pos_SalesReturns` table; Reason required max 500; OriginalTransactionId FK required; decimal columns precision(18,2)
- **Data/SeedData/POSSeedData.vb**: module providing three `CreditAccount` seed instances (Juan Dela Cruz, Maria Santos, Pedro Reyes)

## Feature: POS-03

### Overview
Implemented the cart-based transaction service for the POS module: in-memory cart management, total recalculation with VAT, and finalization into persisted `SalesTransaction` records.

### Requirements
- **MerchSys.POS/Services/ICartService.vb**: defines `CartLineDto`, `CartDto` (in-memory DTOs), and `ICartService` interface with all eight method signatures
- **MerchSys.POS/Services/CartService.vb**: full implementation: shared in-memory `ConcurrentDictionary` cart store, VAT calculation from app settings, TX-YYYY-XXXX number generation, credit customer blocked-status validation, transaction finalization, void, and history query

## Feature: POS-04

### Overview
Implemented `IPaymentService` and `PaymentService` for POS-04 Payment Processing. The service handles all four payment methods (Cash, GCash, BankTransfer, Credit), updates credit account state for credit purchases, and publishes `SaleCompletedEvent` via `IEventBus` after successful payment.

### Requirements
- **MerchSys.POS/Services/IPaymentService.vb**: defines `IPaymentService` interface with `ProcessPaymentAsync` signature and `PaymentResultDto` class (`Success`, `TransactionId`, `ChangeAmount`, `ReceiptNumber`, `ErrorMessage`)
- **MerchSys.POS/Services/PaymentService.vb**: concrete implementation; loads transaction + lines + credit account via EF Core Include, validates per-method rules, updates `CreditAccount.CurrentBalance` / `IsBlocked` / `TotalCreditExtended` / `LastTransactionDate` for Credit payments, publishes `SaleCompletedEvent`, returns `PaymentResultDto`

## Feature: POS-05

### Overview
Implemented the Credit (Utang) System for the POS module — digital credit accounts, balance tracking, payment recording, and the non-negotiable zero-tolerance hard blocking rule. Based on plan POS-05.

### Requirements
- **Services/ICreditService.vb**: interface with 10 methods covering account CRUD, credit extension check, charge, payment recording, history, totals, and overdue accounts
- **Services/CreditService.vb**: implementation including `CreditBlockedException`, all ICreditService methods, and `CreditPaymentEvent` publishing via IEventBus

## Feature: POS-06

### Overview
Implemented BIR-compliant official receipt generation for the POS module. Covers OR-YYYY-XXXX sequential numbering, all BIR-mandated fields, VAT-inclusive breakdown, and 40-character thermal print formatting.

### Requirements
- **Services/IReceiptService.vb**: interface with `GenerateReceiptAsync`, `GetReceiptAsync`, `GetReceiptByTransactionAsync`, `PrintReceiptAsync`
- **Services/ReceiptService.vb**: full implementation with sequential OR number generation, VAT logic, items snapshot, and thermal print formatter

## Feature: POS-07

### Overview
Implemented sales return and exchange processing for the POS module. Returns are validated against original transactions, enforce quantity limits, optionally restock inventory via a cross-module event, and automatically reduce credit balances when the original payment method was Credit.

### Requirements
- **Services/ISalesReturnService.vb**: interface defining `ProcessReturnAsync`, `GetReturnsForTransactionAsync`, and `GetReturnHistoryAsync`
- **Services/SalesReturnService.vb**: full implementation with validation, refund calculation, credit balance adjustment, and restock event publishing
- **Events/StockReturnedEvent.vb**: new MediatR notification consumed by the Inventory module to add returned stock back; required for the cross-module restock flow

## Feature: POS-08

### Overview
Implemented the daily sales summary service for the POS module, covering daily, weekly, and monthly aggregation. Addresses Problem S5 (no daily summary).

### Requirements
- **Services/IDailySummaryService.vb**: interface + DTO classes (`PaymentMethodBreakdownDto`, `TopProductDto`, `DailySummaryDto`, `PeriodSummaryDto`)
- **Services/DailySummaryService.vb**: implementation querying `POSDbContext` for transactions and returns; computes totals, payment breakdown with percentages, top-5 products by quantity, and return metrics; period summary includes per-day breakdown list for trend analysis

## Feature: POS-09

### Overview
Implemented the primary POS transaction screen — the Sales Cart view — which is the most
frequently used interface in the system. Covers the full workflow: product search → add to
cart → payment method selection → pay → receipt preview, including the non-negotiable
credit-blocking rule.

### Requirements
- **MerchSys.SharedKernel/Queries/GetProductCatalogQuery.vb**: MediatR query contract for POS → Inventory cross-module product search (name, SKU, price, stock)
- **MerchSys.SharedKernel/Queries/GetProductCatalogResult.vb**: result DTO with `ProductCatalogItem` (ProductId, ProductName, Sku, UnitPrice, AvailableStock, IsLowStock)
- **MerchSys.POS/Presenters/SalesCartPresenter.vb**: full MVP Presenter with:
- `CartLineItem` (ObservableObject wrapper for DataGrid row editing)
- `ProductSearchItem` (UI DTO from catalog query results)
- Product search via MediatR `GetProductCatalogQuery`
- Cart CRUD via `ICartService` (add, update qty, remove, finalize)
- Payment method selection (Cash / GCash / Bank / Credit) with per-method UI flags
- Cash: real-time change calculation
- Credit: customer search/select via `ICreditService`, `IsBlocked` guard on `CanPay`
- `CanPay` gate: empty cart, insufficient cash, blocked customer all disable Pay
- `ProcessPaymentAsync`: FinalizeAsync → ProcessPaymentAsync → GenerateReceiptAsync sequence
- `SyncCartLines` helper preserves AvailableStock across service round-trips
- **MerchSys.App/Views/POS/SalesCartView.Designer code**: 3-panel UserControl layout:
- Left (260 px): product search with auto-complete list, stock color-coding, double-click to add
- Center (*): DataGrid (Product / Qty / UnitPrice / Discount / LineTotal / Remove) + SubTotal / Discount / VAT / Grand Total
- Right (300 px): payment method buttons (active highlighting via DataTrigger), Cash/Credit conditional panels, blocked customer red badge, Pay button (grey when disabled), receipt preview panel
- **MerchSys.App/Views/POS/SalesCartView.Designer code.vb**: code-behind with DI constructor, `CellEditEnding` handler for quantity updates, double-click handler for product add, Enter key search

## Feature: POS-10

### Overview
Implemented the Credit Management screen (POS-10) — a dedicated view for managing customer credit accounts, recording payments, viewing history, and monitoring AR status.

### Requirements
- **MerchSys.POS/Presenters/CreditManagementPresenter.vb**: Presenter with summary stats (total AR, blocked count, overdue count), in-memory filtered account list, add-account form, payment dialog, and per-account payment/credit-transaction history. Includes a `CreditTransactionItem` helper DTO. Injects `ICreditService` + `POSDbContext` directly for the credit-transaction history query (ICreditService has no method for this).
- **MerchSys.App/Views/POS/CreditManagementView.Designer code**: Full layout with summary cards, filter/search action bar, collapsible add-account form, accounts DataGrid with status icons and per-row Pay button, right-side history panel (payments + credit sales), and a ZIndex overlay for the Record Payment dialog.
- **MerchSys.App/Views/POS/CreditManagementView.Designer code.vb**: Code-behind with constructor DI, DataGrid SelectionChanged → `SelectAccountCommand`, and Enter-key search box handler.
**Build fix applied:** VB.NET `List.Count(predicate)` conflicts with the `.Count` property; replaced with `.Where(predicate).Count()`.
**Build fix applied:** `Border` in the history panel contained two children (empty-state StackPanel + ScrollViewer); wrapped both in a `Grid`.

## Feature: POS-11

### Overview
Implemented the Transaction History view (POS-11): a full WinForms screen for searching past transactions, viewing line items and receipt info, and processing returns — including a modal return dialog with over-return validation.

### Requirements
- **MerchSys.POS/Presenters/TransactionHistoryPresenter.vb**: Presenter with four nested display types (`TransactionSummaryItem`, `TransactionDetailLine`, `ReturnSummaryItem`, `ReturnLineSelection`) and full async command set for search, transaction selection, receipt printing, and return processing
- **MerchSys.App/Views/POS/TransactionHistoryView.Designer code**: WinForms UserControl with filter bar (date range, TX#, payment method), transaction DataGrid, detail panel (line items + receipt info + returns tabs via ScrollViewer), action buttons, and return dialog overlay
- **MerchSys.App/Views/POS/TransactionHistoryView.Designer code.vb**: Code-behind wiring DataGrid SelectionChanged to `SelectTransactionCommand`, Enter-key search on TX# field, and numeric-only guard on return quantity TextBox

## Feature: POS-12

### Overview
Implemented the Daily Summary View (POS-12): a WinForms UserControl that presents daily, weekly, and monthly sales summaries backed by `IDailySummaryService`.

### Requirements
- **MerchSys.POS/Presenters/DailySummaryPresenter.vb**: Presenter with three period modes (Daily/Weekly/Monthly), KPI properties, payment breakdown and top-products collections, trend bar data, Prev/Next navigation commands, and auto-load on view open.
- **MerchSys.App/Views/POS/DailySummaryView.Designer code**: Designer code layout with period-selector header (mode toggle buttons + Prev/Next + DatePicker/week-range/month-year inputs), four KPI summary cards, payment breakdown DataGrid, top-5 products DataGrid, and a bottom-anchored bar chart trend section (hidden for daily mode).
- **MerchSys.App/Views/POS/DailySummaryView.Designer code.vb**: code-behind that injects `DailySummaryPresenter` via constructor DI and auto-triggers `LoadCommand` on the `Loaded` event.

## Feature: POS-13

### Overview
Implemented BIR tamper-proof receipt retention and gap-free sequence generation as specified in POS-13.
Introduces a SHA-256 hash chain sidecar entity, a row-locked per-year sequence table, a cold-storage archive table, an EF `SaveChangesInterceptor` that blocks any modification or deletion of receipts, and a manual EF migration with SQLite-level triggers for defense-in-depth.

### Requirements
- **SharedKernel/Exceptions/ImmutableEntityException.vb**: exception thrown by the interceptor; message includes entity type, PK, and NIRC §235 reference
- **MerchSys.POS/Entities/ReceiptIntegrity.vb**: SHA-256 hash chain sidecar, one-to-one with `OfficialReceipt`; stores `IntegrityHash`, `PreviousHash`, `RetentionExpiresAt`, `CanonicalPayload`, `IsImmutable`, `HashAlgorithm`
- **MerchSys.POS/Entities/ReceiptSequence.vb**: per-year sequence row with `RowVersion As Byte()` as EF optimistic concurrency token
- **MerchSys.POS/Entities/OfficialReceiptArchive.vb**: cold-storage copy of receipts past retention + grace period; mirrors `OfficialReceipt` columns plus `ArchivedAt` and `ArchivedHash`
- **MerchSys.POS/Services/IReceiptIntegrityService.vb**: interface with `ComputeAndPersistAsync`, `ValidateAsync`, `ValidateChainAsync`, `GetNextReceiptNumberAsync`; plus `IntegrityValidationResult` and `ChainValidationResult` result classes
- **MerchSys.POS/Services/ReceiptIntegrityService.vb**: full implementation; hash computed as `SHA256(canonical_json || previous_hash)`; canonical JSON built from receipt fields + lines sorted by `ProductId`; sequence generation uses `Serializable` transaction with up to 10 `DbUpdateConcurrencyException` retries; tamper detection publishes `ReceiptTamperDetectedEvent` via MediatR and logs structured audit entry
- **MerchSys.POS/Data/Interceptors/ImmutableReceiptInterceptor.vb**: overrides `SavingChanges`/`SavingChangesAsync`; throws `ImmutableEntityException` for any `Modified` or `Deleted` state on `OfficialReceipt` or `ReceiptIntegrity`
- **MerchSys.POS/Data/Configurations/ReceiptIntegrityConfiguration.vb**: table `Pos_ReceiptIntegrity`, unique index on `ReceiptId`, index on `RetentionExpiresAt`, one-to-one FK to `Pos_OfficialReceipts`
- **MerchSys.POS/Data/Configurations/ReceiptSequenceConfiguration.vb**: table `Pos_ReceiptSequence`, unique index on `Year`, `RowVersion` marked `IsConcurrencyToken()`
- **MerchSys.POS/Data/Configurations/OfficialReceiptArchiveConfiguration.vb**: table `Pos_OfficialReceiptArchive`, index on `OriginalReceiptId`
- **MerchSys.POS/Data/POSDbContext.vb**: added three new `DbSet` properties; added `OnConfiguring` override that registers `ImmutableReceiptInterceptor`
- **MerchSys.POS/Migrations/20260510120000_AddBirRetentionConstraints.vb**: inline SQL migration creating `Pos_ReceiptSequence`, `Pos_ReceiptIntegrity`, `Pos_OfficialReceiptArchive` tables; adds SQLite triggers `pos_receipts_no_update` and `pos_receipts_no_delete` that `RAISE(ABORT, 'BIR-immutable')`
- **MerchSys.POS/Migrations/POSDbContextModelSnapshot.vb**: added snapshot entries for all three new entities including `IsConcurrencyToken()` on `RowVersion` and the `ReceiptIntegrity → OfficialReceipt` navigation
- **MerchSys.App/appsettings.json**: added `"Bir"` section with `RetentionYears: 10`, `HashAlgorithm: "SHA-256-v1"`, `ArchivePolicy.GraceDays: 90`
- **MerchSys.POS/Tests/Pos.SequenceConcurrencyHarness.vb**: debug-only module (`#If DEBUG Then`) with `RunAsync(connectionString)` that launches 1000 parallel `GetNextReceiptNumberAsync` calls and asserts unique contiguous results

## Feature: POS-14

### Overview
Implements BIR three-bucket VAT decomposition (Vatable / Exempt / Zero-Rated) at the line level, with aggregation to the transaction level, a singleton `VatConfiguration` entity, and a `VatAwareReceiptService` decorator that stamps VAT totals and publishes both the new `SaleCompletedWithVatEvent` and the legacy `SaleCompletedEvent` before returning the `OfficialReceipt`.

### Requirements
- **Entities/VatConfiguration.vb**: singleton entity (Id=1 enforced by CHECK constraint); holds `IsVatRegistered`, `VatRate`, `NonVatPercentageTaxRate`, `EffectiveFrom`, and BIR header fields
- **Entities/Extensions/SalesTransactionVatExtension.vb**: partial class adding `VatableSales`, `VatExemptSales`, `ZeroRatedSales`, `VatRateSnapshot`, `IsVatRegisteredSnapshot` to `SalesTransaction`
- **Entities/Extensions/SalesTransactionLineVatExtension.vb**: partial class adding `Treatment` (VatTreatment enum), `VatableAmount`, `VatExemptAmount`, `ZeroRatedAmount`, `OutputVat` to `SalesTransactionLine`
- **Data/Configurations/VatConfigurationMap.vb**: EF IEntityTypeConfiguration for `Pos_VatConfiguration` table; `ValueGeneratedNever()`, `HasCheckConstraint`, precision on decimal columns
- **Data/Configurations/SalesTransactionConfiguration.vb**: added precision mappings for the five new VAT columns
- **Data/Configurations/SalesTransactionLineConfiguration.vb**: added `HasColumnType("INTEGER")` for `Treatment` enum and precision mappings for four VAT amount columns
- **Data/POSDbContext.vb**: added `Public Property VatConfigurations As DbSet(Of VatConfiguration)`
- **Services/IVatCalculator.vb**: interface with `Decompose`, `CalculateLine`, `AggregateTransaction`; also defines `VatBreakdown` and `TransactionVatTotals` result types
- **Services/VatCalculator.vb**: Philippines BIR decomposition; prices are VAT-inclusive; `MidpointRounding.ToEven`; line-level decompose then sum (no re-decomposition of gross)
- **Services/VatConfigurationLoader.vb**: singleton cache using `IServiceScopeFactory` + `SemaphoreSlim(1,1)` double-checked lazy load; `Invalidate()` for post-update refresh
- **Services/VatAwareReceiptService.vb**: decorator over concrete `ReceiptService`; 7-step flow: load transaction, decompose lines, aggregate totals, `SaveChangesAsync`, delegate to inner, `ComputeAndPersistAsync` hash chain, publish `SaleCompletedWithVatEvent` + legacy `SaleCompletedEvent`
- **Data/DatabaseInitializer.vb**: added `ApplyIfPending` calls for `20260510120000_AddBirRetentionConstraints` and `20260514100000_AddVatThreeBucketColumns`; added corresponding private Sub bodies:
- `ApplyBirRetentionConstraints`: creates `Pos_ReceiptSequence`, `Pos_ReceiptIntegrity`, `Pos_OfficialReceiptArchive` tables and SQLite immutability triggers (`IF NOT EXISTS` for idempotency)
- `ApplyVatThreeBucketColumns`: `ALTER TABLE ADD COLUMN` for 5 columns on `Pos_SalesTransactions`, 5 on `Pos_SalesTransactionLines`; creates `Pos_VatConfiguration` with seed row (Id=1, `IsVatRegistered=0`, `VatRate=0.12`)
- **Application.Designer code.vb**: replaced single `AddScoped(Of IReceiptService, ReceiptService)` with manual decorator registrations: `AddScoped(Of ReceiptService)`, `AddScoped(Of IReceiptService, VatAwareReceiptService)`, `AddScoped(Of IVatCalculator, VatCalculator)`, `AddSingleton(Of VatConfigurationLoader)`, `AddScoped(Of IReceiptIntegrityService, ReceiptIntegrityService)` (the last one was previously missing)

## Feature: POS-15

### Overview
Closes three causally linked gaps identified in the 2026-05-11 POS audit:

1. `ReceiptService.GenerateReceiptAsync` was using a naïve `CountAsync` pattern to produce receipt numbers — not safe under concurrent transactions and superseded by POS-13.
2. `IReceiptIntegrityService` was registered in the composition root (`Application.Designer code.vb`) rather than a module-local extension, inconsistent with the `AddPurchasingServices()` pattern.
3. `Pos_SequenceConcurrencyHarness` existed but had no runner that produces a verifiable Markdown report.

### Requirements
- **Services/ReceiptService.vb**: injected `IReceiptIntegrityService`, replaced `CountAsync`-based `GenerateReceiptNumberAsync` helper with `_receiptIntegrity.GetNextReceiptNumberAsync(DateTime.Now.Year)`, deleted the helper method entirely, added XML doc comment.
- Confirmed `Services/VatAwareReceiptService.vb` — decorator pattern intact; delegates to `_inner.GenerateReceiptAsync()` with no numbering logic of its own. No changes required.
- **Startup/PosServiceRegistration.vb**: new `AddPosModule()` extension method consolidating all POS service and Presenter registrations.
- **Application.Designer code.vb**: replaced 14 individual POS `AddScoped`/`AddSingleton`/`AddTransient` lines with a single `services.AddPosModule()` call; removed now-unused `Imports MerchSys.POS.Services` and `Imports MerchSys.POS.Presenters`.
- **Debug/ReceiptSequenceHarnessReport.vb**: debug-only (`#If DEBUG`) harness runner; uses per-worker `POSDbContext` against a scratch SQLite file, writes Markdown report to `%TEMP%`, cleans up scratch DB.

## Feature: POS-16

### Overview
Implemented the receipt archival background service that moves expired `Pos_OfficialReceipts` and `Pos_ReceiptIntegrity` rows into cold-storage archive tables once the BIR 10-year retention window plus the configured grace period has elapsed. Amended the POS-13 SQLite DELETE trigger to allow archival-path deletes using a connection-scoped session variable pattern.

### Requirements
- **Entities/ReceiptIntegrityArchive.vb**: new INSERT-only archive entity mirroring `ReceiptIntegrity` plus `ArchivedAt` and `ArchivedByService`
- **Data/Configurations/ReceiptIntegrityArchiveConfiguration.vb**: EF Core fluent configuration for `Pos_ReceiptIntegrityArchive`
- **Services/Archival/IReceiptArchivalService.vb**: interface + `ReceiptArchivalBatchResult` class
- **Services/Archival/ReceiptArchivalOptions.vb**: options bound from `Receipts:Archival` (three knobs: `Enabled`, `IntervalHours`, `BatchSize`)
- **Services/Archival/ReceiptArchivalService.vb**: implements `IReceiptArchivalService` and `IHostedService`; uses `PeriodicTimer`, scope-factory pattern, session-variable trigger bypass
- **Migrations/20260515140000_AddReceiptIntegrityArchive.vb**: creates `Pos_ReceiptIntegrityArchive`, adds INSERT-only triggers to both archive tables, and amends `pos_receipts_no_delete`
- **Data/POSDbContext.vb**: added `ReceiptIntegrityArchives As DbSet(Of ReceiptIntegrityArchive)`
- **Migrations/POSDbContextModelSnapshot.vb**: added `ReceiptIntegrityArchive` entity snapshot
- **MerchSys.POS.vbproj**: added `Microsoft.Extensions.Hosting.Abstractions 10.0.0` (required for `IHostedService` in module library)
- **Startup/PosServiceRegistration.vb**: registered `ReceiptArchivalOptions` via `AddOptions().BindConfiguration("Receipts:Archival")`, `AddHostedService(Of ReceiptArchivalService)`, and `AddScoped(Of IReceiptArchivalService, ReceiptArchivalService)`
- **Data/DatabaseInitializer.vb**: added `ApplyAddReceiptIntegrityArchive` migration method and `ApplyIfPending` call
- **appsettings.json**: added `Receipts:Archival` section

## Feature: POS-17

### Overview
Implements the VAT Settings UI (POS-17): a dedicated settings view, view-model, write-side service, and shell navigation entry that allows a Manager to edit `Pos_VatConfiguration` via the UI rather than via ad-hoc SQL. After every successful save the `VatConfigurationLoader` singleton cache is invalidated so `VatAwareReceiptService` and the Accounting KPI providers pick up the new values on the next operation without an app restart.

### Requirements
- **Events/VatConfigurationChangedEvent.vb**: new MediatR event with `OccurredAt`, `IsVatRegistered`, `PreviousIsVatRegistered`
- **Services/IVatConfigurationWriter.vb**: interface `IVatConfigurationWriter`, DTOs `VatConfigurationUpdateRequest`/`VatConfigurationUpdateResult`, and implementation `VatConfigurationWriter` (Scoped)
- **Presenters/VatSettingsPresenter.vb**: Presenter with `SaveCommand`, `ReloadCommand`, percent ↔ fraction conversion, `IsNotVatRegistered` inverse property for Designer code visibility
- **Views/POS/VatSettingsView.Designer code**: two-column settings form: toggle, conditional TIN/rate fields, business info, validation error panel
- **Views/POS/VatSettingsView.Designer code.vb**: code-behind; triggers `ReloadAsync` on `UserControl.Loaded`
- **Interfaces/INotificationService.vb**: added `ShowSuccess(message)` and `ShowError(message)` methods so module-library Presenters can surface toast notifications without a direct WinForms reference
- **Services/DefaultNotificationService.vb**: implemented `ShowSuccess`/`ShowError` using `Notification.WinForms.NotificationManager` (same pattern as `WinFormsLowStockNotifier`)
- **Startup/PosServiceRegistration.vb**: registered `IVatConfigurationWriter → VatConfigurationWriter` (Scoped) and `VatSettingsPresenter` (Transient)
- **Application.Designer code.vb**: registered `Views.POS.VatSettingsView` (Transient)
- **Presenters/MainWindowPresenter.vb**: extracted `BuildPosNavItems()` helper; appends `VatSettings → VatSettingsView` entry only when `CurrentRole = Manager`

## Feature: POS-18

### Overview
Implemented receipt body VAT bucket printing (POS-18). Introduced `IReceiptBodyComposer` /
`BirCompliantReceiptBodyComposer` as the single authoritative path for composing
BIR-compliant Official Receipt text, and wired `ReceiptService.PrintReceiptAsync` to
delegate body composition to the new abstraction.

### Requirements
- **Services/ReceiptFormatting/IReceiptBodyComposer.vb**: interface + `ReceiptBody` class with five named blocks and `AllLines` helper
- **Services/ReceiptFormatting/BirCompliantReceiptBodyComposer.vb**: BIR-compliant implementation; reads VAT buckets from `receipt.Transaction.VatableSales/VatExemptSales/ZeroRatedSales` and `receipt.VatAmount`
- **Services/ReceiptService.vb**: added `IReceiptBodyComposer` constructor parameter; removed inline `BuildItemsSnapshot` / `FormatReceipt` helpers; `PrintReceiptAsync` now loads `VatConfiguration` from DB and delegates to the composer
- **Startup/PosServiceRegistration.vb**: registered `IReceiptBodyComposer → BirCompliantReceiptBodyComposer` as Scoped

## Feature: POS-19

### Overview
Implemented the rendering layer for Bureau of Internal Revenue (BIR) compliant Official Receipts in VISTA's POS module. Extracted the console receipt rendering out of `ReceiptService` into a stateless, pluggable abstraction (`IReceiptRenderer`) and introduced a high-fidelity monospace single-page PDF receipt renderer using `QuestPDF`. The active renderer is dynamically registered at the composition root based on operator configuration in `appsettings.json`.

### Requirements
- **Added NuGet Package**:
- `QuestPDF` (Version `2026.5.0` pinned in `MerchSys.POS.vbproj`). Uses SkiaSharp internally; **no `SixLabors.ImageSharp` transitive**. Resolved transitive tree triggers zero NU1902/NU1903 NuGet security advisories. License: QuestPDF Community License — free for organisations with annual revenue under USD 1M, which Villon Farm Supply satisfies. License declared once at module registration time via `QuestPDF.Settings.License = LicenseType.Community`.
- **Created Abstractions & Models**:
- **IReceiptRenderer.vb**: Core interface for receipt output channels.
- **ReceiptRenderTarget.vb**: Enum for targets (`Console`, `Pdf`).
- **ReceiptPdfOptions.vb**: Strongly-typed options (`OutputDirectory`, `FileNamePattern`, `FontFamily`, `FontSizePt`).
- **Implemented Renderers**:
- **ConsoleReceiptRenderer.vb**: Monospace console stream output preserving Unicode pesos (`₱`).
- **PdfReceiptRenderer.vb**: Monospace single-page PDF generator using ISO A5 portrait orientation (`PageSizes.A5`). Layout uses QuestPDF's `Column` of `Item().Text(line)` calls, one row per `body.AllLines` entry. Defensive overwrite guard throws `InvalidOperationException` on filename collisions. QuestPDF handles font fallback internally if the configured family is not installed.
- **Refactored POS Services & Composition Root**:
- **ReceiptService.vb**: Injected `IReceiptRenderer` and updated `PrintReceiptAsync` to delegate rendering.
- **PosServiceRegistration.vb**: Declared QuestPDF Community License, configured `ReceiptPdfOptions` binding, and implemented dynamic `IReceiptRenderer` registration based on configuration values.
- **appsettings.json**: Configured `"POS:Receipt"` defaults using `Console` renderer to preserve clean startup out-of-the-box.



## Verification (from POS-verification-checklist.md)

---
module: POS
source: POS-audit-2026-06-01.md
originally-generated: 2026-05-17
last-synced: 2026-06-01
---

# Operator Verification Checklist — POS

> Extracted from the 2026-05-17 module audit. Only operator/manual verification tasks are listed here.
> All 18 POS plans are completed. These are the remaining acceptance tests.
>
> **How to use:** Do each step in order. Check the box when done. Write what you saw next to each item.
>
> **Login required (INFRA-15):** The app now shows a login screen on launch.
> Unless a test specifically says to log in as Owner, log in as `manager`.
> Default password: `Vista2026!` (first login will prompt you to change it).

### Key file locations

| What | Path |
|------|------|
| SQLite database | `%LOCALAPPDATA%\MerchSys\merchsys.db` |
| DailySummaryView (code-behind) | `WinForms_Applications\MerchSys\src\MerchSys.App\Views\POS\DailySummaryView.Designer code.vb` |
| DailySummaryPresenter | `WinForms_Applications\MerchSys\src\MerchSys.POS\Presenters\DailySummaryPresenter.vb` |
| Concurrency harness | `WinForms_Applications\MerchSys\src\MerchSys.POS\Tests\Pos.SequenceConcurrencyHarness.vb` |
| ReceiptArchivalService | `WinForms_Applications\MerchSys\src\MerchSys.POS\Services\Archival\ReceiptArchivalService.vb` |
| ReceiptArchivalOptions | `WinForms_Applications\MerchSys\src\MerchSys.POS\Services\Archival\ReceiptArchivalOptions.vb` |
| VatSettingsView (code-behind) | `WinForms_Applications\MerchSys\src\MerchSys.App\Views\POS\VatSettingsView.Designer code.vb` |
| VatSettingsPresenter | `WinForms_Applications\MerchSys\src\MerchSys.POS\Presenters\VatSettingsPresenter.vb` |
| VatConfigurationLoader | `WinForms_Applications\MerchSys\src\MerchSys.POS\Services\VatConfigurationLoader.vb` |
| VatConfigurationChangedEvent | `WinForms_Applications\MerchSys\src\MerchSys.SharedKernel\Events\VatConfigurationChangedEvent.vb` |
| IVatConfigurationWriter | `WinForms_Applications\MerchSys\src\MerchSys.POS\Services\IVatConfigurationWriter.vb` |
| POS service registration (DI) | `WinForms_Applications\MerchSys\src\MerchSys.App\Startup\PosServiceRegistration.vb` |

---

## POS-12 — View — Daily Summary

### Test 1: Daily Summary view works end-to-end

**What to do:**
1. Launch the app (F5).
2. Complete at least one sale so there is data for today.
3. Navigate to the **Daily Summary** view (in the POS section of the sidebar).
4. Look at the summary data displayed.

> **View file:** `WinForms_Applications\MerchSys\src\MerchSys.App\Views\POS\DailySummaryView.Designer code.vb`
> **Presenter:** `WinForms_Applications\MerchSys\src\MerchSys.POS\Presenters\DailySummaryPresenter.vb`

**What you should see:**
- The view opens without crashing.
- Today's sales data appears (total sales, number of transactions, etc.).
- The numbers look correct based on the sales you just made.

- [x] Daily Summary view loads and shows correct data — View opened, KPIs correct, Top 5 products aggregated correctly. Fixed 2 bugs: ImmutableEntityException on PAY (double GenerateReceiptAsync call) and Top 5 grouping (missing Key keyword on VB.NET anonymous type).

---

## POS-13 — BIR Tamper-Proof Receipt Retention & Sequence

### Test 2: Receipt sequence concurrency harness

**What to do:**
1. Open Visual Studio with the solution loaded.
2. Build in **Debug** configuration (Ctrl+Shift+B).
3. Press **F5** to launch the app.
4. Open the **Immediate Window** (Debug → Windows → Immediate).
5. Type the following and press Enter:
   ```
   ? Await Pos_SequenceConcurrencyHarness.RunAsync()
   ```
6. Wait for it to finish (this may take a few seconds).

> **Harness file:** `WinForms_Applications\MerchSys\src\MerchSys.POS\Tests\Pos.SequenceConcurrencyHarness.vb`

**What you should see:**
- The harness runs multiple threads trying to reserve receipt sequence numbers at the same time.
- The output should show:
  - **No duplicate** sequence numbers were assigned.
  - **No gaps** in the sequence.
- If you see duplicates or gaps, there is a concurrency bug.

- [x] Concurrency harness: no duplicates and no gaps in receipt sequence — [PASS] 1000 numbers generated, 1000 unique, contiguous sequence confirmed. Fixed 3 harness bugs: missing no-arg overload (Immediate Window unsupported), EnsureCreatedAsync race (called 1000× in parallel), WAL file lock on temp DB cleanup.

---

## POS-15 — Receipt Numbering Integration & Concurrency Validation

### Test 3: Receipt sequence harness report

**What to do:**
1. Open Visual Studio with the solution loaded.
2. Build in **Debug** configuration (Ctrl+Shift+B).
3. Press **F5** to launch the app.
4. Open the **Immediate Window** (Debug → Windows → Immediate).
5. Type the following and press Enter:
   ```
   ? Await ReceiptSequenceHarnessReport.RunAndReportAsync()
   ```
6. Wait for it to finish.

**What you should see:**
- A report with four key numbers:
  - `Duplicates = 0` (no duplicate sequence numbers)
  - `Gaps = 0` (no missing numbers in the sequence)
  - `TotalReservations = 800` (the harness reserved 800 numbers)
  - `Elapsed` = a reasonable time (a few seconds is normal)

- [x] Receipt harness report: Duplicates=0, Gaps=0, TotalReservations=800 — PASS. TotalReservations=800, Duplicates=0, Gaps=0, ElapsedMs=2383. Wired to Dev menu button (Immediate Window blocked by VS hot-reload).

---

## POS-16 — Receipt Archival Background Service

> This section has 6 tests. They all involve the receipt archival system that moves old receipts to archive tables.
>
> **Service file:** `WinForms_Applications\MerchSys\src\MerchSys.POS\Services\Archival\ReceiptArchivalService.vb`
> **Options file:** `WinForms_Applications\MerchSys\src\MerchSys.POS\Services\Archival\ReceiptArchivalOptions.vb`

### Test 4: Archival moves the right number of receipts

**What to do:**
1. Open DB Browser for SQLite and open `%LOCALAPPDATA%\MerchSys\merchsys.db`.
2. Seed test data: insert 100 receipts into `Pos_OfficialReceipts` where `RetentionExpiresAt` is in the past (expired) AND they are from a **previous** fiscal year. Also insert 100 receipts where `RetentionExpiresAt` is still in the future (in-window).
3. Trigger the archival service (launch the app and wait for the background service to run, or trigger it manually from code).
4. Check the `Pos_OfficialReceiptArchive` table.

**What you should see:**
- Exactly 100 receipts were moved to the archive table (the expired ones).
- The 100 in-window receipts remain in `Pos_OfficialReceipts`.

- [x] Archival moves exactly 100 expired receipts, leaves 100 in-window untouched — PASS. ReceiptsMoved=100, LiveRemaining=100, ArchiveCount=100, ElapsedMs=1204. EF Core ToListAsync anonymous-type projection materialized correctly. No production code change needed; harness wired to Dev menu button.

---

### Test 5: Batch size flag works correctly

**What to do:**
1. Keep the same test data from Test 4 (or re-seed 100 expired receipts).
2. Change the batch size to `batchSize = 50`. You can do this by editing:
   `WinForms_Applications\MerchSys\src\MerchSys.POS\Services\Archival\ReceiptArchivalOptions.vb`
   or setting it in the app configuration.
3. Trigger the archival service once.

**What you should see:**
- Only 50 receipts are moved in this batch.
- The archival result shows `HadMoreEligible = True` (meaning there are more receipts waiting to be archived in the next batch).

- [x] Batch size of 50: only 50 moved, HadMoreEligible = True — PASS. ReceiptsMoved=50, HadMoreEligible=True, LiveRemaining=50, ArchiveCount=50, ElapsedMs=1101. No production code change needed.

---

### Test 6: Fiscal-year guard prevents premature archival

**What to do:**
1. Open DB Browser for SQLite and open `%LOCALAPPDATA%\MerchSys\merchsys.db`.
2. Insert some receipts into `Pos_OfficialReceipts` where `RetentionExpiresAt` is in the past BUT `IssuedAt` is in the **current fiscal year** (i.e., this year, 2026).
3. Trigger the archival service.

**What you should see:**
- Those receipts are **NOT** archived, even though their retention has "expired".
- The BIR rule is: receipts from the current fiscal year must never be archived regardless of the retention date.

- [x] Current-year receipts are NOT archived even if RetentionExpiresAt is past — PASS. ReceiptsMoved=0, LiveRemaining=20, ArchiveCount=0 with IssueDate.Year=2026. Fiscal-year guard working correctly.

---

### Test 7: Rollback on archive failure

**What to do:**
1. Set up a test where the archive-insert step will fail. One way: temporarily rename the `Pos_OfficialReceiptArchive` table in the database so the insert fails.
2. Trigger the archival service.
3. Check the `Pos_OfficialReceipts` table (the live table).

**What you should see:**
- The live table is untouched — no receipts were deleted.
- The failure is rolled back cleanly. No data is lost.

- [x] Forced archive failure: live table remains intact (rollback works) — PASS. ExceptionThrown=True (DbUpdateException on dropped archive table), LiveRemaining=20. Transaction rolled back cleanly, live table untouched.

---

### Test 8: BIR immutability trigger blocks direct deletes

**What to do:**
1. Open DB Browser for SQLite and open `%LOCALAPPDATA%\MerchSys\merchsys.db`.
2. Try to run this SQL directly:
   ```sql
   DELETE FROM Pos_OfficialReceipts WHERE Id = <pick any id>;
   ```

**What you should see:**
- The DELETE fails with a `BIR-immutable` trigger error.
- This proves the immutability trigger is working — you cannot manually delete official receipts.

- [x] Direct DELETE from Pos_OfficialReceipts fails with BIR-immutable error — PASS. `DELETE FROM Pos_OfficialReceipts WHERE Id = 211` returned `Error: BIR-immutable` (exit code 1). Trigger `pos_receipts_no_delete` is active and working.

---

### Test 9: Archive tables reject UPDATE and DELETE

**What to do:**
1. Open DB Browser for SQLite and open `%LOCALAPPDATA%\MerchSys\merchsys.db`.
2. Try these SQL commands on the archive tables:
   ```sql
   UPDATE Pos_OfficialReceiptArchive SET Status = 'test' WHERE Id = <pick any id>;
   ```
   ```sql
   DELETE FROM Pos_OfficialReceiptArchive WHERE Id = <pick any id>;
   ```
3. Do the same for `Pos_ReceiptIntegrityArchive`.

**What you should see:**
- Both UPDATE and DELETE fail on both archive tables.
- Archive records are permanent and cannot be changed or removed.

- [x] Pos_OfficialReceiptArchive rejects UPDATE and DELETE — PASS. UPDATE and DELETE both returned `Error: BIR-archive-immutable` (exit code 1). Triggers `pos_receipt_archive_no_update` and `pos_receipt_archive_no_delete` confirmed working.
- [x] Pos_ReceiptIntegrityArchive rejects UPDATE and DELETE — PASS. UPDATE and DELETE both returned `Error: BIR-archive-immutable` (exit code 1). Triggers `pos_integrity_archive_no_update` and `pos_integrity_archive_no_delete` confirmed working.

---

## POS-17 — VAT Settings UI

### Test 10: Load, save, reload round-trip

**What to do:**
1. Launch the app (F5).
2. Log in with username `manager` and your password.
3. Navigate to **VAT Settings** in the sidebar.
4. Note the current values displayed (VAT rate, registration status, etc.).
5. Change one value (for example, toggle the VAT registration checkbox or change the rate).
6. Click **Save**.
7. Navigate away to a different screen.
8. Navigate back to **VAT Settings**.

> **View file:** `WinForms_Applications\MerchSys\src\MerchSys.App\Views\POS\VatSettingsView.Designer code.vb`
> **Presenter:** `WinForms_Applications\MerchSys\src\MerchSys.POS\Presenters\VatSettingsPresenter.vb`

**What you should see:**
- The new values you saved are still there after navigating away and back.
- The old values are gone — the save actually persisted.

- [x] VAT Settings: load → change → save → reload shows saved values — PASS. Filled in BusinessAddress ("123 National Highway, Bagong Ilog, Pasig City"), saved, navigated away and back; address reloaded correctly. DB confirmed: ModifiedAt=2026-05-23.

---

### Test 11: VatConfigurationLoader returns updated values

**What to do:**
1. After doing Test 10, open the **Immediate Window** in Visual Studio (while the app is still running in Debug).
2. Type:
   ```
   ? Await VatConfigurationLoader.GetAsync()
   ```
3. Check the returned values.

> **Loader file:** `WinForms_Applications\MerchSys\src\MerchSys.POS\Services\VatConfigurationLoader.vb`

**What you should see:**
- The values match what you just saved in the UI (not the old values).

- [x] VatConfigurationLoader.GetAsync() returns the newly saved values — PASS. IsVatRegistered=False, VatRate=0.12, BusinessName="Villon Farm Supply", BusinessAddress="123 National Highway, Bagong Ilog, Pasig City", ModifiedAt=2026-05-23 04:08:40. Cache invalidated by save; loader returned updated DB values. Wired to Dev menu button (VS Immediate Window not suitable for instance service calls).

---

### Test 12: VatConfigurationChangedEvent is published

**What to do:**
1. Open this file in Visual Studio:
   `WinForms_Applications\MerchSys\src\MerchSys.POS\Services\IVatConfigurationWriter.vb`
   (This is where the event gets published after saving.)
2. Find where `VatConfigurationChangedEvent` is created/published. Set a breakpoint on that line.
3. You can also check the event definition at:
   `WinForms_Applications\MerchSys\src\MerchSys.SharedKernel\Events\VatConfigurationChangedEvent.vb`
4. Press **F5** to launch the app in Debug mode.
5. Go to VAT Settings and change the VAT registration status (toggle it on or off).
6. Click Save.

**What you should see:**
- The breakpoint gets hit.
- The event's `PreviousIsVatRegistered` property matches the **old** value (before you changed it).
- The current value matches the **new** value you just set.

- [x] VatConfigurationChangedEvent fires with correct PreviousIsVatRegistered — PASS. Breakpoint hit on PublishAsync at IVatConfigurationWriter.vb:174. previousIsVatRegistered=False (old value), request.IsVatRegistered=True (new value). Event carries correct before/after state.

---

### Test 13: VAT Settings visibility by role

**What to do:**
1. Launch the app and log in with username `manager` and your password.
2. Look at the sidebar navigation.
3. Click the **Log Out** button in the sidebar.
4. Log in with username `owner` and your password.
5. Look at the sidebar navigation again.

> **Navigation config:** `WinForms_Applications\MerchSys\src\MerchSys.App\Presenters\MainWindowPresenter.vb` — search for `VatSettings` to see how role visibility is configured.

**What you should see:**
- **Manager** sees "VAT Settings" in the sidebar.
- **Owner** does **NOT** see "VAT Settings" in the sidebar.

- [x] Manager sees "VAT Settings" in sidebar — PASS.
- [x] Owner does NOT see "VAT Settings" in sidebar — PASS.


### Future / Backlog Item

## 15. ESC/POS Thermal Receipt Renderer

**Module:** POS
**Source:** POS-19 scope decision (2026-05-26)
**Description:** Add an `EscPosThermalRenderer` implementation of `IReceiptRenderer` that emits raw ESC/POS bytes through the Windows print spooler to a configured thermal printer (80mm ≈ 48 chars, 58mm ≈ 32 chars). Includes:
- ESC/POS init / encoding / paper feed / partial cut opcodes
- Width-limited line wrapping (`ReceiptWidthFormatter`)
- "₱" → "PHP " substitution for ASCII-only code pages (`ReceiptCurrencyTransform`)
- P/Invoke wrapper (`RawPrinterHelper`) for `OpenPrinter`/`WritePrinter`/`ClosePrinter`
- Configuration under `POS:Receipt:Thermal` (printer name, paper width, code page, auto-cut)
- Add `Thermal` value to `ReceiptRenderTarget` enum and wire selection in `PosServiceRegistration`
**Why deferred:** No physical thermal printer is available at Villon Farm Supply for hardware verification. Shipping ESC/POS bytes unverified risks deploy-time bugs (wrong opcodes, code-page mismatches, P/Invoke handle leaks) that cannot be caught at build time. POS-19 ships the `IReceiptRenderer` abstraction and a PDF renderer; thermal slots in cleanly when a printer arrives.
**Depends on:** POS-19 (in progress).

---


### Future / Backlog Item

## 18. Stock Valuation, COGS Accuracy, and Duplicate Revenue Record Bugs

**Module:** Inventory / Accounting / POS (cross-cutting)
**Source:** User Observation (2026-05-27) — hands-on testing with multi-vendor purchasing
**Status:** CLOSED — completed via INV-15, ACC-21, and ACC-22. Fully verified under Test 7 of the Manager checklist (split-batch POS checkout and FIFO COGS).
- **18a → INV-15** `Plans/VISTA_Modules/Inventory/15-stock-dashboard-cost-column.md` (UI cost visibility — completed)
- **18c → ACC-22** `Plans/VISTA_Modules/Accounting/22-revenue-record-consolidation.md` (eliminate duplicate-writer race — completed)
- **18b → ACC-21** `Plans/VISTA_Modules/Accounting/21-per-batch-cogs-accuracy.md` (per-batch FIFO COGS via new `Inv_SaleCogs` ledger — completed)

### Reproduction Steps

1. **Vendor Product Catalog tab:** Added "Ammonium Sulfate" (ProductId=3) to all three vendors with different prices:
   - AgriChem Supplies (VendorId=1): ₱1,100/unit
   - FarmFresh Seed Corp. (VendorId=2): ₱1,000/unit
   - Golden Feeds Trading (VendorId=3): ₱1,200/unit
2. **Purchase Orders tab:** Created 3 POs (PO-2026-0001 to PO-2026-0003), one per vendor, each ordering 10 units.
3. **Goods Receiving tab:** Confirmed all 3 POs → 3 `StockBatch` rows created (Batch 1 = ₱1,200, Batch 2 = ₱1,100, Batch 3 = ₱1,000). Total stock: 30 units, total cost: ₱33,000.
4. **Stock Dashboard tab:** Stock quantity was correct (30 units), but the "Price" column showed ₱1,100 — the `RetailPrice` from `Inv_Products` set in the Product Management tab, not the actual per-batch purchase cost.
5. **Sales Cart tab:** Sold all 30 units in a single transaction at ₱1,100/unit (the `RetailPrice`). Revenue = ₱33,000.
6. **Expected:** Some profit or at least break-even, since the average purchase cost was ₱1,100/unit (= ₱33,000/30).
7. **Actual:** Two `RevenueRecord` rows were created with conflicting COGS values, and one shows a ₱3,000 *loss*.

### Root Causes (verified via database inspection)

#### 18a. Stock Dashboard "Price" column shows `RetailPrice`, not actual purchase cost

The `StockDashboardService.GetDashboardDataAsync()` correctly computes `StockValue` using FIFO `UnitCost` from `Inv_StockBatches` (line 116: `b.QuantityRemaining * b.UnitCost`), but the `ProductSummaryDto.RetailPrice` field is sourced from `Inv_Products.RetailPrice` — the selling price set in Product Management, **not** the purchase cost. The dashboard's "Price" column therefore doesn't reflect what was actually paid to vendors.

**DB evidence:**
- `Inv_Products` (ProductId=3): `RetailPrice` = 1100
- `Inv_StockBatches`: Batch 1 `UnitCost` = 1200, Batch 2 `UnitCost` = 1100, Batch 3 `UnitCost` = 1000

**Impact:** User cannot tell the actual purchase cost per product from the Stock Dashboard. The `StockValue` sum card is correct (it uses batch `UnitCost`), but the per-row "Price" column is misleading because it shows the retail selling price, not the cost.

**Fix scope:** The dashboard could show a **weighted-average cost** column alongside `RetailPrice`, computed as `SUM(QuantityRemaining × UnitCost) / SUM(QuantityRemaining)` across non-expired batches. Alternatively, show the FIFO cost (oldest batch's `UnitCost`), matching what Accounting uses for COGS.

#### 18b. COGS uses only the FIFO-oldest batch's `UnitCost` for the *entire* sale quantity

`SaleCompletedAccountingHandler.Handle()` calls `GetProductCostQuery` to get the COGS unit cost. The handler (`GetProductCostQueryHandler`) returns the `UnitCost` of the **single** oldest non-expired batch with remaining stock, then multiplies it by the full `item.Quantity`:

```
costResult.FifoUnitCost * item.Quantity   ← line 45, SaleCompletedAccountingHandler.vb
```

This is incorrect when a sale quantity spans multiple FIFO batches at different costs. The `SaleCompletedHandler` in Inventory *correctly* deducts stock across multiple batches via `DeductStockFIFOAsync()` and returns per-batch `FIFODeductionResult.COGS` values, but Accounting **ignores this** and re-queries cost independently using `GetProductCostQuery`, which only returns a single unit cost.

**DB evidence:**
- `Acc_RevenueRecords` (Id=1): `COGS` = 36000, `GrossProfit` = -3000 (used ₱1,200 × 30 = ₱36,000 — the oldest batch price, despite only 10 of those 30 units came from that batch).
- Correct COGS should be: (10 × ₱1,200) + (10 × ₱1,100) + (10 × ₱1,000) = ₱33,000, yielding ₱0 profit (break-even since `RetailPrice` = average cost = ₱1,100).

**Impact:** **Financial statements show phantom losses (or inflated profits, depending on batch price ordering)**. The reported COGS is wrong any time a sale spans multiple batches with different unit costs. This is an accounting accuracy bug — not cosmetic.

**Fix scope:** The Accounting handler should receive the actual per-batch COGS breakdown from the Inventory module's FIFO deduction engine (the `FIFODeductionResult` list from `StockService.DeductStockFIFOAsync`) instead of independently re-querying a single unit cost. Options:
1. Include the per-batch COGS breakdown directly in the `SaleCompletedEvent` (requires the Inventory handler to run first and pass data downstream).
2. Replace `GetProductCostQuery` with a new query that sums COGS across the batches that were *actually consumed* during this specific sale deduction.

#### 18c. Duplicate `RevenueRecord` rows — race between legacy and VAT accounting handlers

Two `Acc_RevenueRecords` exist for the same `(SourceTransactionId=1, ProductId=3)`:
- Record Id=1: `COGS` = 36000, `GrossProfit` = -3000 (created by `SaleCompletedAccountingHandler`)
- Record Id=2: `COGS` = 0, `GrossProfit` = 33000 (created by `SaleCompletedWithVatHandler` in its `Else` branch)

The `SaleCompletedWithVatHandler` is designed to be idempotent: it first queries for an existing `RevenueRecord` matching `(SourceTransactionId, ProductId)` and updates VAT columns if found. However, **the existing record was not found** — the VAT handler's `Else` branch executed, creating a second record. This happened because:
- `SaleCompletedAccountingHandler` was called first and added the record to the EF `DbContext`, but `SaveChangesWithJournalAsync` may not have flushed before `SaleCompletedWithVatHandler.Handle()` ran its `FirstOrDefaultAsync` query on the **same** `DbContext` instance — resulting in the query not finding the record.
- The second record has `COGS = 0` because by the time `GetProductCostQuery` ran for it, all 30 units had already been deducted by the Inventory handler, leaving zero remaining stock.

**DB evidence:**
- `Acc_ExpenseRecords` (Id=4): `Category` = "COGS", `Amount` = 0 (the COGS expense from the second handler)
- Only one `Pos_SalesTransactions` row exists (Id=1), confirming only one sale happened, but two revenue records exist.

**Impact:** Financial reports double-count revenue, and one of the two records has zero COGS. The duplicate must be prevented.

**Fix scope:** The `SaleCompletedAccountingHandler` should call `SaveChangesWithJournalAsync` *before* the VAT handler runs, so the `FirstOrDefaultAsync` in the VAT handler sees the committed record. Alternatively, combine both handlers into a single handler, or ensure handler ordering via MediatR pipeline configuration.

### Dependencies
- 18a: Independent — UI/display change only.
- 18b: Requires rethinking the COGS data flow between Inventory's FIFO deduction engine and Accounting's revenue recording. May require changes to `SaleCompletedEvent` or a new cross-module query.
- 18c: Requires fixing MediatR handler ordering or merging the two accounting handlers. Related to the dual-event design from POS-14.

### Source Document Evidence

The bugs documented above violate requirements and promises stated in the original project source documents. The following passages confirm that **per-batch COGS accuracy** is not a nice-to-have — it is a core design commitment.

**system_plan.md (§6.2 — Inventory Module):**
> "FIFO Costing: Oldest batch cost used first when valuing sold items, per client confirmation. **Batch-level purchase records maintained for accurate COGS calculation.**"

**system_plan.md (§9 — Database Design Principles):**
> "FIFO Batch-Level Records: Purchase price recorded at the batch/lot level per product to support accurate FIFO COGS calculation. **Oldest unreserved batch cost consumed first on each sale.**"

**system_plan.md (§10/§11 — Risk Register, Risk P5, rated High/High):**
> "Price Volatility & Costing Error: Retail price not updated after supplier cost change — confirmed ongoing problem — **leading to incorrect COGS and distorted profit margins.**"
>
> Mitigation: "FIFO batch-level costing ensures historical sales are not retroactively affected by new purchase prices."

Bug 18b is the *exact manifestation* of P5's predicted failure mode: when a single product is purchased from multiple vendors at different prices, COGS is calculated using only the oldest batch's `UnitCost` for the entire quantity, producing distorted profit margins.

**Inventory-Module_AcademicPaper.md (§2 — FIFO Costing and Inventory Valuation Methods):**
> "When a sale occurs, the system deducts units from the oldest batch first, **calculating the cost of goods sold based on that batch's unit cost.** This approach ensures that the FIFO assumption is applied precisely at the batch level, producing accurate cost-of-goods-sold figures." — (Kieso et al., 2023)

The Inventory module (`StockService.DeductStockFIFOAsync`) correctly implements this: it walks batches oldest-first and computes per-batch COGS in `FIFODeductionResult`. The bug is that Accounting ignores this result and re-queries a single unit cost via `GetProductCostQuery`.

**Accounting-Module_AcademicPaper.md (§1.5 — Scope, V1 Features):**
> "The cost of goods sold will be calculated using the FIFO (First-In, First-Out) costing method, as confirmed by the client, with costs recorded at the batch and lot level per product at the time of receiving goods. **The oldest unreserved batch cost will be consumed first on each sale** to ensure that reported COGS accurately reflects the cost of goods actually sold."

**Accounting-Module_AcademicPaper.md (§2 — Theme 3, FIFO Costing):**
> "Without disciplined batch-level cost tracking, the FIFO method degrades into an approximation that may not accurately reflect the actual flow of costs through inventory." — (Garcia & Lim, 2023)
>
> "Without precise product-level costing, businesses cannot determine which products generate the highest margins, which are unprofitable, and where pricing adjustments are needed." — (Villanueva & Reyes, 2022)

**Purchasing-Module_AcademicPaper.md (§1.2 — Conceptual Framework):**
> P5 identifies "price volatility that causes COGS distortions" as an independent variable the module must address.

**Conclusion:** Bug 18b (single-batch COGS for multi-batch sales) directly contradicts the documented design intent of batch-level FIFO COGS. Bugs 18a and 18c are consequential — 18a prevents the user from *seeing* the cost discrepancy before a sale, and 18c corrupts the accounting output after a sale. Together, these three bugs undermine the system's stated goal of "producing COGS figures that accurately represent the cost of goods actually sold" and its P5 risk mitigation.

---


### Future / Backlog Item

## 24. Direct Electronic Fund Transfer Gateway Integration

**Module:** Point of Sale
**Source:** `POS-Module_AcademicPaper.md` Scope and Delimitation (§1.5)
**Description:** Wire live API integrations for digital payment processors (such as Maya, GCash Merchant APIs, or PayMongo) to process electronic transactions.
**Why deferred:** The POS currently records GCash and bank transfer reference codes manually for bookkeeping reconciliation. Live fund transfers are deferred until a formal merchant account and steady internet connections are established at the physical store.

---

