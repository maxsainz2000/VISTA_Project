-- Database Reset Script to restore state right after Checklist 1 (Test 1)
-- Dropping triggers to allow clean-up of append-only records created in Checklist 2
DROP TRIGGER IF EXISTS trg_Acc_ExpenseRecords_NoDelete;

-- 1. Delete Shrinkage Records before Stock Batches (FK constraint)
DELETE FROM inv_shrinkagerecords WHERE Id = 3;

-- 2. Delete Stock Batch created for Hybrid Rice RC222
DELETE FROM inv_stockbatches WHERE Id = 4;

-- 3. Delete Accounts Payable entry before PO (FK constraint)
DELETE FROM pur_accountspayable WHERE Id = 3;

-- 4. Delete Goods Receipt lines and header for PO 4
DELETE FROM pur_goodsreceiptlines WHERE GoodsReceiptId = 3;
DELETE FROM pur_goodsreceipts WHERE Id = 3;

-- 5. Delete Purchase Order lines and header for PO 4
DELETE FROM pur_purchaseorderlines WHERE PurchaseOrderId = 4;
DELETE FROM pur_purchaseorders WHERE Id = 4;

-- 6. Delete the new vendor created in Checklist 2 (Test 1.1)
DELETE FROM pur_vendors WHERE Id = 4;

-- 7. Delete the Stock Movements created for Hybrid Rice RC222 (Checklist 2, Test 2.1 & 2.3)
DELETE FROM inv_stockmovements WHERE Id IN (11, 12);

-- 8. Delete the Expense Records created for Hybrid Rice RC222 (Checklist 2, Test 2.1 & 2.3)
DELETE FROM acc_expenserecords WHERE Id IN (12, 13);

-- 9. Recreate the immutability trigger for Acc_ExpenseRecords
DELIMITER $$
CREATE TRIGGER trg_Acc_ExpenseRecords_NoDelete
BEFORE DELETE ON `Acc_ExpenseRecords`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Deletions from Acc_ExpenseRecords are prohibited (ledger immutability)';
END$$
DELIMITER ;

-- 10. Reset the LastUnitCost of Hybrid Rice RC222 back to 0.0000 in the catalog (so Test 2.1 can start fresh)
UPDATE pur_vendorproducts SET LastUnitCost = 0.0000, ModifiedAt = NULL, ModifiedBy = NULL WHERE ProductId = 11;
