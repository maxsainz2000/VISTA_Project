Imports System.IO
Imports Microsoft.Data.Sqlite

Namespace Data

    ''' <summary>
    ''' Applies all module migrations to the shared SQLite database on first run.
    ''' Used because EF Core 10's migration runner does not discover VB.NET migration classes.
    ''' All CREATE statements use IF NOT EXISTS / INSERT OR IGNORE so re-runs are idempotent.
    ''' </summary>
    Public Module DatabaseInitializer

        Public Sub Initialize(connectionString As String)
            Directory.CreateDirectory(Path.GetDirectoryName(
                connectionString.Replace("Data Source=", "").Trim()))

            Using conn As New SqliteConnection(connectionString)
                conn.Open()
                EnsureMigrationHistory(conn)
                ApplyIfPending(conn, "20260507100001_InitialPurchasing", AddressOf ApplyPurchasing)
                ApplyIfPending(conn, "20260507100002_InitialInventory", AddressOf ApplyInventory)
                ApplyIfPending(conn, "20260507100003_InitialPOS", AddressOf ApplyPOS)
                ApplyIfPending(conn, "20260507100004_InitialAccounting", AddressOf ApplyAccounting)
                ApplyIfPending(conn, "20260509100003_AddStockMovement", AddressOf ApplyStockMovement)
                ApplyIfPending(conn, "20260509100004_AddStockAuditRecords", AddressOf ApplyStockAuditRecords)
                ApplyIfPending(conn, "20260510100000_AddVatLedgerColumns", AddressOf ApplyVatLedgerColumns)
                ApplyIfPending(conn, "20260510100005_AddSyncJournal", AddressOf ApplySyncJournal)
                ApplyIfPending(conn, "20260510120000_AddBirRetentionConstraints", AddressOf ApplyBirRetentionConstraints)
                ApplyIfPending(conn, "20260514100000_AddVatThreeBucketColumns", AddressOf ApplyVatThreeBucketColumns)
                ApplyIfPending(conn, "20260515100000_FixVatReturnAmendedIndex", AddressOf ApplyFixVatReturnAmendedIndex)
                ApplyIfPending(conn, "20260516100000_AddTamperAuditLog", AddressOf ApplyTamperAuditLog)
            End Using
        End Sub

        ' ── Infrastructure ────────────────────────────────────────────────────

        Private Sub EnsureMigrationHistory(conn As SqliteConnection)
            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (" &
                """MigrationId"" TEXT NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY, " &
                """ProductVersion"" TEXT NOT NULL)")
        End Sub

        Private Sub ApplyIfPending(conn As SqliteConnection, migrationId As String,
                                   applyAction As Action(Of SqliteConnection))
            If IsMigrationApplied(conn, migrationId) Then Return

            Using tx = conn.BeginTransaction()
                applyAction(conn)
                tx.Commit()
            End Using

            Exec(conn,
                "INSERT OR IGNORE INTO ""__EFMigrationsHistory"" " &
                "(""MigrationId"", ""ProductVersion"") VALUES " &
                $"('{migrationId}', '10.0.7')")
        End Sub

        Private Function IsMigrationApplied(conn As SqliteConnection, migrationId As String) As Boolean
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "SELECT COUNT(*) FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = @id"
                cmd.Parameters.AddWithValue("@id", migrationId)
                Return CInt(cmd.ExecuteScalar()) > 0
            End Using
        End Function

        Private Sub Exec(conn As SqliteConnection, sql As String)
            Using cmd = conn.CreateCommand()
                cmd.CommandText = sql
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        ' ── Purchasing (20260507100001) ───────────────────────────────────────

        Private Sub ApplyPurchasing(conn As SqliteConnection)
            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pur_Vendors"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pur_Vendors"" PRIMARY KEY AUTOINCREMENT, " &
                """Name"" TEXT NOT NULL, " &
                """ContactPerson"" TEXT NOT NULL, " &
                """Phone"" TEXT NOT NULL, " &
                """Email"" TEXT NULL, " &
                """Address"" TEXT NOT NULL, " &
                """DefaultLeadTimeDays"" INTEGER NOT NULL, " &
                """Notes"" TEXT NULL, " &
                """IsDeleted"" INTEGER NOT NULL, " &
                """DeletedBy"" TEXT NULL, " &
                """DeletedAt"" TEXT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Pur_Vendors_Name"" ON ""Pur_Vendors"" (""Name"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pur_PurchaseOrders"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pur_PurchaseOrders"" PRIMARY KEY AUTOINCREMENT, " &
                """OrderNumber"" TEXT NOT NULL, " &
                """VendorId"" INTEGER NOT NULL, " &
                """Status"" INTEGER NOT NULL, " &
                """OrderDate"" TEXT NOT NULL, " &
                """ExpectedDeliveryDate"" TEXT NULL, " &
                """TotalAmount"" TEXT NOT NULL, " &
                """Notes"" TEXT NULL, " &
                """IsDeleted"" INTEGER NOT NULL, " &
                """DeletedBy"" TEXT NULL, " &
                """DeletedAt"" TEXT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Pur_PurchaseOrders_OrderNumber"" ON ""Pur_PurchaseOrders"" (""OrderNumber"")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pur_PurchaseOrders_VendorId"" ON ""Pur_PurchaseOrders"" (""VendorId"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pur_PurchaseOrderLines"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pur_PurchaseOrderLines"" PRIMARY KEY AUTOINCREMENT, " &
                """PurchaseOrderId"" INTEGER NOT NULL, " &
                """ProductId"" INTEGER NOT NULL, " &
                """ProductName"" TEXT NOT NULL, " &
                """QuantityOrdered"" INTEGER NOT NULL, " &
                """UnitCost"" TEXT NOT NULL, " &
                """LineTotal"" TEXT NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pur_PurchaseOrderLines_PurchaseOrderId"" ON ""Pur_PurchaseOrderLines"" (""PurchaseOrderId"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pur_GoodsReceipts"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pur_GoodsReceipts"" PRIMARY KEY AUTOINCREMENT, " &
                """PurchaseOrderId"" INTEGER NOT NULL, " &
                """ReceiptNumber"" TEXT NOT NULL, " &
                """ReceivedDate"" TEXT NOT NULL, " &
                """ReceivedBy"" TEXT NULL, " &
                """Notes"" TEXT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Pur_GoodsReceipts_ReceiptNumber"" ON ""Pur_GoodsReceipts"" (""ReceiptNumber"")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pur_GoodsReceipts_PurchaseOrderId"" ON ""Pur_GoodsReceipts"" (""PurchaseOrderId"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pur_GoodsReceiptLines"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pur_GoodsReceiptLines"" PRIMARY KEY AUTOINCREMENT, " &
                """GoodsReceiptId"" INTEGER NOT NULL, " &
                """ProductId"" INTEGER NOT NULL, " &
                """ProductName"" TEXT NOT NULL, " &
                """QuantityOrdered"" INTEGER NOT NULL, " &
                """QuantityReceived"" INTEGER NOT NULL, " &
                """UnitCost"" TEXT NOT NULL, " &
                """ExpiryDate"" TEXT NULL, " &
                """HasDiscrepancy"" INTEGER NOT NULL, " &
                """DiscrepancyNotes"" TEXT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pur_GoodsReceiptLines_GoodsReceiptId"" ON ""Pur_GoodsReceiptLines"" (""GoodsReceiptId"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pur_AccountsPayable"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pur_AccountsPayable"" PRIMARY KEY AUTOINCREMENT, " &
                """PurchaseOrderId"" INTEGER NOT NULL, " &
                """VendorId"" INTEGER NOT NULL, " &
                """InvoiceNumber"" TEXT NULL, " &
                """InvoiceDate"" TEXT NOT NULL, " &
                """DueDate"" TEXT NOT NULL, " &
                """TotalAmount"" TEXT NOT NULL, " &
                """AmountPaid"" TEXT NOT NULL, " &
                """Balance"" TEXT NOT NULL, " &
                """IsPaid"" INTEGER NOT NULL, " &
                """Notes"" TEXT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pur_AccountsPayable_VendorId_IsPaid"" ON ""Pur_AccountsPayable"" (""VendorId"", ""IsPaid"")")
            Exec(conn, "CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Pur_AccountsPayable_PurchaseOrderId"" ON ""Pur_AccountsPayable"" (""PurchaseOrderId"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pur_ReorderConfigs"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pur_ReorderConfigs"" PRIMARY KEY AUTOINCREMENT, " &
                """ProductId"" INTEGER NOT NULL, " &
                """ProductName"" TEXT NOT NULL, " &
                """PreferredVendorId"" INTEGER NULL, " &
                """MinimumThreshold"" INTEGER NOT NULL, " &
                """SafetyStock"" INTEGER NOT NULL, " &
                """DefaultOrderQuantity"" INTEGER NOT NULL, " &
                """LeadTimeDays"" INTEGER NOT NULL, " &
                """IsSeasonalItem"" INTEGER NOT NULL, " &
                """SeasonalMultiplier"" TEXT NOT NULL, " &
                """IsActive"" INTEGER NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Pur_ReorderConfigs_ProductId"" ON ""Pur_ReorderConfigs"" (""ProductId"")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pur_ReorderConfigs_IsActive"" ON ""Pur_ReorderConfigs"" (""IsActive"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pur_ReorderSuggestions"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pur_ReorderSuggestions"" PRIMARY KEY AUTOINCREMENT, " &
                """ProductId"" INTEGER NOT NULL, " &
                """ProductName"" TEXT NOT NULL, " &
                """CurrentStock"" INTEGER NOT NULL, " &
                """ReorderPoint"" INTEGER NOT NULL, " &
                """SuggestedQuantity"" INTEGER NOT NULL, " &
                """PreferredVendorId"" INTEGER NULL, " &
                """PreferredVendorName"" TEXT NULL, " &
                """EstimatedLeadTimeDays"" INTEGER NOT NULL, " &
                """IsSeasonalAdjusted"" INTEGER NOT NULL, " &
                """Status"" TEXT NOT NULL, " &
                """ConvertedToPOId"" INTEGER NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pur_ReorderSuggestions_Status"" ON ""Pur_ReorderSuggestions"" (""Status"")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pur_ReorderSuggestions_ProductId_Status"" ON ""Pur_ReorderSuggestions"" (""ProductId"", ""Status"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pur_PriceChangeAlerts"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pur_PriceChangeAlerts"" PRIMARY KEY AUTOINCREMENT, " &
                """ProductId"" INTEGER NOT NULL, " &
                """ProductName"" TEXT NOT NULL, " &
                """VendorId"" INTEGER NOT NULL, " &
                """VendorName"" TEXT NOT NULL, " &
                """PreviousUnitCost"" TEXT NOT NULL, " &
                """NewUnitCost"" TEXT NOT NULL, " &
                """ChangePercent"" TEXT NOT NULL, " &
                """ChangeDirection"" TEXT NOT NULL, " &
                """GoodsReceiptId"" INTEGER NOT NULL, " &
                """IsAcknowledged"" INTEGER NOT NULL, " &
                """AcknowledgedAt"" TEXT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pur_PriceChangeAlerts_IsAcknowledged"" ON ""Pur_PriceChangeAlerts"" (""IsAcknowledged"")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pur_PriceChangeAlerts_ProductId"" ON ""Pur_PriceChangeAlerts"" (""ProductId"")")

            Exec(conn,
                "INSERT OR IGNORE INTO ""Pur_Vendors"" " &
                "(""Id"",""Name"",""ContactPerson"",""Phone"",""Email"",""Address"",""DefaultLeadTimeDays""," &
                """Notes"",""IsDeleted"",""DeletedBy"",""DeletedAt"",""CreatedBy"",""CreatedAt"",""ModifiedBy"",""ModifiedAt"") VALUES " &
                "(1,'AgriChem Supplies','Juan dela Cruz','09171234567','sales@agrichemsupplies.ph'," &
                "'123 Magsaysay Ave, Cagayan de Oro City',5,'Pesticides and chemicals supplier.'," &
                "0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00')," &
                "(2,'FarmFresh Seeds Corp.','Maria Santos','09189876543','orders@farmfreshseeds.ph'," &
                "'456 National Highway, Bukidnon',7,'Seeds and planting materials supplier.'," &
                "0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00')," &
                "(3,'Golden Feeds Trading','Pedro Reyes','09205551234','info@goldenfeedstrading.ph'," &
                "'789 Rizal Street, Iligan City',3,'Animal feeds and livestock supplies.'," &
                "0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00')")
        End Sub

        ' ── Inventory (20260507100002) ────────────────────────────────────────

        Private Sub ApplyInventory(conn As SqliteConnection)
            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Inv_ProductCategories"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Inv_ProductCategories"" PRIMARY KEY AUTOINCREMENT, " &
                """Name"" TEXT NOT NULL, " &
                """Description"" TEXT NULL, " &
                """IsDeleted"" INTEGER NOT NULL, " &
                """DeletedBy"" TEXT NULL, " &
                """DeletedAt"" TEXT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Inv_ProductCategories_Name"" ON ""Inv_ProductCategories"" (""Name"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Inv_Products"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Inv_Products"" PRIMARY KEY AUTOINCREMENT, " &
                """Name"" TEXT NOT NULL, " &
                """Sku"" TEXT NOT NULL, " &
                """CategoryId"" INTEGER NOT NULL, " &
                """Description"" TEXT NULL, " &
                """RetailPrice"" TEXT NOT NULL, " &
                """Unit"" TEXT NULL, " &
                """HasExpiry"" INTEGER NOT NULL, " &
                """MinimumThreshold"" INTEGER NOT NULL, " &
                """IsActive"" INTEGER NOT NULL, " &
                """IsDeleted"" INTEGER NOT NULL, " &
                """DeletedBy"" TEXT NULL, " &
                """DeletedAt"" TEXT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL, " &
                "CONSTRAINT ""FK_Inv_Products_CategoryId"" FOREIGN KEY (""CategoryId"") REFERENCES ""Inv_ProductCategories"" (""Id"") ON DELETE RESTRICT" &
                ")")
            Exec(conn, "CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Inv_Products_Sku"" ON ""Inv_Products"" (""Sku"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Inv_StockBatches"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Inv_StockBatches"" PRIMARY KEY AUTOINCREMENT, " &
                """ProductId"" INTEGER NOT NULL, " &
                """QuantityReceived"" INTEGER NOT NULL, " &
                """QuantityRemaining"" INTEGER NOT NULL, " &
                """UnitCost"" TEXT NOT NULL, " &
                """ReceiptDate"" TEXT NOT NULL, " &
                """ExpiryDate"" TEXT NULL, " &
                """SourcePurchaseOrderId"" INTEGER NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Inv_StockBatches_ProductId_ReceiptDate"" ON ""Inv_StockBatches"" (""ProductId"", ""ReceiptDate"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Inv_ShrinkageRecords"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Inv_ShrinkageRecords"" PRIMARY KEY AUTOINCREMENT, " &
                """ProductId"" INTEGER NOT NULL, " &
                """StockBatchId"" INTEGER NULL, " &
                """QuantityLost"" INTEGER NOT NULL, " &
                """UnitCost"" TEXT NOT NULL, " &
                """TotalValue"" TEXT NOT NULL, " &
                """Reason"" TEXT NOT NULL, " &
                """Notes"" TEXT NULL, " &
                """RecordedDate"" TEXT NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Inv_StockAlertConfigs"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Inv_StockAlertConfigs"" PRIMARY KEY AUTOINCREMENT, " &
                """ProductId"" INTEGER NOT NULL, " &
                """MinimumThreshold"" INTEGER NOT NULL, " &
                """ExpiryAlertDays"" INTEGER NOT NULL, " &
                """IsAlertEnabled"" INTEGER NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")

            Exec(conn,
                "INSERT OR IGNORE INTO ""Inv_ProductCategories"" " &
                "(""Id"",""Name"",""Description"",""IsDeleted"",""DeletedBy"",""DeletedAt"",""CreatedBy"",""CreatedAt"",""ModifiedBy"",""ModifiedAt"") VALUES " &
                "(1,'Fertilizers','Soil nutrients and plant growth supplements.',0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00')," &
                "(2,'Pesticides/Chemicals','Insecticides, herbicides, and fungicides.',0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00')," &
                "(3,'Seeds','Planting seeds for various crops.',0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00')," &
                "(4,'Animal Feeds','Feeds for hogs, poultry, and aquaculture.',0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00')")

            Exec(conn,
                "INSERT OR IGNORE INTO ""Inv_Products"" " &
                "(""Id"",""CategoryId"",""Name"",""Sku"",""Unit"",""RetailPrice"",""HasExpiry"",""MinimumThreshold"",""IsActive"",""IsDeleted"",""DeletedBy"",""DeletedAt"",""CreatedBy"",""CreatedAt"",""ModifiedBy"",""ModifiedAt"",""Description"") VALUES " &
                "(1,1,'Complete Fertilizer 14-14-14','FERT-001','bag','1250',0,10,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(2,1,'Urea 46-0-0','FERT-002','bag','1450',0,10,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(3,1,'Ammonium Sulfate','FERT-003','bag','1100',0,10,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(4,1,'Muriate of Potash 0-0-60','FERT-004','bag','1600',0,5,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(5,1,'Calcium Nitrate','FERT-005','bag','1800',0,5,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(6,2,'Malathion 57 EC','PEST-001','bottle','480',1,5,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(7,2,'Cypermethrin 10 EC','PEST-002','bottle','520',1,5,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(8,2,'Lambda-Cyhalothrin 2.5 EC','PEST-003','bottle','650',1,5,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(9,2,'Glyphosate 48 SL','PEST-004','bottle','390',1,5,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(10,2,'Mancozeb 80 WP','PEST-005','pack','280',1,10,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(11,3,'Hybrid Rice RC222','SEED-001','kg','1950',1,5,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(12,3,'Corn Yellow Hybrid','SEED-002','kg','1200',1,5,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(13,3,'Eggplant Seeds','SEED-003','pack','85',1,10,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(14,3,'Tomato Seeds','SEED-004','pack','95',1,10,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(15,3,'Ampalaya Seeds','SEED-005','pack','75',1,10,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(16,4,'Hog Grower Pellets','FEED-001','bag','1350',1,10,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(17,4,'Poultry Layer Mash','FEED-002','bag','1280',1,10,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(18,4,'Hog Starter Crumble','FEED-003','bag','1420',1,5,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(19,4,'Broiler Starter Mash','FEED-004','bag','1300',1,5,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)," &
                "(20,4,'Tilapia Pellets','FEED-005','bag','980',1,5,1,0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00',NULL)")
        End Sub

        ' ── POS (20260507100003) ──────────────────────────────────────────────

        Private Sub ApplyPOS(conn As SqliteConnection)
            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pos_CreditAccounts"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pos_CreditAccounts"" PRIMARY KEY AUTOINCREMENT, " &
                """CustomerName"" TEXT NOT NULL, " &
                """Phone"" TEXT NULL, " &
                """Address"" TEXT NULL, " &
                """CurrentBalance"" TEXT NOT NULL, " &
                """TotalCreditExtended"" TEXT NOT NULL, " &
                """TotalPaymentsReceived"" TEXT NOT NULL, " &
                """IsBlocked"" INTEGER NOT NULL, " &
                """LastTransactionDate"" TEXT NULL, " &
                """Notes"" TEXT NULL, " &
                """IsDeleted"" INTEGER NOT NULL, " &
                """DeletedBy"" TEXT NULL, " &
                """DeletedAt"" TEXT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pos_CreditAccounts_IsBlocked"" ON ""Pos_CreditAccounts"" (""IsBlocked"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pos_SalesTransactions"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pos_SalesTransactions"" PRIMARY KEY AUTOINCREMENT, " &
                """TransactionNumber"" TEXT NOT NULL, " &
                """TransactionDate"" TEXT NOT NULL, " &
                """CustomerId"" INTEGER NULL, " &
                """CustomerName"" TEXT NULL, " &
                """PaymentMethod"" INTEGER NOT NULL, " &
                """SubTotal"" TEXT NOT NULL, " &
                """DiscountAmount"" TEXT NOT NULL, " &
                """VatAmount"" TEXT NOT NULL, " &
                """TotalAmount"" TEXT NOT NULL, " &
                """AmountTendered"" TEXT NOT NULL, " &
                """ChangeAmount"" TEXT NOT NULL, " &
                """IsVoided"" INTEGER NOT NULL, " &
                """VoidReason"" TEXT NULL, " &
                """CreditAccountId"" INTEGER NULL, " &
                """IsDeleted"" INTEGER NOT NULL, " &
                """DeletedBy"" TEXT NULL, " &
                """DeletedAt"" TEXT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Pos_SalesTransactions_TransactionNumber"" ON ""Pos_SalesTransactions"" (""TransactionNumber"")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pos_SalesTransactions_TransactionDate"" ON ""Pos_SalesTransactions"" (""TransactionDate"")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pos_SalesTransactions_CreditAccountId"" ON ""Pos_SalesTransactions"" (""CreditAccountId"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pos_SalesTransactionLines"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pos_SalesTransactionLines"" PRIMARY KEY AUTOINCREMENT, " &
                """TransactionId"" INTEGER NOT NULL, " &
                """ProductId"" INTEGER NOT NULL, " &
                """ProductName"" TEXT NOT NULL, " &
                """Quantity"" INTEGER NOT NULL, " &
                """UnitPrice"" TEXT NOT NULL, " &
                """DiscountAmount"" TEXT NOT NULL, " &
                """LineTotal"" TEXT NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pos_SalesTransactionLines_TransactionId"" ON ""Pos_SalesTransactionLines"" (""TransactionId"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pos_OfficialReceipts"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pos_OfficialReceipts"" PRIMARY KEY AUTOINCREMENT, " &
                """TransactionId"" INTEGER NOT NULL, " &
                """ReceiptNumber"" TEXT NOT NULL, " &
                """BusinessName"" TEXT NOT NULL, " &
                """BusinessAddress"" TEXT NULL, " &
                """BusinessTIN"" TEXT NULL, " &
                """IssueDate"" TEXT NOT NULL, " &
                """Items"" TEXT NULL, " &
                """TotalAmount"" TEXT NOT NULL, " &
                """VatAmount"" TEXT NOT NULL, " &
                """IsVatRegistered"" INTEGER NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Pos_OfficialReceipts_ReceiptNumber"" ON ""Pos_OfficialReceipts"" (""ReceiptNumber"")")
            Exec(conn, "CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Pos_OfficialReceipts_TransactionId"" ON ""Pos_OfficialReceipts"" (""TransactionId"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pos_CreditPayments"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pos_CreditPayments"" PRIMARY KEY AUTOINCREMENT, " &
                """CreditAccountId"" INTEGER NOT NULL, " &
                """PaymentAmount"" TEXT NOT NULL, " &
                """PaymentDate"" TEXT NOT NULL, " &
                """PaymentMethod"" INTEGER NOT NULL, " &
                """Notes"" TEXT NULL, " &
                """ReceivedBy"" TEXT NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pos_CreditPayments_CreditAccountId"" ON ""Pos_CreditPayments"" (""CreditAccountId"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pos_SalesReturns"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pos_SalesReturns"" PRIMARY KEY AUTOINCREMENT, " &
                """OriginalTransactionId"" INTEGER NOT NULL, " &
                """ReturnDate"" TEXT NOT NULL, " &
                """ProductId"" INTEGER NOT NULL, " &
                """ProductName"" TEXT NOT NULL, " &
                """QuantityReturned"" INTEGER NOT NULL, " &
                """UnitPrice"" TEXT NOT NULL, " &
                """RefundAmount"" TEXT NOT NULL, " &
                """Reason"" TEXT NOT NULL, " &
                """IsRestocked"" INTEGER NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pos_SalesReturns_OriginalTransactionId"" ON ""Pos_SalesReturns"" (""OriginalTransactionId"")")

            Exec(conn,
                "INSERT OR IGNORE INTO ""Pos_CreditAccounts"" " &
                "(""Id"",""CustomerName"",""Phone"",""Address"",""Notes"",""CurrentBalance"",""TotalCreditExtended""," &
                """TotalPaymentsReceived"",""IsBlocked"",""IsDeleted"",""DeletedBy"",""DeletedAt"",""CreatedBy"",""CreatedAt"",""ModifiedBy"",""ModifiedAt"",""LastTransactionDate"") VALUES " &
                "(1,'Juan Dela Cruz','','','Farmer','0','0','0',0,0,NULL,NULL,'System','2026-01-01 00:00:00','System',NULL,NULL)," &
                "(2,'Maria Santos','','','Farmer','500','500','0',1,0,NULL,NULL,'System','2026-01-01 00:00:00','System',NULL,NULL)," &
                "(3,'Pedro Reyes','','','Farmer','0','0','0',0,0,NULL,NULL,'System','2026-01-01 00:00:00','System',NULL,NULL)")
        End Sub

        ' ── StockMovement (20260509100003) ───────────────────────────────────────

        Private Sub ApplyStockMovement(conn As SqliteConnection)
            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Inv_StockMovements"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Inv_StockMovements"" PRIMARY KEY AUTOINCREMENT, " &
                """ProductId"" INTEGER NOT NULL, " &
                """MovementType"" TEXT NOT NULL, " &
                """Quantity"" INTEGER NOT NULL, " &
                """OccurredAt"" TEXT NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL, " &
                "CONSTRAINT ""FK_Inv_StockMovements_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Inv_Products"" (""Id"") ON DELETE RESTRICT" &
                ")")
            Exec(conn,
                "CREATE INDEX IF NOT EXISTS ""IX_Inv_StockMovements_ProductId_OccurredAt"" " &
                "ON ""Inv_StockMovements"" (""ProductId"", ""OccurredAt"")")
        End Sub

        ' ── StockAuditRecords (20260509100004) ───────────────────────────────────────

        Private Sub ApplyStockAuditRecords(conn As SqliteConnection)
            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Inv_StockAuditRecords"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Inv_StockAuditRecords"" PRIMARY KEY AUTOINCREMENT, " &
                """ProductId"" INTEGER NOT NULL, " &
                """ExpectedQuantity"" INTEGER NOT NULL, " &
                """PhysicalCount"" INTEGER NOT NULL, " &
                """Variance"" INTEGER NOT NULL, " &
                """Reason"" TEXT NOT NULL, " &
                """Notes"" TEXT NULL, " &
                """PerformedBy"" TEXT NOT NULL, " &
                """AuditedAt"" TEXT NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL, " &
                "CONSTRAINT ""FK_Inv_StockAuditRecords_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Inv_Products"" (""Id"") ON DELETE RESTRICT" &
                ")")
            Exec(conn,
                "CREATE INDEX IF NOT EXISTS ""IX_Inv_StockAuditRecords_ProductId"" " &
                "ON ""Inv_StockAuditRecords"" (""ProductId"")")
        End Sub

        ' ── SyncJournal (20260510100005) ─────────────────────────────────────

        Private Sub ApplySyncJournal(conn As SqliteConnection)
            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Sync_Journal"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Sync_Journal"" PRIMARY KEY AUTOINCREMENT, " &
                """TableName"" TEXT NOT NULL, " &
                """RowId"" INTEGER NOT NULL, " &
                """Operation"" TEXT NOT NULL, " &
                """Payload"" TEXT NULL, " &
                """AttemptCount"" INTEGER NOT NULL DEFAULT 0, " &
                """LastError"" TEXT NULL, " &
                """SyncedAt"" TEXT NULL, " &
                """ModuleName"" TEXT NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn,
                "CREATE INDEX IF NOT EXISTS ""IX_Sync_Journal_ModuleName_SyncedAt"" " &
                "ON ""Sync_Journal"" (""ModuleName"", ""SyncedAt"")")
        End Sub

        ' ── Accounting (20260507100004) ───────────────────────────────────────

        Private Sub ApplyAccounting(conn As SqliteConnection)
            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Acc_FinancialPeriods"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Acc_FinancialPeriods"" PRIMARY KEY AUTOINCREMENT, " &
                """PeriodType"" TEXT NOT NULL, " &
                """StartDate"" TEXT NOT NULL, " &
                """EndDate"" TEXT NOT NULL, " &
                """TotalRevenue"" TEXT NOT NULL, " &
                """TotalCOGS"" TEXT NOT NULL, " &
                """GrossProfit"" TEXT NOT NULL, " &
                """GrossMarginPercent"" TEXT NOT NULL, " &
                """TotalExpenses"" TEXT NOT NULL, " &
                """NetIncome"" TEXT NOT NULL, " &
                """IsClosed"" INTEGER NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Acc_RevenueRecords"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Acc_RevenueRecords"" PRIMARY KEY AUTOINCREMENT, " &
                """RecordDate"" TEXT NOT NULL, " &
                """SourceTransactionId"" INTEGER NOT NULL, " &
                """PaymentMethod"" INTEGER NOT NULL, " &
                """GrossAmount"" TEXT NOT NULL, " &
                """DiscountAmount"" TEXT NOT NULL, " &
                """NetAmount"" TEXT NOT NULL, " &
                """VatAmount"" TEXT NOT NULL, " &
                """ProductId"" INTEGER NOT NULL, " &
                """ProductName"" TEXT NOT NULL, " &
                """QuantitySold"" INTEGER NOT NULL, " &
                """COGS"" TEXT NOT NULL, " &
                """GrossProfit"" TEXT NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Acc_RevenueRecords_RecordDate"" ON ""Acc_RevenueRecords"" (""RecordDate"")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Acc_RevenueRecords_ProductId"" ON ""Acc_RevenueRecords"" (""ProductId"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Acc_ExpenseRecords"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Acc_ExpenseRecords"" PRIMARY KEY AUTOINCREMENT, " &
                """RecordDate"" TEXT NOT NULL, " &
                """Category"" TEXT NOT NULL, " &
                """Description"" TEXT NULL, " &
                """Amount"" TEXT NOT NULL, " &
                """SourceModule"" TEXT NOT NULL, " &
                """SourceReferenceId"" INTEGER NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Acc_FinancialSnapshots"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Acc_FinancialSnapshots"" PRIMARY KEY AUTOINCREMENT, " &
                """SnapshotDate"" TEXT NOT NULL, " &
                """TotalAR"" TEXT NOT NULL, " &
                """TotalAP"" TEXT NOT NULL, " &
                """InventoryValue"" TEXT NOT NULL, " &
                """TodayRevenue"" TEXT NOT NULL, " &
                """MonthToDateRevenue"" TEXT NOT NULL, " &
                """YearToDateRevenue"" TEXT NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Acc_FinancialSnapshots_SnapshotDate"" ON ""Acc_FinancialSnapshots"" (""SnapshotDate"")")
        End Sub

        ' ── BIR Retention Constraints (20260510120000) ────────────────────────

        Private Sub ApplyBirRetentionConstraints(conn As SqliteConnection)
            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pos_ReceiptSequence"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pos_ReceiptSequence"" PRIMARY KEY AUTOINCREMENT, " &
                """Year"" INTEGER NOT NULL, " &
                """NextValue"" INTEGER NOT NULL, " &
                """RowVersion"" BLOB NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Pos_ReceiptSequence_Year"" ON ""Pos_ReceiptSequence"" (""Year"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pos_ReceiptIntegrity"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pos_ReceiptIntegrity"" PRIMARY KEY AUTOINCREMENT, " &
                """ReceiptId"" INTEGER NOT NULL, " &
                """IntegrityHash"" TEXT NOT NULL, " &
                """PreviousHash"" TEXT NOT NULL, " &
                """RetentionExpiresAt"" TEXT NOT NULL, " &
                """IsImmutable"" INTEGER NOT NULL, " &
                """HashAlgorithm"" TEXT NOT NULL, " &
                """CanonicalPayload"" TEXT NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL, " &
                "CONSTRAINT ""FK_Pos_ReceiptIntegrity_Pos_OfficialReceipts_ReceiptId"" " &
                "FOREIGN KEY (""ReceiptId"") REFERENCES ""Pos_OfficialReceipts"" (""Id"")" &
                ")")
            Exec(conn, "CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Pos_ReceiptIntegrity_ReceiptId"" ON ""Pos_ReceiptIntegrity"" (""ReceiptId"")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pos_ReceiptIntegrity_RetentionExpiresAt"" ON ""Pos_ReceiptIntegrity"" (""RetentionExpiresAt"")")

            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pos_OfficialReceiptArchive"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pos_OfficialReceiptArchive"" PRIMARY KEY AUTOINCREMENT, " &
                """OriginalReceiptId"" INTEGER NOT NULL, " &
                """TransactionId"" INTEGER NOT NULL, " &
                """ReceiptNumber"" TEXT NOT NULL, " &
                """BusinessName"" TEXT NOT NULL, " &
                """BusinessAddress"" TEXT NULL, " &
                """BusinessTIN"" TEXT NULL, " &
                """IssueDate"" TEXT NOT NULL, " &
                """Items"" TEXT NULL, " &
                """TotalAmount"" TEXT NOT NULL, " &
                """VatAmount"" TEXT NOT NULL, " &
                """IsVatRegistered"" INTEGER NOT NULL, " &
                """ArchivedAt"" TEXT NOT NULL, " &
                """ArchivedHash"" TEXT NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")
            Exec(conn, "CREATE INDEX IF NOT EXISTS ""IX_Pos_OfficialReceiptArchive_OriginalReceiptId"" ON ""Pos_OfficialReceiptArchive"" (""OriginalReceiptId"")")

            Exec(conn,
                "CREATE TRIGGER IF NOT EXISTS pos_receipts_no_update " &
                "BEFORE UPDATE ON ""Pos_OfficialReceipts"" " &
                "BEGIN " &
                "SELECT RAISE(ABORT, 'BIR-immutable'); " &
                "END")

            Exec(conn,
                "CREATE TRIGGER IF NOT EXISTS pos_receipts_no_delete " &
                "BEFORE DELETE ON ""Pos_OfficialReceipts"" " &
                "BEGIN " &
                "SELECT RAISE(ABORT, 'BIR-immutable'); " &
                "END")
        End Sub

        ' ── VAT Ledger Columns & Return Tables (20260510100000) ─────────────────

        Private Sub ApplyVatLedgerColumns(conn As SqliteConnection)
            ' Add BIR three-bucket VAT columns to Acc_RevenueRecords
            Exec(conn, "ALTER TABLE ""Acc_RevenueRecords"" ADD COLUMN ""VatableAmount"" TEXT NOT NULL DEFAULT '0'")
            Exec(conn, "ALTER TABLE ""Acc_RevenueRecords"" ADD COLUMN ""VatExemptAmount"" TEXT NOT NULL DEFAULT '0'")
            Exec(conn, "ALTER TABLE ""Acc_RevenueRecords"" ADD COLUMN ""ZeroRatedAmount"" TEXT NOT NULL DEFAULT '0'")
            Exec(conn, "ALTER TABLE ""Acc_RevenueRecords"" ADD COLUMN ""OutputVat"" TEXT NOT NULL DEFAULT '0'")
            Exec(conn, "ALTER TABLE ""Acc_RevenueRecords"" ADD COLUMN ""InputVat"" TEXT NOT NULL DEFAULT '0'")
            Exec(conn, "ALTER TABLE ""Acc_RevenueRecords"" ADD COLUMN ""VatTreatment"" INTEGER NOT NULL DEFAULT 0")
            Exec(conn, "UPDATE ""Acc_RevenueRecords"" SET ""VatableAmount"" = ""NetAmount"" WHERE ""VatableAmount"" = '0'")

            ' Add BIR three-bucket VAT columns to Acc_ExpenseRecords
            Exec(conn, "ALTER TABLE ""Acc_ExpenseRecords"" ADD COLUMN ""VatableAmount"" TEXT NOT NULL DEFAULT '0'")
            Exec(conn, "ALTER TABLE ""Acc_ExpenseRecords"" ADD COLUMN ""VatExemptAmount"" TEXT NOT NULL DEFAULT '0'")
            Exec(conn, "ALTER TABLE ""Acc_ExpenseRecords"" ADD COLUMN ""ZeroRatedAmount"" TEXT NOT NULL DEFAULT '0'")
            Exec(conn, "ALTER TABLE ""Acc_ExpenseRecords"" ADD COLUMN ""OutputVat"" TEXT NOT NULL DEFAULT '0'")
            Exec(conn, "ALTER TABLE ""Acc_ExpenseRecords"" ADD COLUMN ""InputVat"" TEXT NOT NULL DEFAULT '0'")
            Exec(conn, "ALTER TABLE ""Acc_ExpenseRecords"" ADD COLUMN ""VatTreatment"" INTEGER NOT NULL DEFAULT 0")
            Exec(conn, "UPDATE ""Acc_ExpenseRecords"" SET ""VatableAmount"" = ""Amount"" WHERE ""VatableAmount"" = '0'")

            ' Create Acc_VatReturns
            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Acc_VatReturns"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Acc_VatReturns"" PRIMARY KEY AUTOINCREMENT, " &
                """Year"" INTEGER NOT NULL, " &
                """Period"" INTEGER NOT NULL, " &
                """PeriodType"" INTEGER NOT NULL, " &
                """FormType"" INTEGER NOT NULL, " &
                """TotalVatableSales"" TEXT NOT NULL, " &
                """TotalVatExemptSales"" TEXT NOT NULL, " &
                """TotalZeroRatedSales"" TEXT NOT NULL, " &
                """TotalOutputVat"" TEXT NOT NULL, " &
                """TotalVatablePurchases"" TEXT NOT NULL, " &
                """TotalInputVat"" TEXT NOT NULL, " &
                """VatPayable"" TEXT NOT NULL, " &
                """FilingStatus"" INTEGER NOT NULL, " &
                """FiledAt"" TEXT NULL, " &
                """FiledBy"" TEXT NULL, " &
                """GeneratedAt"" TEXT NOT NULL, " &
                """IsVatRegisteredSnapshot"" INTEGER NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")

            Exec(conn,
                "CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Acc_VatReturns_Year_Period_PeriodType_FormType"" " &
                "ON ""Acc_VatReturns"" (""Year"", ""Period"", ""PeriodType"", ""FormType"")")

            ' Create Acc_VatReturnLines
            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Acc_VatReturnLines"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Acc_VatReturnLines"" PRIMARY KEY AUTOINCREMENT, " &
                """VatReturnId"" INTEGER NOT NULL, " &
                """SourceModule"" TEXT NOT NULL, " &
                """SourceTable"" TEXT NOT NULL, " &
                """SourceRowId"" INTEGER NOT NULL, " &
                """TransactionDate"" TEXT NOT NULL, " &
                """VatableAmount"" TEXT NOT NULL, " &
                """VatExemptAmount"" TEXT NOT NULL, " &
                """ZeroRatedAmount"" TEXT NOT NULL, " &
                """OutputVat"" TEXT NOT NULL, " &
                """InputVat"" TEXT NOT NULL, " &
                """Treatment"" INTEGER NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL, " &
                "CONSTRAINT ""FK_Acc_VatReturnLines_Acc_VatReturns_VatReturnId"" " &
                "FOREIGN KEY (""VatReturnId"") REFERENCES ""Acc_VatReturns"" (""Id"") ON DELETE CASCADE" &
                ")")

            Exec(conn,
                "CREATE INDEX IF NOT EXISTS ""IX_Acc_VatReturnLines_VatReturnId"" " &
                "ON ""Acc_VatReturnLines"" (""VatReturnId"")")

            Exec(conn,
                "CREATE INDEX IF NOT EXISTS ""IX_Acc_VatReturnLines_SourceModule_SourceTable_SourceRowId"" " &
                "ON ""Acc_VatReturnLines"" (""SourceModule"", ""SourceTable"", ""SourceRowId"")")
        End Sub

        ' ── Fix VAT Return Amended Index (20260515100000) ─────────────────────────

        Private Sub ApplyFixVatReturnAmendedIndex(conn As SqliteConnection)
            ' Replace the full unique index with a partial one that excludes Amended rows
            ' (FilingStatus = 3), allowing AmendReturnAsync to create a second row
            ' for the same period without a constraint violation.
            Exec(conn, "DROP INDEX IF EXISTS ""IX_Acc_VatReturns_Year_Period_PeriodType_FormType""")
            Exec(conn,
                "CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Acc_VatReturns_Year_Period_PeriodType_FormType_Active"" " &
                "ON ""Acc_VatReturns"" (""Year"", ""Period"", ""PeriodType"", ""FormType"") " &
                "WHERE ""FilingStatus"" != 3")
        End Sub

        ' ── Tamper Audit Log (20260516100000) ────────────────────────────────

        Private Sub ApplyTamperAuditLog(conn As SqliteConnection)
            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Acc_TamperAuditLog"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Acc_TamperAuditLog"" PRIMARY KEY AUTOINCREMENT, " &
                """DetectedAt"" TEXT NOT NULL, " &
                """ReceiptId"" INTEGER NOT NULL, " &
                """ReceiptNumber"" TEXT NOT NULL, " &
                """TamperKind"" TEXT NOT NULL, " &
                """DetectedByService"" TEXT NOT NULL, " &
                """ExpectedValue"" TEXT NULL, " &
                """ActualValue"" TEXT NULL, " &
                """AdditionalContextJson"" TEXT NULL, " &
                """MachineName"" TEXT NOT NULL, " &
                """OperatingUser"" TEXT NOT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """CreatedBy"" TEXT NOT NULL" &
                ")")

            Exec(conn,
                "CREATE INDEX IF NOT EXISTS ""IX_Acc_TamperAuditLog_DetectedAt_TamperKind"" " &
                "ON ""Acc_TamperAuditLog"" (""DetectedAt"", ""TamperKind"")")

            Exec(conn,
                "CREATE TRIGGER IF NOT EXISTS acc_tamper_audit_no_update " &
                "BEFORE UPDATE ON ""Acc_TamperAuditLog"" " &
                "BEGIN " &
                "SELECT RAISE(ABORT, 'tamper-audit-immutable'); " &
                "END")

            Exec(conn,
                "CREATE TRIGGER IF NOT EXISTS acc_tamper_audit_no_delete " &
                "BEFORE DELETE ON ""Acc_TamperAuditLog"" " &
                "BEGIN " &
                "SELECT RAISE(ABORT, 'tamper-audit-immutable'); " &
                "END")
        End Sub

        ' ── VAT Three-Bucket Columns (20260514100000) ─────────────────────────

        Private Sub ApplyVatThreeBucketColumns(conn As SqliteConnection)
            ' Extend Pos_SalesTransactions with VAT aggregate columns
            Exec(conn, "ALTER TABLE ""Pos_SalesTransactions"" ADD COLUMN ""VatableSales"" TEXT NOT NULL DEFAULT '0'")
            Exec(conn, "ALTER TABLE ""Pos_SalesTransactions"" ADD COLUMN ""VatExemptSales"" TEXT NOT NULL DEFAULT '0'")
            Exec(conn, "ALTER TABLE ""Pos_SalesTransactions"" ADD COLUMN ""ZeroRatedSales"" TEXT NOT NULL DEFAULT '0'")
            Exec(conn, "ALTER TABLE ""Pos_SalesTransactions"" ADD COLUMN ""VatRateSnapshot"" TEXT NOT NULL DEFAULT '0.12'")
            Exec(conn, "ALTER TABLE ""Pos_SalesTransactions"" ADD COLUMN ""IsVatRegisteredSnapshot"" INTEGER NOT NULL DEFAULT 0")

            ' Extend Pos_SalesTransactionLines with per-line VAT columns
            Exec(conn, "ALTER TABLE ""Pos_SalesTransactionLines"" ADD COLUMN ""Treatment"" INTEGER NOT NULL DEFAULT 0")
            Exec(conn, "ALTER TABLE ""Pos_SalesTransactionLines"" ADD COLUMN ""VatableAmount"" TEXT NOT NULL DEFAULT '0'")
            Exec(conn, "ALTER TABLE ""Pos_SalesTransactionLines"" ADD COLUMN ""VatExemptAmount"" TEXT NOT NULL DEFAULT '0'")
            Exec(conn, "ALTER TABLE ""Pos_SalesTransactionLines"" ADD COLUMN ""ZeroRatedAmount"" TEXT NOT NULL DEFAULT '0'")
            Exec(conn, "ALTER TABLE ""Pos_SalesTransactionLines"" ADD COLUMN ""OutputVat"" TEXT NOT NULL DEFAULT '0'")

            ' VAT configuration singleton (Id=1 enforced by CHECK constraint in EF config)
            Exec(conn,
                "CREATE TABLE IF NOT EXISTS ""Pos_VatConfiguration"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pos_VatConfiguration"" PRIMARY KEY, " &
                """IsVatRegistered"" INTEGER NOT NULL, " &
                """VatRate"" TEXT NOT NULL, " &
                """NonVatPercentageTaxRate"" TEXT NOT NULL, " &
                """EffectiveFrom"" TEXT NOT NULL, " &
                """BusinessTIN"" TEXT NULL, " &
                """BusinessName"" TEXT NULL, " &
                """BusinessAddress"" TEXT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL, " &
                "CONSTRAINT ""CK_Pos_VatConfiguration_SingleRow"" CHECK (""Id"" = 1)" &
                ")")

            ' Seed the singleton row (non-VAT-registered default; owner updates via settings UI)
            Exec(conn,
                "INSERT OR IGNORE INTO ""Pos_VatConfiguration"" " &
                "(""Id"",""IsVatRegistered"",""VatRate"",""NonVatPercentageTaxRate"",""EffectiveFrom""," &
                """BusinessTIN"",""BusinessName"",""BusinessAddress""," &
                """CreatedBy"",""CreatedAt"",""ModifiedBy"",""ModifiedAt"") VALUES " &
                "(1,0,'0.12','0.03','2026-01-01 00:00:00',NULL,'Villon Farm Supply',NULL," &
                "'System','2026-01-01 00:00:00','System','2026-01-01 00:00:00')")
        End Sub

    End Module

End Namespace
