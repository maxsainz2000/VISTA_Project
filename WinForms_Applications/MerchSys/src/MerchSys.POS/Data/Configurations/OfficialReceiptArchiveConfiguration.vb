Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.POS.Entities

Namespace Data.Configurations

    Friend Class OfficialReceiptArchiveConfiguration
        Implements IEntityTypeConfiguration(Of OfficialReceiptArchive)

        Public Sub Configure(builder As EntityTypeBuilder(Of OfficialReceiptArchive)) Implements IEntityTypeConfiguration(Of OfficialReceiptArchive).Configure
            builder.ToTable("Pos_OfficialReceiptArchive")

            builder.Property(Function(a) a.ReceiptNumber).IsRequired().HasMaxLength(20)
            builder.Property(Function(a) a.BusinessName).IsRequired().HasMaxLength(200)
            builder.Property(Function(a) a.BusinessAddress).HasMaxLength(500)
            builder.Property(Function(a) a.BusinessTIN).HasMaxLength(50)
            builder.Property(Function(a) a.Items).HasMaxLength(4000)
            builder.Property(Function(a) a.TotalAmount).HasPrecision(18, 2)
            builder.Property(Function(a) a.VatAmount).HasPrecision(18, 2)
            builder.Property(Function(a) a.ArchivedHash).IsRequired().HasMaxLength(64)
            builder.Property(Function(a) a.ArchivedAt).IsRequired()

            builder.HasIndex(Function(a) a.OriginalReceiptId)
        End Sub

    End Class

End Namespace
