Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.POS.Entities

Namespace Data.Configurations

    Friend Class OfficialReceiptConfiguration
        Implements IEntityTypeConfiguration(Of OfficialReceipt)

        Public Sub Configure(builder As EntityTypeBuilder(Of OfficialReceipt)) Implements IEntityTypeConfiguration(Of OfficialReceipt).Configure
            builder.ToTable("Pos_OfficialReceipts")

            builder.Property(Function(r) r.ReceiptNumber).
                IsRequired().
                HasMaxLength(20)

            builder.HasIndex(Function(r) r.ReceiptNumber).IsUnique()

            builder.Property(Function(r) r.TotalAmount).HasPrecision(18, 2)
            builder.Property(Function(r) r.VatAmount).HasPrecision(18, 2)

            builder.Property(Function(r) r.BusinessName).
                IsRequired().
                HasMaxLength(200)

            builder.Property(Function(r) r.BusinessAddress).HasMaxLength(500)
            builder.Property(Function(r) r.BusinessTIN).HasMaxLength(50)
            builder.Property(Function(r) r.Items).HasMaxLength(4000)
        End Sub

    End Class

End Namespace
