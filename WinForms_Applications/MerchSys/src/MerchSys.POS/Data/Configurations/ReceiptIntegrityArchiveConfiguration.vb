Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.POS.Entities

Namespace Data.Configurations

    Public Class ReceiptIntegrityArchiveConfiguration
        Implements IEntityTypeConfiguration(Of ReceiptIntegrityArchive)

        Public Sub Configure(builder As EntityTypeBuilder(Of ReceiptIntegrityArchive)) Implements IEntityTypeConfiguration(Of ReceiptIntegrityArchive).Configure
            builder.ToTable("Pos_ReceiptIntegrityArchive")
            builder.HasKey(Function(x) x.Id)

            builder.Property(Function(x) x.OriginalIntegrityId).IsRequired()
            builder.Property(Function(x) x.ReceiptId).IsRequired()
            builder.Property(Function(x) x.IntegrityHash).IsRequired().HasMaxLength(64)
            builder.Property(Function(x) x.PreviousHash).IsRequired().HasMaxLength(64)
            builder.Property(Function(x) x.RetentionExpiresAt).IsRequired()
            builder.Property(Function(x) x.IsImmutable).IsRequired()
            builder.Property(Function(x) x.HashAlgorithm).IsRequired().HasMaxLength(20)
            builder.Property(Function(x) x.CanonicalPayload).IsRequired().HasMaxLength(8000)
            builder.Property(Function(x) x.ArchivedAt).IsRequired()
            builder.Property(Function(x) x.ArchivedByService).IsRequired().HasMaxLength(100)

            builder.HasIndex(Function(x) x.ReceiptId)
            builder.HasIndex(Function(x) x.OriginalIntegrityId)
        End Sub

    End Class

End Namespace
