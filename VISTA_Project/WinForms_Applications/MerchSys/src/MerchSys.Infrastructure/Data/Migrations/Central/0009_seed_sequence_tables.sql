-- Migration: Seed INT-22 sequence tables with existing max values
-- Fixes a duplicate-key checkout regression introduced in INT-22 where the newly created 
-- Pos_TransactionSequences and Pur_OrderSequences tables were not seeded, causing them 
-- to start at NextValue = 1 and collide with existing transactions/orders.

-- 1. Seed POS Transaction Sequence for the current year
INSERT INTO `Pos_TransactionSequences` (`Year`, `NextValue`, `CreatedBy`, `CreatedAt`, `RowVersion`)
SELECT 
    YEAR(CURRENT_DATE),
    COALESCE(MAX(CAST(SUBSTRING_INDEX(`TransactionNumber`, '-', -1) AS UNSIGNED)), 0),
    'System', 
    UTC_TIMESTAMP(6),
    CURRENT_TIMESTAMP(6)
FROM `Pos_SalesTransactions`
WHERE YEAR(`TransactionDate`) = YEAR(CURRENT_DATE)
ON DUPLICATE KEY UPDATE 
    `NextValue` = GREATEST(`NextValue`, VALUES(`NextValue`));

-- 2. Seed Purchasing Order Sequence for the current year (SeqKey format: PO-YYYY)
INSERT INTO `Pur_OrderSequences` (`SeqKey`, `NextValue`, `CreatedBy`, `CreatedAt`, `RowVersion`)
SELECT 
    CONCAT('PO-', YEAR(CURRENT_DATE)),
    COALESCE(MAX(CAST(SUBSTRING_INDEX(`OrderNumber`, '-', -1) AS UNSIGNED)), 0),
    'System', 
    UTC_TIMESTAMP(6),
    CURRENT_TIMESTAMP(6)
FROM `Pur_PurchaseOrders`
WHERE YEAR(`OrderDate`) = YEAR(CURRENT_DATE)
ON DUPLICATE KEY UPDATE 
    `NextValue` = GREATEST(`NextValue`, VALUES(`NextValue`));
