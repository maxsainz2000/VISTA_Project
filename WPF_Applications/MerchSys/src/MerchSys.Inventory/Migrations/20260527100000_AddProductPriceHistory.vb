Imports System
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Migrations

Namespace Migrations

    <MigrationAttribute("20260527100000_AddProductPriceHistory")>
    Public Class AddProductPriceHistory
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql(
                "CREATE TABLE ""Inv_ProductPriceHistory"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Inv_ProductPriceHistory"" PRIMARY KEY AUTOINCREMENT, " &
                """ProductId"" INTEGER NOT NULL, " &
                """OldPrice"" TEXT NOT NULL, " &
                """NewPrice"" TEXT NOT NULL, " &
                """ChangedAt"" TEXT NOT NULL, " &
                """ChangedBy"" TEXT NOT NULL, " &
                """Reason"" TEXT NULL, " &
                "CONSTRAINT ""FK_Inv_ProductPriceHistory_Inv_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Inv_Products"" (""Id"") ON DELETE RESTRICT" &
                ")")

            migrationBuilder.Sql(
                "CREATE INDEX ""IX_Inv_ProductPriceHistory_ProductId_ChangedAt"" " &
                "ON ""Inv_ProductPriceHistory"" (""ProductId"", ""ChangedAt"" DESC)")
        End Sub

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Inv_ProductPriceHistory""")
        End Sub

    End Class

End Namespace
