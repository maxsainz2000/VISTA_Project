Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Inventory.Entities

Namespace Data.Configurations

    Public Class StockAuditRecordConfiguration
        Implements IEntityTypeConfiguration(Of StockAuditRecord)

        Public Sub Configure(builder As EntityTypeBuilder(Of StockAuditRecord)) Implements IEntityTypeConfiguration(Of StockAuditRecord).Configure
            builder.ToTable("Inv_StockAuditRecords")

            builder.HasKey(Function(a) a.Id)

            builder.Property(Function(a) a.Reason).IsRequired().HasMaxLength(100)
            builder.Property(Function(a) a.Notes).HasMaxLength(512)
            builder.Property(Function(a) a.PerformedBy).IsRequired().HasMaxLength(100)

            builder.HasOne(Function(a) a.Product) _
                   .WithMany() _
                   .HasForeignKey(Function(a) a.ProductId) _
                   .OnDelete(DeleteBehavior.Restrict)
        End Sub

    End Class

End Namespace
