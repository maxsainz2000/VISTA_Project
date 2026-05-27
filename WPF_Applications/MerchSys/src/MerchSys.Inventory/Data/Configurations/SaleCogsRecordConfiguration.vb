Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Inventory.Entities

Namespace Data.Configurations

    Public Class SaleCogsRecordConfiguration
        Implements IEntityTypeConfiguration(Of SaleCogsRecord)

        Public Sub Configure(builder As EntityTypeBuilder(Of SaleCogsRecord)) Implements IEntityTypeConfiguration(Of SaleCogsRecord).Configure
            builder.ToTable("Inv_SaleCogs")

            builder.HasKey(Function(r) r.Id)

            builder.Property(Function(r) r.UnitCost).HasPrecision(18, 4)
            builder.Property(Function(r) r.Cogs).HasPrecision(18, 4)

            ' Index for primary read path (Accounting query)
            builder.HasIndex(Function(r) New With {r.TransactionId, r.ProductId})

            ' Index for secondary read path (Batch audit)
            builder.HasIndex(Function(r) r.BatchId)

            ' Relationship with StockBatch
            builder.HasOne(Function(r) r.Batch) _
                .WithMany() _
                .HasForeignKey(Function(r) r.BatchId) _
                .OnDelete(DeleteBehavior.Restrict)
        End Sub

    End Class

End Namespace
