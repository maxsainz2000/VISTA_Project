Imports System
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Migrations

Namespace Migrations

    <MigrationAttribute("20260527120000_AddInvSaleCogs")>
    Public Class AddInvSaleCogs
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql(
                "CREATE TABLE ""Inv_SaleCogs"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Inv_SaleCogs"" PRIMARY KEY AUTOINCREMENT, " &
                """TransactionId"" INTEGER NOT NULL, " &
                """ProductId"" INTEGER NOT NULL, " &
                """BatchId"" INTEGER NOT NULL, " &
                """QuantityDeducted"" INTEGER NOT NULL, " &
                """UnitCost"" TEXT NOT NULL, " &
                """Cogs"" TEXT NOT NULL, " &
                """DeductedAt"" TEXT NOT NULL, " &
                "CONSTRAINT ""FK_Inv_SaleCogs_Inv_StockBatches_BatchId"" FOREIGN KEY (""BatchId"") REFERENCES ""Inv_StockBatches"" (""Id"") ON DELETE RESTRICT" &
                ")")

            migrationBuilder.Sql(
                "CREATE INDEX ""IX_Inv_SaleCogs_Tx_Product"" " &
                "ON ""Inv_SaleCogs"" (""TransactionId"", ""ProductId"")")

            migrationBuilder.Sql(
                "CREATE INDEX ""IX_Inv_SaleCogs_Batch"" " &
                "ON ""Inv_SaleCogs"" (""BatchId"")")
        End Sub

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Inv_SaleCogs""")
        End Sub

    End Class

End Namespace
