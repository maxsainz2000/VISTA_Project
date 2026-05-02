Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.POS.Entities

Namespace Data.Configurations

    Friend Class SalesTransactionConfiguration
        Implements IEntityTypeConfiguration(Of SalesTransaction)

        Public Sub Configure(builder As EntityTypeBuilder(Of SalesTransaction)) Implements IEntityTypeConfiguration(Of SalesTransaction).Configure
            builder.ToTable("Pos_SalesTransactions")

            builder.Property(Function(s) s.TransactionNumber).
                IsRequired().
                HasMaxLength(20)

            builder.HasIndex(Function(s) s.TransactionNumber).IsUnique()

            builder.Property(Function(s) s.SubTotal).HasPrecision(18, 2)
            builder.Property(Function(s) s.DiscountAmount).HasPrecision(18, 2)
            builder.Property(Function(s) s.VatAmount).HasPrecision(18, 2)
            builder.Property(Function(s) s.TotalAmount).HasPrecision(18, 2)
            builder.Property(Function(s) s.AmountTendered).HasPrecision(18, 2)
            builder.Property(Function(s) s.ChangeAmount).HasPrecision(18, 2)

            builder.HasIndex(Function(s) s.TransactionDate)

            builder.Property(Function(s) s.CustomerName).HasMaxLength(200)
            builder.Property(Function(s) s.VoidReason).HasMaxLength(500)

            builder.HasMany(Function(s) s.Lines).
                WithOne(Function(l) l.Transaction).
                HasForeignKey(Function(l) l.TransactionId).
                OnDelete(DeleteBehavior.Cascade)

            builder.HasOne(Function(s) s.Receipt).
                WithOne(Function(r) r.Transaction).
                HasForeignKey(Of OfficialReceipt)(Function(r) r.TransactionId)

            builder.HasOne(Function(s) s.CreditAccount).
                WithMany(Function(c) c.Transactions).
                HasForeignKey("CreditAccountId").
                IsRequired(False)
        End Sub

    End Class

End Namespace
