# Implementation Progress Report: INFRA-22

---

module: Infrastructure
agent: antigravity
date: 2026-05-27
plan-ref: Plans/VISTA_Modules/Infrastructure/22-central-schema-inv-salecogs.md
status: completed

---

## Task Summary

This progress report documents the completion of **INFRA-22: Central MariaDB Schema + Sync Map for Inv_SaleCogs**. We have aligned the remote MariaDB central schema with the local SQLite DB for the `Inv_SaleCogs` table, registered the table in `InventorySyncMap` with deserialization support, and customized the sync context to resolve missing `ModifiedAt` column checks.

**Plan:** `[[22-central-schema-inv-salecogs.md]]`

## What Was Done

- Created [mariadb-inv-salecogs-schema.sql](file:///c:/Users/Admin/Documents/VISTA_Project/Plans/VISTA_Modules/Infrastructure/sql/mariadb-inv-salecogs-schema.sql) — central table DDL script.
- Created [AddInvSaleCogs.sql](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Infrastructure/Data/Migrations/Central/AddInvSaleCogs.sql) — migration script for existing deployments.
- Modified [mariadb-init.sql](file:///c:/Users/Admin/Documents/VISTA_Project/Plans/VISTA_Modules/Infrastructure/sql/mariadb-init.sql) — added central schema table creation for fresh deploys.
- Modified [InventorySyncMap.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Sync/SyncMaps/InventorySyncMap.vb) — added `RemoteSaleCogs` POCO, `"Inv_SaleCogs"` to the tables registry list, and its payload deserialization in `ToRemote`.
- Modified [MariaDbSyncContext.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Sync/MariaDbSyncContext.vb) — updated `FetchRemoteRowAsync` to dynamically check the table name and select `DeductedAt` instead of `ModifiedAt` for `Inv_SaleCogs`, ensuring conflict resolution checks succeed without schema mismatches.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (Build succeeded with 0 warnings, 0 errors) |
| Unit tests pass | N/A |
| Manual verification | ✅ (MariaDB migration executed and table created correctly) |

### SQLite + MariaDB Verification

1. **Idempotency property:** Running `AddInvSaleCogs.sql` multiple times runs without errors and produces no schema drift.
2. **Backlog Rows Drained:** None existed (database was wiped/reset before the task).
3. **Local + Central Table Alignment:**
   - **Local SQLite schema `Inv_SaleCogs`:** `Id` (INT PK), `TransactionId` (INT), `ProductId` (INT), `BatchId` (INT), `QuantityDeducted` (INT), `UnitCost` (TEXT), `Cogs` (TEXT), `DeductedAt` (TEXT).
   - **Central MariaDB `inv_salecogs` created schema:**
     ```sql
     CREATE TABLE `inv_salecogs` (
       `Id` int(11) NOT NULL AUTO_INCREMENT,
       `TransactionId` int(11) NOT NULL,
       `ProductId` int(11) NOT NULL,
       `BatchId` int(11) NOT NULL,
       `QuantityDeducted` int(11) NOT NULL,
       `UnitCost` decimal(18,4) NOT NULL,
       `Cogs` decimal(18,4) NOT NULL,
       `DeductedAt` datetime(6) NOT NULL,
       PRIMARY KEY (`Id`),
       KEY `IX_Inv_SaleCogs_Tx_Product` (`TransactionId`,`ProductId`),
       KEY `IX_Inv_SaleCogs_Batch` (`BatchId`),
       CONSTRAINT `FK_Inv_SaleCogs_Inv_StockBatches_BatchId` FOREIGN KEY (`BatchId`) REFERENCES `inv_stockbatches` (`Id`)
     ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
     ```

### `InventorySyncMap.vb` Changes Git Diff

```diff
diff --git a/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Sync/SyncMaps/InventorySyncMap.vb b/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Sync/SyncMaps/InventorySyncMap.vb
index cd6cf1f..e93f6a4 100644
--- a/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Sync/SyncMaps/InventorySyncMap.vb
+++ b/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Sync/SyncMaps/InventorySyncMap.vb
@@ -106,6 +106,22 @@ Namespace Sync.SyncMaps
         Public Property ModifiedAt As DateTime?
     End Class
 
+    ''' <summary>
+    ''' Synced representation of a local Inv_SaleCogs record.
+    ''' This entity represents an append-only per-batch FIFO COGS record.
+    ''' Reference: ACC-21 (Local SQLite Schema) + INFRA-22 (Central Schema Sync).
+    ''' </summary>
+    Public Class RemoteSaleCogs
+        Public Property Id As Integer
+        Public Property TransactionId As Integer
+        Public Property ProductId As Integer
+        Public Property BatchId As Integer
+        Public Property QuantityDeducted As Integer
+        Public Property UnitCost As Decimal
+        Public Property Cogs As Decimal
+        Public Property DeductedAt As DateTime
+    End Class
+
     ' ── Sync map ──────────────────────────────────────────────────────────────────────
 
     ''' <summary>
@@ -122,7 +138,7 @@ Namespace Sync.SyncMaps
         Public Shared ReadOnly Property Tables As IReadOnlyList(Of String) = New String() {
             "Inv_ProductCategories", "Inv_Products", "Inv_StockBatches",
             "Inv_ShrinkageRecords", "Inv_StockAlertConfigs", "Inv_StockMovements",
-            "Inv_StockAuditRecords"
+            "Inv_StockAuditRecords", "Inv_SaleCogs" ' INFRA-22: Inv_SaleCogs is append-only — LastWriteWins is moot in practice.
         }
 
         Public Shared Function ToRemote(entry As SyncJournal) As Object
@@ -141,6 +157,8 @@ Namespace Sync.SyncMaps
                     Return JsonSerializer.Deserialize(Of RemoteStockMovement)(entry.Payload, _options)
                 Case "Inv_StockAuditRecords"
                     Return JsonSerializer.Deserialize(Of RemoteStockAuditRecord)(entry.Payload, _options)
+                Case "Inv_SaleCogs"
+                    Return JsonSerializer.Deserialize(Of RemoteSaleCogs)(entry.Payload, _options)
                 Case Else
                     Return Nothing
             End Select
```

## What's Next

This module's infrastructure is fully resolved. Subsequent tasks will continue to build upon transaction data sync and BIR auditing.

## Cross-References

- Domain Wiki pages consulted: `LLM_Wiki/wiki/concepts/offline-first-sync.md`, `LLM_Wiki/wiki/concepts/fifo-costing.md`
