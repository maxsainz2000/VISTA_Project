Imports System
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Migrations

Namespace Migrations

    <MigrationAttribute("20260507100003_InitialPOS")>
    Public Class InitialPOS
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql(
                "CREATE TABLE ""Pos_CreditAccounts"" (" &
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

            migrationBuilder.Sql("CREATE INDEX ""IX_Pos_CreditAccounts_IsBlocked"" ON ""Pos_CreditAccounts"" (""IsBlocked"")")

            migrationBuilder.Sql(
                "CREATE TABLE ""Pos_SalesTransactions"" (" &
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

            migrationBuilder.Sql("CREATE UNIQUE INDEX ""IX_Pos_SalesTransactions_TransactionNumber"" ON ""Pos_SalesTransactions"" (""TransactionNumber"")")
            migrationBuilder.Sql("CREATE INDEX ""IX_Pos_SalesTransactions_TransactionDate"" ON ""Pos_SalesTransactions"" (""TransactionDate"")")
            migrationBuilder.Sql("CREATE INDEX ""IX_Pos_SalesTransactions_CreditAccountId"" ON ""Pos_SalesTransactions"" (""CreditAccountId"")")

            migrationBuilder.Sql(
                "CREATE TABLE ""Pos_SalesTransactionLines"" (" &
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

            migrationBuilder.Sql("CREATE INDEX ""IX_Pos_SalesTransactionLines_TransactionId"" ON ""Pos_SalesTransactionLines"" (""TransactionId"")")

            migrationBuilder.Sql(
                "CREATE TABLE ""Pos_OfficialReceipts"" (" &
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

            migrationBuilder.Sql("CREATE UNIQUE INDEX ""IX_Pos_OfficialReceipts_ReceiptNumber"" ON ""Pos_OfficialReceipts"" (""ReceiptNumber"")")
            migrationBuilder.Sql("CREATE UNIQUE INDEX ""IX_Pos_OfficialReceipts_TransactionId"" ON ""Pos_OfficialReceipts"" (""TransactionId"")")

            migrationBuilder.Sql(
                "CREATE TABLE ""Pos_CreditPayments"" (" &
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

            migrationBuilder.Sql("CREATE INDEX ""IX_Pos_CreditPayments_CreditAccountId"" ON ""Pos_CreditPayments"" (""CreditAccountId"")")

            migrationBuilder.Sql(
                "CREATE TABLE ""Pos_SalesReturns"" (" &
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

            migrationBuilder.Sql("CREATE INDEX ""IX_Pos_SalesReturns_OriginalTransactionId"" ON ""Pos_SalesReturns"" (""OriginalTransactionId"")")

            ' Seed: 3 sample credit accounts
            migrationBuilder.Sql(
                "INSERT INTO ""Pos_CreditAccounts"" (""Id"",""CustomerName"",""Phone"",""Address"",""Notes"",""CurrentBalance"",""TotalCreditExtended"",""TotalPaymentsReceived"",""IsBlocked"",""IsDeleted"",""DeletedBy"",""DeletedAt"",""CreatedBy"",""CreatedAt"",""ModifiedBy"",""ModifiedAt"",""LastTransactionDate"") VALUES " &
                "(1,'Juan Dela Cruz','','','Farmer','0','0','0',0,0,NULL,NULL,'System','2026-01-01 00:00:00','System',NULL,NULL)," &
                "(2,'Maria Santos','','','Farmer','500','500','0',1,0,NULL,NULL,'System','2026-01-01 00:00:00','System',NULL,NULL)," &
                "(3,'Pedro Reyes','','','Farmer','0','0','0',0,0,NULL,NULL,'System','2026-01-01 00:00:00','System',NULL,NULL)")
        End Sub

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pos_SalesReturns""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pos_CreditPayments""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pos_OfficialReceipts""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pos_SalesTransactionLines""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pos_SalesTransactions""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pos_CreditAccounts""")
        End Sub

    End Class

End Namespace
