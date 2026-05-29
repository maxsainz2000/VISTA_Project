Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Purchasing.Entities

Namespace Data.Configurations

    Public Class PurchaseOrderLineConfiguration
        Implements IEntityTypeConfiguration(Of PurchaseOrderLine)

        Public Sub Configure(builder As EntityTypeBuilder(Of PurchaseOrderLine)) Implements IEntityTypeConfiguration(Of PurchaseOrderLine).Configure
            builder.ToTable("Pur_PurchaseOrderLines")

            builder.HasKey(Function(l) l.Id)

            builder.Property(Function(l) l.PurchaseOrderId).IsRequired()
            builder.Property(Function(l) l.ProductName).IsRequired().HasMaxLength(200)
            builder.Property(Function(l) l.UnitCost).HasPrecision(18, 4)
            builder.Property(Function(l) l.LineTotal).HasPrecision(18, 2)
        End Sub

    End Class

End Namespace
