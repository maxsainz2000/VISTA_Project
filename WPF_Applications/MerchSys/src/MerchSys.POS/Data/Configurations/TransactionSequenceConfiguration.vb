Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.POS.Entities

Namespace Data.Configurations

    Friend Class TransactionSequenceConfiguration
        Implements IEntityTypeConfiguration(Of TransactionSequence)

        Public Sub Configure(builder As EntityTypeBuilder(Of TransactionSequence)) Implements IEntityTypeConfiguration(Of TransactionSequence).Configure
            builder.ToTable("Pos_TransactionSequences")

            builder.HasKey(Function(s) s.Year)
            builder.Property(Function(s) s.Year).ValueGeneratedNever()

            builder.Property(Function(s) s.RowVersion).
                IsRowVersion().
                HasColumnType("TIMESTAMP(6)").
                ValueGeneratedOnAddOrUpdate()
        End Sub

    End Class

End Namespace
