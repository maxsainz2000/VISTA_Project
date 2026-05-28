---
type: layer-manifest
module: MerchSys.POS
layer: Data Access
last-updated: 2026-05-28
---

# MerchSys.POS — Data Access

This page details the Data Access layer for the **MerchSys.POS** module. Excludes obsolete SQLite migrations; schema bootstrapping is done centrally via `MariaDbSchemaInitializer`.

## DbContext
- **Class:** `POSDbContext`
- **Base:** `AppDbContext` (Inherited from SharedKernel)
- **Primary Responsibility:** Orchestrates persistence for sales transactions, credit accounts, and official receipts.

## Files and Classes

| File Path | Class / Interface | Responsibilities / Notes |
|---|---|---|
| `src/MerchSys.POS/Data/POSDbContext.vb` | `POSDbContext` | Entry point for persistence for all POS entities. Exposes DbSets. |
| `src/MerchSys.POS/Data/Interceptors/ImmutableReceiptInterceptor.vb` | `ImmutableReceiptInterceptor` | `SaveChangesInterceptor` | Blocks UPDATE/DELETE operations on receipts to ensure BIR compliance. |
| `src/MerchSys.POS/Data/POSDbContextFactory.vb` | `POSDbContextFactory` | `IDesignTimeDbContextFactory(Of POSDbContext)` implementation for EF CLI design-time support. |
| `src/MerchSys.POS/Data/Configurations/CreditAccountConfiguration.vb` | `CreditAccountConfiguration` | Configures `CreditAccount` entity. Includes `RowVersion` optimistic concurrency token. |
| `src/MerchSys.POS/Data/Configurations/CreditPaymentConfiguration.vb` | `CreditPaymentConfiguration` | Configures `CreditPayment` entity. |
| `src/MerchSys.POS/Data/Configurations/OfficialReceiptConfiguration.vb` | `OfficialReceiptConfiguration` | Configures `OfficialReceipt` entity. |
| `src/MerchSys.POS/Data/Configurations/SalesReturnConfiguration.vb` | `SalesReturnConfiguration` | Configures `SalesReturn` entity. |
| `src/MerchSys.POS/Data/Configurations/SalesTransactionConfiguration.vb` | `SalesTransactionConfiguration` | Configures `SalesTransaction` entity. Includes `RowVersion` optimistic concurrency token. |
| `src/MerchSys.POS/Data/Configurations/SalesTransactionLineConfiguration.vb` | `SalesTransactionLineConfiguration` | Configures `SalesTransactionLine` entity. |
| `src/MerchSys.POS/Data/Configurations/ReceiptIntegrityConfiguration.vb` | `ReceiptIntegrityConfiguration` | Configures `ReceiptIntegrity` entity. |
| `src/MerchSys.POS/Data/Configurations/ReceiptSequenceConfiguration.vb` | `ReceiptSequenceConfiguration` | Configures `ReceiptSequence` entity. Leverages central `RowVersion` token from ConcurrencyAwareEntity instead of local shadowing. |
| `src/MerchSys.POS/Data/Configurations/OfficialReceiptArchiveConfiguration.vb` | `OfficialReceiptArchiveConfiguration` | Configures `OfficialReceiptArchive` entity. |
| `src/MerchSys.POS/Data/Configurations/ReceiptIntegrityArchiveConfiguration.vb` | `ReceiptIntegrityArchiveConfiguration` | Configures `ReceiptIntegrityArchive` entity. |
| `src/MerchSys.POS/Data/Configurations/VatConfigurationMap.vb` | `VatConfigurationMap` | Configures the singleton `VatConfiguration` entity with Id=1 enforcement. |
| `src/MerchSys.POS/Data/SeedData/POSSeedData.vb` | `POSSeedData` | Contains initial seed data for the POS module. |
