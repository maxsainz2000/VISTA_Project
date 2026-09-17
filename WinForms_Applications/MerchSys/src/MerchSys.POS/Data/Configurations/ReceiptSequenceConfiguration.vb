Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.POS.Entities

Namespace Data.Configurations

    Friend Class ReceiptSequenceConfiguration
        Implements IEntityTypeConfiguration(Of ReceiptSequence)

        Public Sub Configure(builder As EntityTypeBuilder(Of ReceiptSequence)) Implements IEntityTypeConfiguration(Of ReceiptSequence).Configure
            builder.ToTable("Pos_ReceiptSequence")

            builder.HasIndex(Function(s) s.Year).IsUnique()

            builder.Property(Function(s) s.RowVersion).
                IsRowVersion().
                HasColumnType("TIMESTAMP(6)").
                ValueGeneratedOnAddOrUpdate()
        End Sub

    End Class

End Namespace
