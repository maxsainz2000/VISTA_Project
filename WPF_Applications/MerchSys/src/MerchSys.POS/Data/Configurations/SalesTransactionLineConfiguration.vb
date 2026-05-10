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

            ' VAT per-line columns (POS-14 extension via partial class)
            builder.Property(Function(l) l.Treatment).HasColumnType("INTEGER")
            builder.Property(Function(l) l.VatableAmount).HasPrecision(18, 2)
            builder.Property(Function(l) l.VatExemptAmount).HasPrecision(18, 2)
            builder.Property(Function(l) l.ZeroRatedAmount).HasPrecision(18, 2)
            builder.Property(Function(l) l.OutputVat).HasPrecision(18, 2)
        End Sub

    End Class

End Namespace
