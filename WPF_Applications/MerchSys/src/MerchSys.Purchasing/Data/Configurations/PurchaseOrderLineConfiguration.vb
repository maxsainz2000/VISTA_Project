Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Purchasing.Entities

Namespace Data.Configurations

    Public Class PurchaseOrderLineConfiguration
        Implements IEntityTypeConfiguration(Of PurchaseOrderLine)

        Public Sub Configure(builder As EntityTypeBuilder(Of PurchaseOrderLine)) Implements IEntityTypeConfiguration(Of PurchaseOrderLine).Configure
            builder.ToTable("Pur_PurchaseOrderLines")

            builder.HasKey(Function(l) l.Id)

            ' Append-only child table: no RowVersion column. Ignore the property
            ' inherited from AuditableEntity -> ConcurrencyAwareEntity so EF does
            ' not reference a non-existent column on insert.
            builder.Ignore(Function(l) l.RowVersion)

            builder.Property(Function(l) l.PurchaseOrderId).IsRequired()
            builder.Property(Function(l) l.ProductName).IsRequired().HasMaxLength(200)
            builder.Property(Function(l) l.UnitCost).HasPrecision(18, 4)
            builder.Property(Function(l) l.LineTotal).HasPrecision(18, 2)
        End Sub

    End Class

End Namespace
