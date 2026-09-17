Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Purchasing.Entities

Namespace Data.Configurations

    Public Class PriceChangeAlertConfiguration
        Implements IEntityTypeConfiguration(Of PriceChangeAlert)

        Public Sub Configure(builder As EntityTypeBuilder(Of PriceChangeAlert)) Implements IEntityTypeConfiguration(Of PriceChangeAlert).Configure
            builder.ToTable("Pur_PriceChangeAlerts")

            builder.HasKey(Function(a) a.Id)

            builder.Property(Function(a) a.ProductId).IsRequired()
            builder.Property(Function(a) a.ProductName).IsRequired()
            builder.Property(Function(a) a.VendorId).IsRequired()
            builder.Property(Function(a) a.VendorName).IsRequired()
            builder.Property(Function(a) a.PreviousUnitCost).HasPrecision(18, 4)
            builder.Property(Function(a) a.NewUnitCost).HasPrecision(18, 4)
            builder.Property(Function(a) a.ChangePercent).HasPrecision(18, 4)
            builder.Property(Function(a) a.ChangeDirection).IsRequired()
            builder.Property(Function(a) a.GoodsReceiptId).IsRequired()

            builder.HasIndex(Function(a) a.IsAcknowledged)
            builder.HasIndex(Function(a) a.ProductId)
        End Sub

    End Class

End Namespace
