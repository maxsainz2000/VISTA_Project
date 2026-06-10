-- ============================================================================
-- 0006_paging_support_indexes.sql
-- INFRA-34 — Server-Side Paging for Unbounded History Grids.
--
-- Adds the secondary indexes that the keyset-paged history reads order/seek on.
-- Without these the ORDER BY ... DESC scans + filesorts the whole table; with
-- them the page read is an index range seek (O(page), not O(table)).
--
-- MariaDB 11.4 LTS supports CREATE INDEX IF NOT EXISTS, so this script is
-- idempotent on its own; the __SchemaMigrations ledger also prevents re-runs.
-- NOTE: once applied, this script's content is frozen — the bootstrap's SHA-256
-- drift check aborts startup if it is edited. Do not modify after first apply.
-- ============================================================================

-- 1. Inv_ShrinkageRecords — ShrinkageService.GetShrinkageHistoryAsync orders by
--    RecordedDate DESC and had no index on it (previously a filesort).
CREATE INDEX IF NOT EXISTS `IX_Inv_ShrinkageRecords_RecordedDate`
    ON `Inv_ShrinkageRecords` (`RecordedDate`);

-- 2. Inv_StockAuditRecords — InventoryAuditService history orders by AuditedAt
--    DESC and had only an index on ProductId.
CREATE INDEX IF NOT EXISTS `IX_Inv_StockAuditRecords_AuditedAt`
    ON `Inv_StockAuditRecords` (`AuditedAt`);

-- 3. Pos_SalesTransactions — the per-account credit history
--    (CreditManagementViewModel) filters CustomerId and orders by TransactionDate
--    DESC. CustomerId was unindexed; this composite supports both the equality
--    filter and the ordered scan in one index.
CREATE INDEX IF NOT EXISTS `IX_Pos_SalesTransactions_CustomerId_TransactionDate`
    ON `Pos_SalesTransactions` (`CustomerId`, `TransactionDate`);
