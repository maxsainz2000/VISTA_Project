Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Migrations

Namespace Migrations

    <MigrationAttribute("20260527110000_AddVendorProductCatalog")>
    Public Class AddVendorProductCatalog
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql(
                "CREATE TABLE IF NOT EXISTS ""Pur_VendorProducts"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pur_VendorProducts"" PRIMARY KEY AUTOINCREMENT, " &
                """VendorId"" INTEGER NOT NULL, " &
                """ProductId"" INTEGER NOT NULL, " &
                """ProductName"" TEXT NOT NULL, " &
                """LastUnitCost"" TEXT NOT NULL DEFAULT '0', " &
                """Notes"" TEXT NULL, " &
                """IsDeleted"" INTEGER NOT NULL DEFAULT 0, " &
                """DeletedBy"" TEXT NULL, " &
                """DeletedAt"" TEXT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL, " &
                "CONSTRAINT ""FK_Pur_VendorProducts_Pur_Vendors_VendorId"" FOREIGN KEY (""VendorId"") REFERENCES ""Pur_Vendors"" (""Id"") ON DELETE RESTRICT" &
                ");")

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS ""UX_Pur_VendorProducts_Vendor_Product"" " &
                "ON ""Pur_VendorProducts"" (""VendorId"", ""ProductId"") " &
                "WHERE ""IsDeleted"" = 0;")
        End Sub

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql("DROP INDEX IF EXISTS ""UX_Pur_VendorProducts_Vendor_Product"";")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pur_VendorProducts"";")
        End Sub

    End Class

End Namespace
