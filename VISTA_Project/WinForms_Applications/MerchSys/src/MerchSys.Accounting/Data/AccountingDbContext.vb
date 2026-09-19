Imports Microsoft.EntityFrameworkCore
Imports MerchSys.SharedKernel.Data
Imports MerchSys.Accounting.Entities

Namespace Data

    Public Class AccountingDbContext
        Inherits BaseDbContext

        Public Property FinancialPeriods As DbSet(Of FinancialPeriod)
        Public Property RevenueRecords As DbSet(Of RevenueRecord)
        Public Property ExpenseRecords As DbSet(Of ExpenseRecord)
        Public Property FinancialSnapshots As DbSet(Of FinancialSnapshot)

        Public Sub New(options As DbContextOptions(Of AccountingDbContext), session As MerchSys.SharedKernel.Interfaces.ISessionService)
            MyBase.New(options, session)
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            MyBase.OnModelCreating(modelBuilder)
            modelBuilder.ApplyConfigurationsFromAssembly(GetType(AccountingDbContext).Assembly)
        End Sub

    End Class

End Namespace
