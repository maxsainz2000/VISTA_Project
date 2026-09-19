Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Accounting.Entities

Namespace Data.Configurations

    Public Class VatReturnConfiguration
        Implements IEntityTypeConfiguration(Of VatReturn)

        Public Sub Configure(builder As EntityTypeBuilder(Of VatReturn)) Implements IEntityTypeConfiguration(Of VatReturn).Configure
            builder.ToTable("Acc_VatReturns")

            builder.HasKey(Function(r) r.Id)

            builder.Property(Function(r) r.FiledBy).HasMaxLength(256)

            builder.Property(Function(r) r.TotalVatableSales).HasPrecision(18, 2)
            builder.Property(Function(r) r.TotalVatExemptSales).HasPrecision(18, 2)
            builder.Property(Function(r) r.TotalZeroRatedSales).HasPrecision(18, 2)
            builder.Property(Function(r) r.TotalOutputVat).HasPrecision(18, 2)
            builder.Property(Function(r) r.TotalVatablePurchases).HasPrecision(18, 2)
            builder.Property(Function(r) r.TotalInputVat).HasPrecision(18, 2)
            builder.Property(Function(r) r.VatPayable).HasPrecision(18, 2)

            ' Prevents duplicate filings for the same period/form combination.
            builder.HasIndex(Function(r) New With {r.Year, r.Period, r.PeriodType, r.FormType}).IsUnique()

            builder.HasMany(Function(r) r.Lines).
                    WithOne(Function(l) l.VatReturn).
                    HasForeignKey(Function(l) l.VatReturnId).
                    OnDelete(DeleteBehavior.Cascade)

            builder.Property(Function(e) e.RowVersion).
                IsRowVersion().
                HasColumnType("TIMESTAMP(6)").
                ValueGeneratedOnAddOrUpdate()
        End Sub

    End Class

    Public Class VatReturnLineConfiguration
        Implements IEntityTypeConfiguration(Of VatReturnLine)

        Public Sub Configure(builder As EntityTypeBuilder(Of VatReturnLine)) Implements IEntityTypeConfiguration(Of VatReturnLine).Configure
            builder.ToTable("Acc_VatReturnLines")

            builder.HasKey(Function(l) l.Id)

            builder.Property(Function(l) l.SourceModule).IsRequired().HasMaxLength(50)
            builder.Property(Function(l) l.SourceTable).IsRequired().HasMaxLength(100)

            builder.Property(Function(l) l.VatableAmount).HasPrecision(18, 2)
            builder.Property(Function(l) l.VatExemptAmount).HasPrecision(18, 2)
            builder.Property(Function(l) l.ZeroRatedAmount).HasPrecision(18, 2)
            builder.Property(Function(l) l.OutputVat).HasPrecision(18, 2)
            builder.Property(Function(l) l.InputVat).HasPrecision(18, 2)

            builder.HasIndex(Function(l) l.VatReturnId)
            builder.HasIndex(Function(l) New With {l.SourceModule, l.SourceTable, l.SourceRowId})
        End Sub

    End Class

End Namespace
