Imports System
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Migrations

Namespace Migrations

    <MigrationAttribute("20260507100001_InitialPurchasing")>
    Public Class InitialPurchasing
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql(
                "CREATE TABLE ""Pur_Vendors"" (" &
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

            migrationBuilder.Sql("CREATE UNIQUE INDEX ""IX_Pur_Vendors_Name"" ON ""Pur_Vendors"" (""Name"")")

            migrationBuilder.Sql(
                "CREATE TABLE ""Pur_PurchaseOrders"" (" &
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

            migrationBuilder.Sql("CREATE UNIQUE INDEX ""IX_Pur_PurchaseOrders_OrderNumber"" ON ""Pur_PurchaseOrders"" (""OrderNumber"")")
            migrationBuilder.Sql("CREATE INDEX ""IX_Pur_PurchaseOrders_VendorId"" ON ""Pur_PurchaseOrders"" (""VendorId"")")

            migrationBuilder.Sql(
                "CREATE TABLE ""Pur_PurchaseOrderLines"" (" &
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

            migrationBuilder.Sql("CREATE INDEX ""IX_Pur_PurchaseOrderLines_PurchaseOrderId"" ON ""Pur_PurchaseOrderLines"" (""PurchaseOrderId"")")

            migrationBuilder.Sql(
                "CREATE TABLE ""Pur_GoodsReceipts"" (" &
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

            migrationBuilder.Sql("CREATE UNIQUE INDEX ""IX_Pur_GoodsReceipts_ReceiptNumber"" ON ""Pur_GoodsReceipts"" (""ReceiptNumber"")")
            migrationBuilder.Sql("CREATE INDEX ""IX_Pur_GoodsReceipts_PurchaseOrderId"" ON ""Pur_GoodsReceipts"" (""PurchaseOrderId"")")

            migrationBuilder.Sql(
                "CREATE TABLE ""Pur_GoodsReceiptLines"" (" &
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

            migrationBuilder.Sql("CREATE INDEX ""IX_Pur_GoodsReceiptLines_GoodsReceiptId"" ON ""Pur_GoodsReceiptLines"" (""GoodsReceiptId"")")

            migrationBuilder.Sql(
                "CREATE TABLE ""Pur_AccountsPayable"" (" &
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

            migrationBuilder.Sql("CREATE INDEX ""IX_Pur_AccountsPayable_VendorId_IsPaid"" ON ""Pur_AccountsPayable"" (""VendorId"", ""IsPaid"")")
            migrationBuilder.Sql("CREATE UNIQUE INDEX ""IX_Pur_AccountsPayable_PurchaseOrderId"" ON ""Pur_AccountsPayable"" (""PurchaseOrderId"")")

            migrationBuilder.Sql(
                "CREATE TABLE ""Pur_ReorderConfigs"" (" &
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

            migrationBuilder.Sql("CREATE UNIQUE INDEX ""IX_Pur_ReorderConfigs_ProductId"" ON ""Pur_ReorderConfigs"" (""ProductId"")")
            migrationBuilder.Sql("CREATE INDEX ""IX_Pur_ReorderConfigs_IsActive"" ON ""Pur_ReorderConfigs"" (""IsActive"")")

            migrationBuilder.Sql(
                "CREATE TABLE ""Pur_ReorderSuggestions"" (" &
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

            migrationBuilder.Sql("CREATE INDEX ""IX_Pur_ReorderSuggestions_Status"" ON ""Pur_ReorderSuggestions"" (""Status"")")
            migrationBuilder.Sql("CREATE INDEX ""IX_Pur_ReorderSuggestions_ProductId_Status"" ON ""Pur_ReorderSuggestions"" (""ProductId"", ""Status"")")

            migrationBuilder.Sql(
                "CREATE TABLE ""Pur_PriceChangeAlerts"" (" &
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

            migrationBuilder.Sql("CREATE INDEX ""IX_Pur_PriceChangeAlerts_IsAcknowledged"" ON ""Pur_PriceChangeAlerts"" (""IsAcknowledged"")")
            migrationBuilder.Sql("CREATE INDEX ""IX_Pur_PriceChangeAlerts_ProductId"" ON ""Pur_PriceChangeAlerts"" (""ProductId"")")

            ' Seed: 3 vendors
            migrationBuilder.Sql(
                "INSERT INTO ""Pur_Vendors"" (""Id"",""Name"",""ContactPerson"",""Phone"",""Email"",""Address"",""DefaultLeadTimeDays"",""Notes"",""IsDeleted"",""DeletedBy"",""DeletedAt"",""CreatedBy"",""CreatedAt"",""ModifiedBy"",""ModifiedAt"") VALUES " &
                "(1,'AgriChem Supplies','Juan dela Cruz','09171234567','sales@agrichemsupplies.ph','123 Magsaysay Ave, Cagayan de Oro City',5,'Pesticides and chemicals supplier.',0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00')," &
                "(2,'FarmFresh Seeds Corp.','Maria Santos','09189876543','orders@farmfreshseeds.ph','456 National Highway, Bukidnon',7,'Seeds and planting materials supplier.',0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00')," &
                "(3,'Golden Feeds Trading','Pedro Reyes','09205551234','info@goldenfeedstrading.ph','789 Rizal Street, Iligan City',3,'Animal feeds and livestock supplies.',0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00')")
        End Sub

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pur_PriceChangeAlerts""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pur_ReorderSuggestions""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pur_ReorderConfigs""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pur_AccountsPayable""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pur_GoodsReceiptLines""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pur_GoodsReceipts""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pur_PurchaseOrderLines""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pur_PurchaseOrders""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pur_Vendors""")
        End Sub

    End Class

End Namespace
