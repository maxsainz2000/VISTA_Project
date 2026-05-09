Imports System
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Migrations

Namespace Migrations

    <MigrationAttribute("20260507100002_InitialInventory")>
    Public Class InitialInventory
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql(
                "CREATE TABLE ""Inv_ProductCategories"" (" &
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

            migrationBuilder.Sql("CREATE UNIQUE INDEX ""IX_Inv_ProductCategories_Name"" ON ""Inv_ProductCategories"" (""Name"")")

            migrationBuilder.Sql(
                "CREATE TABLE ""Inv_Products"" (" &
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

            migrationBuilder.Sql("CREATE UNIQUE INDEX ""IX_Inv_Products_Sku"" ON ""Inv_Products"" (""Sku"")")

            migrationBuilder.Sql(
                "CREATE TABLE ""Inv_StockBatches"" (" &
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

            migrationBuilder.Sql("CREATE INDEX ""IX_Inv_StockBatches_ProductId_ReceiptDate"" ON ""Inv_StockBatches"" (""ProductId"", ""ReceiptDate"")")

            migrationBuilder.Sql(
                "CREATE TABLE ""Inv_ShrinkageRecords"" (" &
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

            migrationBuilder.Sql(
                "CREATE TABLE ""Inv_StockAlertConfigs"" (" &
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

            ' Seed: 4 product categories
            migrationBuilder.Sql(
                "INSERT INTO ""Inv_ProductCategories"" (""Id"",""Name"",""Description"",""IsDeleted"",""DeletedBy"",""DeletedAt"",""CreatedBy"",""CreatedAt"",""ModifiedBy"",""ModifiedAt"") VALUES " &
                "(1,'Fertilizers','Soil nutrients and plant growth supplements.',0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00')," &
                "(2,'Pesticides/Chemicals','Insecticides, herbicides, and fungicides.',0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00')," &
                "(3,'Seeds','Planting seeds for various crops.',0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00')," &
                "(4,'Animal Feeds','Feeds for hogs, poultry, and aquaculture.',0,NULL,NULL,'Manager','2026-01-01 00:00:00','Manager','2026-01-01 00:00:00')")

            ' Seed: 20 products (5 fertilizers, 5 pesticides, 5 seeds, 5 feeds)
            migrationBuilder.Sql(
                "INSERT INTO ""Inv_Products"" (""Id"",""CategoryId"",""Name"",""Sku"",""Unit"",""RetailPrice"",""HasExpiry"",""MinimumThreshold"",""IsActive"",""IsDeleted"",""DeletedBy"",""DeletedAt"",""CreatedBy"",""CreatedAt"",""ModifiedBy"",""ModifiedAt"",""Description"") VALUES " &
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

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Inv_StockAlertConfigs""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Inv_ShrinkageRecords""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Inv_StockBatches""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Inv_Products""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Inv_ProductCategories""")
        End Sub

    End Class

End Namespace
