Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Accounting.Entities

Namespace Data.Configurations

    Public Class FinancialSnapshotConfiguration
        Implements IEntityTypeConfiguration(Of FinancialSnapshot)

        Public Sub Configure(builder As EntityTypeBuilder(Of FinancialSnapshot)) Implements IEntityTypeConfiguration(Of FinancialSnapshot).Configure
            builder.ToTable("Acc_FinancialSnapshots")

            builder.HasKey(Function(s) s.Id)

            builder.Property(Function(s) s.TotalAR).HasPrecision(18, 2)
            builder.Property(Function(s) s.TotalAP).HasPrecision(18, 2)
            builder.Property(Function(s) s.InventoryValue).HasPrecision(18, 2)
            builder.Property(Function(s) s.TodayRevenue).HasPrecision(18, 2)
            builder.Property(Function(s) s.MonthToDateRevenue).HasPrecision(18, 2)
            builder.Property(Function(s) s.YearToDateRevenue).HasPrecision(18, 2)

            builder.HasIndex(Function(s) s.SnapshotDate).IsUnique()
        End Sub

    End Class

End Namespace
