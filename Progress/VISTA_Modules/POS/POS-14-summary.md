---
module: MerchSys.POS
agent: claude-code
date: 2026-05-10
plan-ref: Plans/VISTA_Modules/POS/14-vat-configuration-calculation.md
status: completed
---

## Task Summary

Implements BIR three-bucket VAT decomposition (Vatable / Exempt / Zero-Rated) at the line level, with aggregation to the transaction level, a singleton `VatConfiguration` entity, and a `VatAwareReceiptService` decorator that stamps VAT totals and publishes both the new `SaleCompletedWithVatEvent` and the legacy `SaleCompletedEvent` before returning the `OfficialReceipt`.

**Plan:** `14-vat-configuration-calculation.md`

## What Was Done

- Created `src/MerchSys.POS/Entities/VatConfiguration.vb` — singleton entity (Id=1 enforced by CHECK constraint); holds `IsVatRegistered`, `VatRate`, `NonVatPercentageTaxRate`, `EffectiveFrom`, and BIR header fields
- Created `src/MerchSys.POS/Entities/Extensions/SalesTransactionVatExtension.vb` — partial class adding `VatableSales`, `VatExemptSales`, `ZeroRatedSales`, `VatRateSnapshot`, `IsVatRegisteredSnapshot` to `SalesTransaction`
- Created `src/MerchSys.POS/Entities/Extensions/SalesTransactionLineVatExtension.vb` — partial class adding `Treatment` (VatTreatment enum), `VatableAmount`, `VatExemptAmount`, `ZeroRatedAmount`, `OutputVat` to `SalesTransactionLine`
- Created `src/MerchSys.POS/Data/Configurations/VatConfigurationMap.vb` — EF IEntityTypeConfiguration for `Pos_VatConfiguration` table; `ValueGeneratedNever()`, `HasCheckConstraint`, precision on decimal columns
- Modified `src/MerchSys.POS/Data/Configurations/SalesTransactionConfiguration.vb` — added precision mappings for the five new VAT columns
- Modified `src/MerchSys.POS/Data/Configurations/SalesTransactionLineConfiguration.vb` — added `HasColumnType("INTEGER")` for `Treatment` enum and precision mappings for four VAT amount columns
- Modified `src/MerchSys.POS/Data/POSDbContext.vb` — added `Public Property VatConfigurations As DbSet(Of VatConfiguration)`
- Created `src/MerchSys.POS/Services/IVatCalculator.vb` — interface with `Decompose`, `CalculateLine`, `AggregateTransaction`; also defines `VatBreakdown` and `TransactionVatTotals` result types
- Created `src/MerchSys.POS/Services/VatCalculator.vb` — Philippines BIR decomposition; prices are VAT-inclusive; `MidpointRounding.ToEven`; line-level decompose then sum (no re-decomposition of gross)
- Created `src/MerchSys.POS/Services/VatConfigurationLoader.vb` — singleton cache using `IServiceScopeFactory` + `SemaphoreSlim(1,1)` double-checked lazy load; `Invalidate()` for post-update refresh
- Created `src/MerchSys.POS/Services/VatAwareReceiptService.vb` — decorator over concrete `ReceiptService`; 7-step flow: load transaction, decompose lines, aggregate totals, `SaveChangesAsync`, delegate to inner, `ComputeAndPersistAsync` hash chain, publish `SaleCompletedWithVatEvent` + legacy `SaleCompletedEvent`
- Modified `src/MerchSys.App/Data/DatabaseInitializer.vb` — added `ApplyIfPending` calls for `20260510120000_AddBirRetentionConstraints` and `20260514100000_AddVatThreeBucketColumns`; added corresponding private Sub bodies:
  - `ApplyBirRetentionConstraints`: creates `Pos_ReceiptSequence`, `Pos_ReceiptIntegrity`, `Pos_OfficialReceiptArchive` tables and SQLite immutability triggers (`IF NOT EXISTS` for idempotency)
  - `ApplyVatThreeBucketColumns`: `ALTER TABLE ADD COLUMN` for 5 columns on `Pos_SalesTransactions`, 5 on `Pos_SalesTransactionLines`; creates `Pos_VatConfiguration` with seed row (Id=1, `IsVatRegistered=0`, `VatRate=0.12`)
- Modified `src/MerchSys.App/Application.xaml.vb` — replaced single `AddScoped(Of IReceiptService, ReceiptService)` with manual decorator registrations: `AddScoped(Of ReceiptService)`, `AddScoped(Of IReceiptService, VatAwareReceiptService)`, `AddScoped(Of IVatCalculator, VatCalculator)`, `AddSingleton(Of VatConfigurationLoader)`, `AddScoped(Of IReceiptIntegrityService, ReceiptIntegrityService)` (the last one was previously missing)

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | N/A — build verification deferred per project policy |
| Unit tests pass | N/A — test phase is separate |
| Manual verification | N/A |

## Issues Encountered

- **Issue:** `IReceiptIntegrityService` was never registered in DI — `VatAwareReceiptService` depends on it and would throw at runtime.
  - **Resolution:** Added `services.AddScoped(Of IReceiptIntegrityService, ReceiptIntegrityService)()` to the POS DI block in `Application.xaml.vb`.

- **Issue:** `20260510120000_AddBirRetentionConstraints` (POS-13's migration for `Pos_ReceiptIntegrity` etc.) had no `DatabaseInitializer` entry, meaning the table would be missing at runtime for `VatAwareReceiptService`.
  - **Resolution:** Added the `ApplyIfPending` call and full `ApplyBirRetentionConstraints` Sub body in `DatabaseInitializer.vb`.

- **Issue:** No Scrutor in the NuGet manifest — cannot use automatic decorator registration.
  - **Resolution:** Manual factory-style registration: `ReceiptService` registered as concrete scoped type; `IReceiptService` → `VatAwareReceiptService` which receives the concrete `ReceiptService` via DI constructor injection.

## What's Next

- [ ] VAT Settings UI (ViewModel + View) for owner to toggle `IsVatRegistered` and set TIN — calls `VatConfigurationLoader.Invalidate()` after save
- [ ] Update `ReceiptService.GenerateReceiptAsync` to print the three BIR VAT buckets on the receipt body (Vatable Sales, VAT Exempt Sales, Zero-Rated Sales, Output VAT)
- [ ] Accounting handler for `SaleCompletedWithVatEvent` to record VAT-disaggregated revenue entries in `Acc_RevenueRecords`
- [ ] BIR VAT Relief Report (monthly summary of all three buckets) — Accounting module plan

## Cross-References

- Domain Wiki pages consulted: `LLM_Wiki/wiki/concepts/bir-compliance.md`, `LLM_Wiki/wiki/concepts/vat-ready.md`
- Codebase Wiki consulted: `LLM_Wiki/codebase_wiki/index.md`, POS module index
