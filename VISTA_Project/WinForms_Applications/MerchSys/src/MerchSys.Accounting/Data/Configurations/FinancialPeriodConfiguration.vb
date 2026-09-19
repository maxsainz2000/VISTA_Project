Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Accounting.Entities

Namespace Data.Configurations

    Public Class FinancialPeriodConfiguration
        Implements IEntityTypeConfiguration(Of FinancialPeriod)

        Public Sub Configure(builder As EntityTypeBuilder(Of FinancialPeriod)) Implements IEntityTypeConfiguration(Of FinancialPeriod).Configure
            builder.ToTable("Acc_FinancialPeriods")

            builder.HasKey(Function(p) p.Id)

            builder.Property(Function(p) p.PeriodType).IsRequired().HasMaxLength(20)
            builder.Property(Function(p) p.TotalRevenue).HasPrecision(18, 2)
            builder.Property(Function(p) p.TotalCOGS).HasPrecision(18, 2)
            builder.Property(Function(p) p.GrossProfit).HasPrecision(18, 2)
            builder.Property(Function(p) p.GrossMarginPercent).HasPrecision(10, 4)
            builder.Property(Function(p) p.TotalExpenses).HasPrecision(18, 2)
            builder.Property(Function(p) p.NetIncome).HasPrecision(18, 2)

            builder.Property(Function(e) e.RowVersion).
                IsRowVersion().
                HasColumnType("TIMESTAMP(6)").
                ValueGeneratedOnAddOrUpdate()
        End Sub

    End Class

End Namespace
