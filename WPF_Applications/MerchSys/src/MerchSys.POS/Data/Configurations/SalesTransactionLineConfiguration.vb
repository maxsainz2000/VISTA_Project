Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.POS.Entities

Namespace Data.Configurations

    Friend Class SalesTransactionLineConfiguration
        Implements IEntityTypeConfiguration(Of SalesTransactionLine)

        Public Sub Configure(builder As EntityTypeBuilder(Of SalesTransactionLine)) Implements IEntityTypeConfiguration(Of SalesTransactionLine).Configure
            builder.ToTable("Pos_SalesTransactionLines")

            builder.Property(Function(l) l.ProductName).
                IsRequired().
                HasMaxLength(200)

            builder.Property(Function(l) l.UnitPrice).HasPrecision(18, 2)
            builder.Property(Function(l) l.DiscountAmount).HasPrecision(18, 2)
            builder.Property(Function(l) l.LineTotal).HasPrecision(18, 2)
        End Sub

    End Class

End Namespace
