Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Inventory.Entities

Namespace Data.Configurations

    Public Class StockBatchConfiguration
        Implements IEntityTypeConfiguration(Of StockBatch)

        Public Sub Configure(builder As EntityTypeBuilder(Of StockBatch)) Implements IEntityTypeConfiguration(Of StockBatch).Configure
            builder.ToTable("Inv_StockBatches")

            builder.HasKey(Function(b) b.Id)

            builder.Property(Function(b) b.UnitCost).HasPrecision(18, 4)

            ' Composite index for FIFO ordering: oldest receipt per product first
            builder.HasIndex(Function(b) New With {b.ProductId, b.ReceiptDate})

            builder.Ignore(Function(b) b.IsExpired)
            builder.Ignore(Function(b) b.IsFullyConsumed)
        End Sub

    End Class

End Namespace
