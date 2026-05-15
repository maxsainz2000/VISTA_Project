---
type: layer-manifest
module: MerchSys.POS
layer: Data Access
last-updated: 2026-05-15
---

# MerchSys.POS — Data Access

This page details the Data Access layer for the **MerchSys.POS** module. Provides manual migration files for VB.NET support.

## DbContext
- **Class:** `POSDbContext`
- **Base:** `AppDbContext` (Inherited from SharedKernel)
- **Primary Responsibility:** Orchestrates persistence for sales transactions, credit accounts, and official receipts.

## Files and Classes

| File Path | Class / Interface | Responsibilities / Notes |
|---|---|---|
| `src/MerchSys.POS/Data/POSDbContext.vb` | `POSDbContext` | Entry point for persistence for all POS entities. |
| `src/MerchSys.POS/Data/PosSyncableRepository.vb` | `PosSyncableRepository` | `ISyncableRepository` implementation that journals changes to `Sync_Journal` on save. |
| `src/MerchSys.POS/Data/Interceptors/ImmutableReceiptInterceptor.vb` | `ImmutableReceiptInterceptor` | `SaveChangesInterceptor` | Blocks UPDATE/DELETE operations on receipts to ensure BIR compliance. |
| `src/MerchSys.POS/Data/POSDbContextFactory.vb` | `POSDbContextFactory` | `IDesignTimeDbContextFactory(Of POSDbContext)` implementation for EF CLI design-time support. |
| `src/MerchSys.POS/Data/Configurations/CreditAccountConfiguration.vb` | `CreditAccountConfiguration` | Configures `CreditAccount` entity. |
| `src/MerchSys.POS/Data/Configurations/CreditPaymentConfiguration.vb` | `CreditPaymentConfiguration` | Configures `CreditPayment` entity. |
| `src/MerchSys.POS/Data/Configurations/OfficialReceiptConfiguration.vb` | `OfficialReceiptConfiguration` | Configures `OfficialReceipt` entity. |
| `src/MerchSys.POS/Data/Configurations/SalesReturnConfiguration.vb` | `SalesReturnConfiguration` | Configures `SalesReturn` entity. |
| `src/MerchSys.POS/Data/Configurations/SalesTransactionConfiguration.vb` | `SalesTransactionConfiguration` | Configures `SalesTransaction` entity. |
| `src/MerchSys.POS/Data/Configurations/SalesTransactionLineConfiguration.vb` | `SalesTransactionLineConfiguration` | Configures `SalesTransactionLine` entity. |
| `src/MerchSys.POS/Data/Configurations/ReceiptIntegrityConfiguration.vb` | `ReceiptIntegrityConfiguration` | Configures `ReceiptIntegrity` entity. |
| `src/MerchSys.POS/Data/Configurations/ReceiptSequenceConfiguration.vb` | `ReceiptSequenceConfiguration` | Configures `ReceiptSequence` entity. |
| `src/MerchSys.POS/Data/Configurations/OfficialReceiptArchiveConfiguration.vb` | `OfficialReceiptArchiveConfiguration` | Configures `OfficialReceiptArchive` entity. |
| `src/MerchSys.POS/Data/Configurations/ReceiptIntegrityArchiveConfiguration.vb` | `ReceiptIntegrityArchiveConfiguration` | Configures `ReceiptIntegrityArchive` entity. |
| `src/MerchSys.POS/Data/Configurations/VatConfigurationMap.vb` | `VatConfigurationMap` | Configures the singleton `VatConfiguration` entity with Id=1 enforcement. |
| `src/MerchSys.POS/Data/SeedData/POSSeedData.vb` | `POSSeedData` | Contains initial seed data for the POS module. |
| `src/MerchSys.POS/Migrations/20260507100003_InitialPOS.vb` | `InitialPOS` | Manual EF Core migration (Sqlite) for 6 POS tables and seed data. |
| `src/MerchSys.POS/Migrations/20260510120000_AddBirRetentionConstraints.vb` | `AddBirRetentionConstraints` | Manual EF Core migration for BIR compliance tables and SQLite triggers. |
| `src/MerchSys.POS/Migrations/20260514100000_AddVatThreeBucketColumns.vb` | `AddVatThreeBucketColumns` | Manual EF Core migration adding VAT decomposition columns to sales tables and seeding `Pos_VatConfiguration`. |
| `src/MerchSys.POS/Migrations/20260515140000_AddReceiptIntegrityArchive.vb` | `AddReceiptIntegrityArchive` | Creates `Pos_ReceiptIntegrityArchive` and amends `pos_receipts_no_delete` trigger for archival bypass. |
| `src/MerchSys.POS/Migrations/POSDbContextModelSnapshot.vb` | `POSDbContextModelSnapshot` | EF Core model snapshot for the POS module. |
| `src/MerchSys.POS/Tests/Pos.SequenceConcurrencyHarness.vb` | `Pos.SequenceConcurrencyHarness` | Debug-only harness for stress-testing gap-free sequence generation. |
