Imports System
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Migrations

Namespace Migrations

    <MigrationAttribute("20260507100004_InitialAccounting")>
    Public Class InitialAccounting
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql(
                "CREATE TABLE ""Acc_FinancialPeriods"" (" &
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

            migrationBuilder.Sql(
                "CREATE TABLE ""Acc_RevenueRecords"" (" &
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

            migrationBuilder.Sql(
                "CREATE TABLE ""Acc_ExpenseRecords"" (" &
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

            migrationBuilder.Sql(
                "CREATE TABLE ""Acc_FinancialSnapshots"" (" &
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

            migrationBuilder.Sql("CREATE INDEX ""IX_Acc_RevenueRecords_RecordDate"" ON ""Acc_RevenueRecords"" (""RecordDate"")")
            migrationBuilder.Sql("CREATE INDEX ""IX_Acc_RevenueRecords_ProductId"" ON ""Acc_RevenueRecords"" (""ProductId"")")
            migrationBuilder.Sql("CREATE UNIQUE INDEX ""IX_Acc_FinancialSnapshots_SnapshotDate"" ON ""Acc_FinancialSnapshots"" (""SnapshotDate"")")
        End Sub

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Acc_FinancialSnapshots""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Acc_ExpenseRecords""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Acc_RevenueRecords""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Acc_FinancialPeriods""")
        End Sub

    End Class

End Namespace
