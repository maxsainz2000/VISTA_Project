Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Inventory.Entities

Namespace Data.Configurations

    Public Class ShrinkageRecordConfiguration
        Implements IEntityTypeConfiguration(Of ShrinkageRecord)

        Public Sub Configure(builder As EntityTypeBuilder(Of ShrinkageRecord)) Implements IEntityTypeConfiguration(Of ShrinkageRecord).Configure
            builder.ToTable("Inv_ShrinkageRecords")

            builder.HasKey(Function(s) s.Id)

            builder.Property(Function(s) s.UnitCost).HasPrecision(18, 4)
            builder.Property(Function(s) s.TotalValue).HasPrecision(18, 2)
            builder.Property(Function(s) s.Reason).IsRequired().HasMaxLength(50)
            builder.Property(Function(s) s.Notes).HasMaxLength(256)

            builder.HasOne(Function(s) s.StockBatch).
                    WithMany().
                    HasForeignKey(Function(s) s.StockBatchId).
                    OnDelete(DeleteBehavior.SetNull).
                    IsRequired(False)
        End Sub

    End Class

End Namespace
