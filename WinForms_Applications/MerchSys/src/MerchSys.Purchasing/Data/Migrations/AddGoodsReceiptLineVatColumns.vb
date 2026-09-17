Imports Microsoft.EntityFrameworkCore.Migrations

Namespace Data.Migrations

    ''' <summary>
    ''' Adds per-line VAT classification and computed VAT amounts to Pur_GoodsReceiptLines.
    ''' Applied at runtime via DatabaseInitializer (migration ID: 20260516140000_AddGoodsReceiptLineVatColumns).
    ''' NOTE: EF Core 10 CLI cannot discover VB.NET migration classes — see agent_wiki
    ''' errors/efcore10-vbnet-migration-discovery-bug.md. DatabaseInitializer is the authoritative runner.
    ''' </summary>
    <MigrationAttribute("20260516140000_AddGoodsReceiptLineVatColumns")>
    Public Class AddGoodsReceiptLineVatColumns
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)
            migrationBuilder.AddColumn(Of Integer)(
                name:="VatClassification",
                table:="Pur_GoodsReceiptLines",
                nullable:=False,
                defaultValue:=0)

            migrationBuilder.AddColumn(Of Decimal)(
                name:="VatAmount",
                table:="Pur_GoodsReceiptLines",
                nullable:=False,
                defaultValue:=0D)

            migrationBuilder.AddColumn(Of Decimal)(
                name:="VatableSales",
                table:="Pur_GoodsReceiptLines",
                nullable:=False,
                defaultValue:=0D)
        End Sub

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)
            ' SQLite does not support DROP COLUMN — schema rollback requires table recreation.
            ' In practice, use DatabaseInitializer idempotency rather than EF Down().
        End Sub

    End Class

End Namespace
