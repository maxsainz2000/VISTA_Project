Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Accounting.Entities

Namespace Data.Configurations

    Public Class RevenueRecordConfiguration
        Implements IEntityTypeConfiguration(Of RevenueRecord)

        Public Sub Configure(builder As EntityTypeBuilder(Of RevenueRecord)) Implements IEntityTypeConfiguration(Of RevenueRecord).Configure
            builder.ToTable("Acc_RevenueRecords")

            builder.HasKey(Function(r) r.Id)

            builder.Property(Function(r) r.ProductName).IsRequired().HasMaxLength(200)
            builder.Property(Function(r) r.GrossAmount).HasPrecision(18, 2)
            builder.Property(Function(r) r.DiscountAmount).HasPrecision(18, 2)
            builder.Property(Function(r) r.NetAmount).HasPrecision(18, 2)
            builder.Property(Function(r) r.VatAmount).HasPrecision(18, 2)
            builder.Property(Function(r) r.COGS).HasPrecision(18, 2)
            builder.Property(Function(r) r.GrossProfit).HasPrecision(18, 2)

            builder.HasIndex(Function(r) r.RecordDate)
            builder.HasIndex(Function(r) r.ProductId)
        End Sub

    End Class

End Namespace
