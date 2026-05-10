Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.POS.Entities

Namespace Data.Configurations

    ''' <summary>
    ''' EF Core configuration for <see cref="VatConfiguration"/>.
    ''' Enforces the singleton invariant with a primary-key unique constraint and
    ''' a check constraint so only Id = 1 can be inserted.
    ''' </summary>
    Friend Class VatConfigurationMap
        Implements IEntityTypeConfiguration(Of VatConfiguration)

        Public Sub Configure(builder As EntityTypeBuilder(Of VatConfiguration)) Implements IEntityTypeConfiguration(Of VatConfiguration).Configure
            builder.ToTable("Pos_VatConfiguration")

            ' Never auto-generate Id — the single row is always inserted with Id = 1.
            builder.Property(Function(v) v.Id).ValueGeneratedNever()

            ' Database-level singleton guard: only Id = 1 may ever be inserted.
            builder.HasCheckConstraint("CK_Pos_VatConfiguration_SingleRow", """Id"" = 1")

            builder.Property(Function(v) v.VatRate).HasPrecision(5, 4)
            builder.Property(Function(v) v.NonVatPercentageTaxRate).HasPrecision(5, 4)

            builder.Property(Function(v) v.BusinessTIN).HasMaxLength(50)
            builder.Property(Function(v) v.BusinessName).HasMaxLength(200)
            builder.Property(Function(v) v.BusinessAddress).HasMaxLength(500)
        End Sub

    End Class

End Namespace
