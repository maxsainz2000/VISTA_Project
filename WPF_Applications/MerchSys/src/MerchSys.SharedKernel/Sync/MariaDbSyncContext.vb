Imports System.Data
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.SharedKernel.Sync.SyncMaps

Namespace Sync

    ''' <summary>
    ''' EF Core DbContext targeting the central MariaDB 11.4.x instance via Pomelo.
    ''' All module entity types are mapped here under their canonical Pur_/Inv_/Pos_/Acc_ table names.
    ''' Writes are scripted via <c>Add</c>/<c>Update</c> with <c>SaveChangesAsync</c>;
    ''' query tracking is disabled because the context is push-only in this phase.
    ''' Connection string source: <c>Sync:MariaDbConnection</c> in <c>appsettings.json</c>.
    ''' </summary>
    Public Class MariaDbSyncContext
        Inherits DbContext

        Public Sub New(options As DbContextOptions(Of MariaDbSyncContext))
            MyBase.New(options)
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking
        End Sub

        ' ── Purchasing ────────────────────────────────────────────────────────────────
        Public Property Vendors As DbSet(Of RemoteVendor)
        Public Property PurchaseOrders As DbSet(Of RemotePurchaseOrder)
        Public Property PurchaseOrderLines As DbSet(Of RemotePurchaseOrderLine)
        Public Property GoodsReceipts As DbSet(Of RemoteGoodsReceipt)
        Public Property GoodsReceiptLines As DbSet(Of RemoteGoodsReceiptLine)
        Public Property AccountsPayableEntries As DbSet(Of RemoteAccountsPayableEntry)
        Public Property ReorderConfigs As DbSet(Of RemoteReorderConfig)
        Public Property ReorderSuggestions As DbSet(Of RemoteReorderSuggestion)
        Public Property PriceChangeAlerts As DbSet(Of RemotePriceChangeAlert)

        ' ── Inventory ─────────────────────────────────────────────────────────────────
        Public Property ProductCategories As DbSet(Of RemoteProductCategory)
        Public Property Products As DbSet(Of RemoteProduct)
        Public Property StockBatches As DbSet(Of RemoteStockBatch)
        Public Property ShrinkageRecords As DbSet(Of RemoteShrinkageRecord)
        Public Property StockAlertConfigs As DbSet(Of RemoteStockAlertConfig)
        Public Property StockMovements As DbSet(Of RemoteStockMovement)
        Public Property StockAuditRecords As DbSet(Of RemoteStockAuditRecord)

        ' ── POS ───────────────────────────────────────────────────────────────────────
        Public Property SalesTransactions As DbSet(Of RemoteSalesTransaction)
        Public Property SalesTransactionLines As DbSet(Of RemoteSalesTransactionLine)
        Public Property CreditAccounts As DbSet(Of RemoteCreditAccount)
        Public Property OfficialReceipts As DbSet(Of RemoteOfficialReceipt)
        Public Property CreditPayments As DbSet(Of RemoteCreditPayment)
        Public Property SalesReturns As DbSet(Of RemoteSalesReturn)
        Public Property ReceiptIntegrityRecords As DbSet(Of RemoteReceiptIntegrity)

        ' ── Accounting ────────────────────────────────────────────────────────────────
        Public Property FinancialPeriods As DbSet(Of RemoteFinancialPeriod)
        Public Property RevenueRecords As DbSet(Of RemoteRevenueRecord)
        Public Property ExpenseRecords As DbSet(Of RemoteExpenseRecord)
        Public Property FinancialSnapshots As DbSet(Of RemoteFinancialSnapshot)

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            MyBase.OnModelCreating(modelBuilder)
            ConfigurePurchasing(modelBuilder)
            ConfigureInventory(modelBuilder)
            ConfigurePOS(modelBuilder)
            ConfigureAccounting(modelBuilder)
        End Sub

        Private Shared Sub ConfigurePurchasing(m As ModelBuilder)
            m.Entity(Of RemoteVendor)(Sub(e)
                                          e.ToTable("Pur_Vendors")
                                          e.HasKey(Function(x) x.Id)
                                          e.Property(Function(x) x.Id).ValueGeneratedNever()
                                      End Sub)
            m.Entity(Of RemotePurchaseOrder)(Sub(e)
                                                 e.ToTable("Pur_PurchaseOrders")
                                                 e.HasKey(Function(x) x.Id)
                                                 e.Property(Function(x) x.Id).ValueGeneratedNever()
                                             End Sub)
            m.Entity(Of RemotePurchaseOrderLine)(Sub(e)
                                                     e.ToTable("Pur_PurchaseOrderLines")
                                                     e.HasKey(Function(x) x.Id)
                                                     e.Property(Function(x) x.Id).ValueGeneratedNever()
                                                 End Sub)
            m.Entity(Of RemoteGoodsReceipt)(Sub(e)
                                                e.ToTable("Pur_GoodsReceipts")
                                                e.HasKey(Function(x) x.Id)
                                                e.Property(Function(x) x.Id).ValueGeneratedNever()
                                            End Sub)
            m.Entity(Of RemoteGoodsReceiptLine)(Sub(e)
                                                    e.ToTable("Pur_GoodsReceiptLines")
                                                    e.HasKey(Function(x) x.Id)
                                                    e.Property(Function(x) x.Id).ValueGeneratedNever()
                                                End Sub)
            m.Entity(Of RemoteAccountsPayableEntry)(Sub(e)
                                                        e.ToTable("Pur_AccountsPayable")
                                                        e.HasKey(Function(x) x.Id)
                                                        e.Property(Function(x) x.Id).ValueGeneratedNever()
                                                    End Sub)
            m.Entity(Of RemoteReorderConfig)(Sub(e)
                                                 e.ToTable("Pur_ReorderConfigs")
                                                 e.HasKey(Function(x) x.Id)
                                                 e.Property(Function(x) x.Id).ValueGeneratedNever()
                                             End Sub)
            m.Entity(Of RemoteReorderSuggestion)(Sub(e)
                                                     e.ToTable("Pur_ReorderSuggestions")
                                                     e.HasKey(Function(x) x.Id)
                                                     e.Property(Function(x) x.Id).ValueGeneratedNever()
                                                 End Sub)
            m.Entity(Of RemotePriceChangeAlert)(Sub(e)
                                                    e.ToTable("Pur_PriceChangeAlerts")
                                                    e.HasKey(Function(x) x.Id)
                                                    e.Property(Function(x) x.Id).ValueGeneratedNever()
                                                End Sub)
        End Sub

        Private Shared Sub ConfigureInventory(m As ModelBuilder)
            m.Entity(Of RemoteProductCategory)(Sub(e)
                                                   e.ToTable("Inv_ProductCategories")
                                                   e.HasKey(Function(x) x.Id)
                                                   e.Property(Function(x) x.Id).ValueGeneratedNever()
                                               End Sub)
            m.Entity(Of RemoteProduct)(Sub(e)
                                           e.ToTable("Inv_Products")
                                           e.HasKey(Function(x) x.Id)
                                           e.Property(Function(x) x.Id).ValueGeneratedNever()
                                       End Sub)
            m.Entity(Of RemoteStockBatch)(Sub(e)
                                              e.ToTable("Inv_StockBatches")
                                              e.HasKey(Function(x) x.Id)
                                              e.Property(Function(x) x.Id).ValueGeneratedNever()
                                          End Sub)
            m.Entity(Of RemoteShrinkageRecord)(Sub(e)
                                                   e.ToTable("Inv_ShrinkageRecords")
                                                   e.HasKey(Function(x) x.Id)
                                                   e.Property(Function(x) x.Id).ValueGeneratedNever()
                                               End Sub)
            m.Entity(Of RemoteStockAlertConfig)(Sub(e)
                                                    e.ToTable("Inv_StockAlertConfigs")
                                                    e.HasKey(Function(x) x.Id)
                                                    e.Property(Function(x) x.Id).ValueGeneratedNever()
                                                End Sub)
            m.Entity(Of RemoteStockMovement)(Sub(e)
                                                 e.ToTable("Inv_StockMovements")
                                                 e.HasKey(Function(x) x.Id)
                                                 e.Property(Function(x) x.Id).ValueGeneratedNever()
                                             End Sub)
            m.Entity(Of RemoteStockAuditRecord)(Sub(e)
                                                    e.ToTable("Inv_StockAuditRecords")
                                                    e.HasKey(Function(x) x.Id)
                                                    e.Property(Function(x) x.Id).ValueGeneratedNever()
                                                End Sub)
        End Sub

        Private Shared Sub ConfigurePOS(m As ModelBuilder)
            m.Entity(Of RemoteSalesTransaction)(Sub(e)
                                                    e.ToTable("Pos_SalesTransactions")
                                                    e.HasKey(Function(x) x.Id)
                                                    e.Property(Function(x) x.Id).ValueGeneratedNever()
                                                End Sub)
            m.Entity(Of RemoteSalesTransactionLine)(Sub(e)
                                                        e.ToTable("Pos_SalesTransactionLines")
                                                        e.HasKey(Function(x) x.Id)
                                                        e.Property(Function(x) x.Id).ValueGeneratedNever()
                                                    End Sub)
            m.Entity(Of RemoteCreditAccount)(Sub(e)
                                                 e.ToTable("Pos_CreditAccounts")
                                                 e.HasKey(Function(x) x.Id)
                                                 e.Property(Function(x) x.Id).ValueGeneratedNever()
                                             End Sub)
            m.Entity(Of RemoteOfficialReceipt)(Sub(e)
                                                   e.ToTable("Pos_OfficialReceipts")
                                                   e.HasKey(Function(x) x.Id)
                                                   e.Property(Function(x) x.Id).ValueGeneratedNever()
                                               End Sub)
            m.Entity(Of RemoteCreditPayment)(Sub(e)
                                                 e.ToTable("Pos_CreditPayments")
                                                 e.HasKey(Function(x) x.Id)
                                                 e.Property(Function(x) x.Id).ValueGeneratedNever()
                                             End Sub)
            m.Entity(Of RemoteSalesReturn)(Sub(e)
                                               e.ToTable("Pos_SalesReturns")
                                               e.HasKey(Function(x) x.Id)
                                               e.Property(Function(x) x.Id).ValueGeneratedNever()
                                           End Sub)
            m.Entity(Of RemoteReceiptIntegrity)(Sub(e)
                                                    e.ToTable("Pos_ReceiptIntegrity")
                                                    e.HasKey(Function(x) x.Id)
                                                    e.Property(Function(x) x.Id).ValueGeneratedNever()
                                                End Sub)
        End Sub

        Private Shared Sub ConfigureAccounting(m As ModelBuilder)
            m.Entity(Of RemoteFinancialPeriod)(Sub(e)
                                                   e.ToTable("Acc_FinancialPeriods")
                                                   e.HasKey(Function(x) x.Id)
                                                   e.Property(Function(x) x.Id).ValueGeneratedNever()
                                               End Sub)
            m.Entity(Of RemoteRevenueRecord)(Sub(e)
                                                 e.ToTable("Acc_RevenueRecords")
                                                 e.HasKey(Function(x) x.Id)
                                                 e.Property(Function(x) x.Id).ValueGeneratedNever()
                                             End Sub)
            m.Entity(Of RemoteExpenseRecord)(Sub(e)
                                                 e.ToTable("Acc_ExpenseRecords")
                                                 e.HasKey(Function(x) x.Id)
                                                 e.Property(Function(x) x.Id).ValueGeneratedNever()
                                             End Sub)
            m.Entity(Of RemoteFinancialSnapshot)(Sub(e)
                                                     e.ToTable("Acc_FinancialSnapshots")
                                                     e.HasKey(Function(x) x.Id)
                                                     e.Property(Function(x) x.Id).ValueGeneratedNever()
                                                 End Sub)
        End Sub

        ''' <summary>
        ''' Queries the remote MariaDB for an existing row in <paramref name="tableName"/>
        ''' with the given <paramref name="entityId"/> (integer PK from the sync journal).
        ''' Returns a <see cref="RemoteRowSnapshot"/> used by <see cref="IConflictResolver"/>
        ''' to apply the per-table conflict policy.
        ''' Uses raw ADO.NET to avoid type-specific DbSet casts.
        ''' </summary>
        Public Async Function FetchRemoteRowAsync(tableName As String, entityId As Long) As Task(Of RemoteRowSnapshot)
            Dim conn = Database.GetDbConnection()
            Dim wasOpen = conn.State = ConnectionState.Open
            If Not wasOpen Then Await conn.OpenAsync()

            Try
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = $"SELECT `ModifiedAt` FROM `{tableName}` WHERE `Id` = @id LIMIT 1"
                    Dim p = cmd.CreateParameter()
                    p.ParameterName = "@id"
                    p.Value = entityId
                    cmd.Parameters.Add(p)

                    Using reader = Await cmd.ExecuteReaderAsync()
                        If Await reader.ReadAsync() Then
                            Dim modAt As DateTime? = Nothing
                            If Not reader.IsDBNull(0) Then modAt = reader.GetDateTime(0)
                            Return New RemoteRowSnapshot With {.Exists = True, .ModifiedAt = modAt}
                        End If
                    End Using
                End Using
                Return New RemoteRowSnapshot With {.Exists = False}
            Finally
                ' Await is not permitted in Finally blocks in VB.NET; use synchronous Close.
                If Not wasOpen Then conn.Close()
            End Try
        End Function

    End Class

End Namespace
