Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.POS.Entities

Namespace Data.Configurations

    Friend Class CreditPaymentConfiguration
        Implements IEntityTypeConfiguration(Of CreditPayment)

        Public Sub Configure(builder As EntityTypeBuilder(Of CreditPayment)) Implements IEntityTypeConfiguration(Of CreditPayment).Configure
            builder.ToTable("Pos_CreditPayments")

            builder.Property(Function(p) p.PaymentAmount).HasPrecision(18, 2)

            builder.Property(Function(p) p.Notes).HasMaxLength(500)
            builder.Property(Function(p) p.ReceivedBy).
                IsRequired().
                HasMaxLength(100)

            builder.HasOne(Function(p) p.CreditAccount).
                WithMany(Function(c) c.Payments).
                HasForeignKey(Function(p) p.CreditAccountId).
                IsRequired()
        End Sub

    End Class

End Namespace
