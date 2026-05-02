Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.POS.Entities
Imports MerchSys.POS.Data.SeedData

Namespace Data.Configurations

    Friend Class CreditAccountConfiguration
        Implements IEntityTypeConfiguration(Of CreditAccount)

        Public Sub Configure(builder As EntityTypeBuilder(Of CreditAccount)) Implements IEntityTypeConfiguration(Of CreditAccount).Configure
            builder.ToTable("Pos_CreditAccounts")

            builder.Property(Function(c) c.CustomerName).
                IsRequired().
                HasMaxLength(200)

            builder.Property(Function(c) c.CurrentBalance).HasPrecision(18, 2)
            builder.Property(Function(c) c.TotalCreditExtended).HasPrecision(18, 2)
            builder.Property(Function(c) c.TotalPaymentsReceived).HasPrecision(18, 2)

            builder.HasIndex(Function(c) c.IsBlocked)

            builder.Property(Function(c) c.Phone).HasMaxLength(50)
            builder.Property(Function(c) c.Address).HasMaxLength(500)
            builder.Property(Function(c) c.Notes).HasMaxLength(1000)

            builder.HasData(POSSeedData.GetCreditAccountSeeds())
        End Sub

    End Class

End Namespace
