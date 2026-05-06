Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Accounting.Entities

Namespace Data.Configurations

    Public Class ExpenseRecordConfiguration
        Implements IEntityTypeConfiguration(Of ExpenseRecord)

        Public Sub Configure(builder As EntityTypeBuilder(Of ExpenseRecord)) Implements IEntityTypeConfiguration(Of ExpenseRecord).Configure
            builder.ToTable("Acc_ExpenseRecords")

            builder.HasKey(Function(e) e.Id)

            builder.Property(Function(e) e.Category).IsRequired().HasMaxLength(50)
            builder.Property(Function(e) e.Amount).HasPrecision(18, 2)
            builder.Property(Function(e) e.Description).HasMaxLength(500)
            builder.Property(Function(e) e.SourceModule).IsRequired().HasMaxLength(50)
        End Sub

    End Class

End Namespace
