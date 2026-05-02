Imports Microsoft.EntityFrameworkCore
Imports MerchSys.SharedKernel.Data
Imports MerchSys.POS.Entities

Namespace Data

    ''' <summary>
    ''' EF Core DbContext for the POS module.
    ''' All tables use the <c>Pos_</c> prefix to prevent naming collisions with other modules
    ''' in the shared <c>merchsys.db</c> SQLite file.
    ''' </summary>
    Public Class POSDbContext
        Inherits BaseDbContext

        Public Property SalesTransactions As DbSet(Of SalesTransaction)
        Public Property SalesTransactionLines As DbSet(Of SalesTransactionLine)
        Public Property OfficialReceipts As DbSet(Of OfficialReceipt)
        Public Property CreditAccounts As DbSet(Of CreditAccount)
        Public Property CreditPayments As DbSet(Of CreditPayment)
        Public Property SalesReturns As DbSet(Of SalesReturn)

        Public Sub New(options As DbContextOptions(Of POSDbContext))
            MyBase.New(options)
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            MyBase.OnModelCreating(modelBuilder)
            modelBuilder.ApplyConfigurationsFromAssembly(GetType(POSDbContext).Assembly)
        End Sub

    End Class

End Namespace
