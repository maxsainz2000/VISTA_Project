Imports System
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Migrations

Namespace Migrations

    <MigrationAttribute("20260509100003_AddStockMovement")>
    Public Class AddStockMovement
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql(
                "CREATE TABLE ""Inv_StockMovements"" (" &
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

            migrationBuilder.Sql(
                "CREATE INDEX ""IX_Inv_StockMovements_ProductId_OccurredAt"" " &
                "ON ""Inv_StockMovements"" (""ProductId"", ""OccurredAt"")")
        End Sub

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Inv_StockMovements""")
        End Sub

    End Class

End Namespace
