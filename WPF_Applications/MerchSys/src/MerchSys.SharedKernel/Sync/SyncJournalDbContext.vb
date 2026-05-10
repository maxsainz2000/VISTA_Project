Imports Microsoft.EntityFrameworkCore
Imports MerchSys.SharedKernel.Data

Namespace Sync

    ''' <summary>
    ''' Standalone DbContext for the <c>Sync_Journal</c> table.
    ''' Kept separate from all module DbContexts so any layer can append journal rows
    ''' without taking a module-specific dependency.
    ''' </summary>
    Public Class SyncJournalDbContext
        Inherits BaseDbContext

        Public Sub New(options As DbContextOptions(Of SyncJournalDbContext))
            MyBase.New(options)
        End Sub

        Public Property SyncJournalEntries As DbSet(Of SyncJournal)

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            MyBase.OnModelCreating(modelBuilder)

            modelBuilder.Entity(Of SyncJournal)(Sub(e)
                                                    e.ToTable("Sync_Journal")
                                                    e.HasKey(Function(j) j.Id)
                                                    e.Property(Function(j) j.TableName).IsRequired()
                                                    e.Property(Function(j) j.Operation).IsRequired()
                                                    e.Property(Function(j) j.ModuleName).IsRequired()
                                                    e.HasIndex(Function(j) New With {j.ModuleName, j.SyncedAt}).
                                                        HasDatabaseName("IX_Sync_Journal_ModuleName_SyncedAt")
                                                End Sub)
        End Sub

    End Class

End Namespace
