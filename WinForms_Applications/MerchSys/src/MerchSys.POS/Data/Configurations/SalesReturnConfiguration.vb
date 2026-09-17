Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.POS.Entities

Namespace Data.Configurations

    Friend Class SalesReturnConfiguration
        Implements IEntityTypeConfiguration(Of SalesReturn)

        Public Sub Configure(builder As EntityTypeBuilder(Of SalesReturn)) Implements IEntityTypeConfiguration(Of SalesReturn).Configure
            builder.ToTable("Pos_SalesReturns")

            builder.Property(Function(r) r.Reason).
                IsRequired().
                HasMaxLength(500)

            builder.Property(Function(r) r.ProductName).
                IsRequired().
                HasMaxLength(200)

            builder.Property(Function(r) r.UnitPrice).HasPrecision(18, 2)
            builder.Property(Function(r) r.RefundAmount).HasPrecision(18, 2)

            builder.HasOne(Function(r) r.OriginalTransaction).
                WithMany().
                HasForeignKey(Function(r) r.OriginalTransactionId).
                IsRequired()
        End Sub

    End Class

End Namespace
