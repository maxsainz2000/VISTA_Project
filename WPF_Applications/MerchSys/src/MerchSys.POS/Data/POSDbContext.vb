Imports Microsoft.EntityFrameworkCore
Imports MerchSys.SharedKernel.Data
Imports MerchSys.POS.Entities
Imports MerchSys.POS.Data.Interceptors

Namespace Data

    ''' <summary>
    ''' EF Core DbContext for the POS module.
    ''' All tables use the <c>Pos_</c> prefix to prevent naming collisions with other modules
    ''' in the shared <c>merchsys.db</c> SQLite file.
    ''' <para>
    ''' Registers <see cref="ImmutableReceiptInterceptor"/> to enforce NIRC §235 immutability
    ''' of <see cref="OfficialReceipt"/> and <see cref="ReceiptIntegrity"/> at the ORM layer.
    ''' </para>
    ''' </summary>
    Public Class POSDbContext
        Inherits BaseDbContext

        Public Property SalesTransactions As DbSet(Of SalesTransaction)
        Public Property SalesTransactionLines As DbSet(Of SalesTransactionLine)
        Public Property OfficialReceipts As DbSet(Of OfficialReceipt)
        Public Property CreditAccounts As DbSet(Of CreditAccount)
        Public Property CreditPayments As DbSet(Of CreditPayment)
        Public Property SalesReturns As DbSet(Of SalesReturn)
        Public Property ReceiptIntegrities As DbSet(Of ReceiptIntegrity)
        Public Property ReceiptSequences As DbSet(Of ReceiptSequence)
        Public Property OfficialReceiptArchives As DbSet(Of OfficialReceiptArchive)
        Public Property ReceiptIntegrityArchives As DbSet(Of ReceiptIntegrityArchive)
        Public Property VatConfigurations As DbSet(Of VatConfiguration)

        Public Sub New(options As DbContextOptions(Of POSDbContext))
            MyBase.New(options)
        End Sub

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            optionsBuilder.AddInterceptors(New ImmutableReceiptInterceptor())
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            MyBase.OnModelCreating(modelBuilder)
            modelBuilder.ApplyConfigurationsFromAssembly(GetType(POSDbContext).Assembly)
        End Sub

    End Class

End Namespace
