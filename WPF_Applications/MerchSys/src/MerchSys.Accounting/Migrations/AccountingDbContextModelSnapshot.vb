Imports System
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports MerchSys.Accounting.Data
Imports MerchSys.Accounting.Entities

Namespace Migrations

    <DbContext(GetType(AccountingDbContext))>
    Partial Class AccountingDbContextModelSnapshot
        Inherits ModelSnapshot

        Protected Overrides Sub BuildModel(modelBuilder As ModelBuilder)
            modelBuilder.HasAnnotation("ProductVersion", "10.0.7")

            modelBuilder.Entity("MerchSys.Accounting.Entities.ExpenseRecord", Sub(b)
                b.Property(Of Integer)("Id").
                    ValueGeneratedOnAdd().
                    HasColumnType("INTEGER")
                b.Property(Of Decimal)("Amount").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of String)("Category").
                    IsRequired().
                    HasMaxLength(50).
                    HasColumnType("TEXT")
                b.Property(Of DateTime)("CreatedAt").
                    HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").
                    HasMaxLength(256).
                    HasColumnType("TEXT")
                b.Property(Of String)("Description").
                    HasMaxLength(500).
                    HasColumnType("TEXT")
                b.Property(Of DateTime?)("ModifiedAt").
                    HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").
                    HasMaxLength(256).
                    HasColumnType("TEXT")
                b.Property(Of DateTime)("RecordDate").
                    HasColumnType("TEXT")
                b.Property(Of String)("SourceModule").
                    IsRequired().
                    HasMaxLength(50).
                    HasColumnType("TEXT")
                b.Property(Of Integer?)("SourceReferenceId").
                    HasColumnType("INTEGER")
                b.HasKey("Id")
                b.ToTable("Acc_ExpenseRecords")
            End Sub)

            modelBuilder.Entity("MerchSys.Accounting.Entities.FinancialPeriod", Sub(b)
                b.Property(Of Integer)("Id").
                    ValueGeneratedOnAdd().
                    HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").
                    HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").
                    HasMaxLength(256).
                    HasColumnType("TEXT")
                b.Property(Of DateTime)("EndDate").
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("GrossMarginPercent").
                    HasPrecision(10, 4).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("GrossProfit").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of Boolean)("IsClosed").
                    HasColumnType("INTEGER")
                b.Property(Of DateTime?)("ModifiedAt").
                    HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").
                    HasMaxLength(256).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("NetIncome").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of String)("PeriodType").
                    IsRequired().
                    HasMaxLength(20).
                    HasColumnType("TEXT")
                b.Property(Of DateTime)("StartDate").
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("TotalCOGS").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("TotalExpenses").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("TotalRevenue").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.HasKey("Id")
                b.ToTable("Acc_FinancialPeriods")
            End Sub)

            modelBuilder.Entity("MerchSys.Accounting.Entities.FinancialSnapshot", Sub(b)
                b.Property(Of Integer)("Id").
                    ValueGeneratedOnAdd().
                    HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").
                    HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").
                    HasMaxLength(256).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("InventoryValue").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of DateTime?)("ModifiedAt").
                    HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").
                    HasMaxLength(256).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("MonthToDateRevenue").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of DateTime)("SnapshotDate").
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("TodayRevenue").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("TotalAP").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("TotalAR").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("YearToDateRevenue").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("SnapshotDate").IsUnique()
                b.ToTable("Acc_FinancialSnapshots")
            End Sub)

            modelBuilder.Entity("MerchSys.Accounting.Entities.RevenueRecord", Sub(b)
                b.Property(Of Integer)("Id").
                    ValueGeneratedOnAdd().
                    HasColumnType("INTEGER")
                b.Property(Of Decimal)("COGS").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of DateTime)("CreatedAt").
                    HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").
                    HasMaxLength(256).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("DiscountAmount").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("GrossAmount").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("GrossProfit").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of DateTime?)("ModifiedAt").
                    HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").
                    HasMaxLength(256).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("NetAmount").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.Property(Of Integer)("PaymentMethod").
                    HasColumnType("INTEGER")
                b.Property(Of Integer)("ProductId").
                    HasColumnType("INTEGER")
                b.Property(Of String)("ProductName").
                    IsRequired().
                    HasMaxLength(200).
                    HasColumnType("TEXT")
                b.Property(Of Integer)("QuantitySold").
                    HasColumnType("INTEGER")
                b.Property(Of DateTime)("RecordDate").
                    HasColumnType("TEXT")
                b.Property(Of Integer)("SourceTransactionId").
                    HasColumnType("INTEGER")
                b.Property(Of Decimal)("VatAmount").
                    HasPrecision(18, 2).
                    HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("ProductId")
                b.HasIndex("RecordDate")
                b.ToTable("Acc_RevenueRecords")
            End Sub)
        End Sub

    End Class

End Namespace
