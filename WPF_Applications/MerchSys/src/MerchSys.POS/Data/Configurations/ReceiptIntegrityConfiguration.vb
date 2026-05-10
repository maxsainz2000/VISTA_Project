Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.POS.Entities

Namespace Data.Configurations

    Friend Class ReceiptIntegrityConfiguration
        Implements IEntityTypeConfiguration(Of ReceiptIntegrity)

        Public Sub Configure(builder As EntityTypeBuilder(Of ReceiptIntegrity)) Implements IEntityTypeConfiguration(Of ReceiptIntegrity).Configure
            builder.ToTable("Pos_ReceiptIntegrity")

            builder.Property(Function(i) i.IntegrityHash).IsRequired().HasMaxLength(64)
            builder.Property(Function(i) i.PreviousHash).IsRequired().HasMaxLength(64)
            builder.Property(Function(i) i.HashAlgorithm).IsRequired().HasMaxLength(20)
            builder.Property(Function(i) i.CanonicalPayload).IsRequired().HasMaxLength(8000)
            builder.Property(Function(i) i.IsImmutable).IsRequired()
            builder.Property(Function(i) i.RetentionExpiresAt).IsRequired()

            builder.HasIndex(Function(i) i.ReceiptId).IsUnique()
            builder.HasIndex(Function(i) i.RetentionExpiresAt)

            builder.HasOne(Function(i) i.Receipt).
                WithOne().
                HasForeignKey(Of ReceiptIntegrity)(Function(i) i.ReceiptId).
                IsRequired()
        End Sub

    End Class

End Namespace
