Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Purchasing.Entities

Namespace Data.Configurations

    Friend Class OrderSequenceConfiguration
        Implements IEntityTypeConfiguration(Of OrderSequence)

        Public Sub Configure(builder As EntityTypeBuilder(Of OrderSequence)) Implements IEntityTypeConfiguration(Of OrderSequence).Configure
            builder.ToTable("Pur_OrderSequences")

            builder.HasKey(Function(s) s.SeqKey)
            builder.Property(Function(s) s.SeqKey).
                HasMaxLength(16).
                ValueGeneratedNever()

            builder.Property(Function(s) s.RowVersion).
                IsRowVersion().
                HasColumnType("TIMESTAMP(6)").
                ValueGeneratedOnAddOrUpdate()
        End Sub

    End Class

End Namespace
