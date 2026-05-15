#If DEBUG Then
Imports System.IO
Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports Microsoft.Extensions.Logging
Imports MerchSys.Accounting.Data
Imports MerchSys.Inventory.Data
Imports MerchSys.Inventory.Entities
Imports MerchSys.Inventory.Services
Imports MerchSys.POS.Data
Imports MerchSys.POS.Entities
Imports MerchSys.POS.Services
Imports MerchSys.Purchasing.Data
Imports MerchSys.Purchasing.Dtos
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Services
Imports MerchSys.Purchasing.Services.Vat
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.App.Services

Namespace Debug

    ''' <summary>
    ''' Shared flag store injected as a Singleton into the scratch ServiceProvider.
    ''' Set by probe handlers when their respective events fire during a harness run.
    ''' </summary>
    Public Class ChainProbe
        Public Property GoodsReceivedFired As Boolean
        Public Property SaleCompletedFired As Boolean
    End Class

    ''' <summary>
    ''' Probe handler that sets <see cref="ChainProbe.GoodsReceivedFired"/> when
    ''' <see cref="GoodsReceivedEvent"/> is dispatched by MediatR.
    ''' Registered in the scratch DI container before <c>BuildServiceProvider</c> so that it is
    ''' already wired at the time the producer fires — MediatR resolves all handlers at dispatch time.
    ''' </summary>
    Friend Class GoodsReceivedProbeHandler
        Implements INotificationHandler(Of GoodsReceivedEvent)

        Private ReadOnly _probe As ChainProbe

        Public Sub New(probe As ChainProbe)
            _probe = probe
        End Sub

        Public Function Handle(notification As GoodsReceivedEvent,
                               cancellationToken As CancellationToken) As Task _
            Implements INotificationHandler(Of GoodsReceivedEvent).Handle
            _probe.GoodsReceivedFired = True
            Return Task.CompletedTask
        End Function

    End Class

    ''' <summary>
    ''' Probe handler that sets <see cref="ChainProbe.SaleCompletedFired"/> when
    ''' <see cref="SaleCompletedEvent"/> is dispatched by MediatR.
    ''' Registered before <c>BuildServiceProvider</c> for the same reason as
    ''' <see cref="GoodsReceivedProbeHandler"/>.
    ''' </summary>
    Friend Class SaleCompletedProbeHandler
        Implements INotificationHandler(Of SaleCompletedEvent)

        Private ReadOnly _probe As ChainProbe

        Public Sub New(probe As ChainProbe)
            _probe = probe
        End Sub

        Public Function Handle(notification As SaleCompletedEvent,
                               cancellationToken As CancellationToken) As Task _
            Implements INotificationHandler(Of SaleCompletedEvent).Handle
            _probe.SaleCompletedFired = True
            Return Task.CompletedTask
        End Function

    End Class

    ''' <summary>
    ''' No-op stub for <see cref="ILowStockNotifier"/> used inside the harness.
    ''' <see cref="WpfLowStockNotifier"/> requires a live WPF dispatcher; the harness
    ''' builds its own headless <c>ServiceProvider</c>, so the real notifier would fault.
    ''' </summary>
    Friend Class HarnessLowStockNotifier
        Implements ILowStockNotifier

        Public Sub NotifyLowStock(alertCount As Integer) Implements ILowStockNotifier.NotifyLowStock
            ' Intentionally empty — harness runs with its own headless ServiceProvider.
        End Sub

    End Class

    ''' <summary>
    ''' Verifies the two core runtime event chains end-to-end against a fresh scratch SQLite
    ''' database shared across both runs:
    ''' <list type="bullet">
    '''   <item>
    '''     <see cref="VerifyGoodsReceivedChainAsync"/> — PO draft → submit → receive goods →
    '''     <see cref="GoodsReceivedEvent"/> → <c>GoodsReceivedHandler</c> →
    '''     <c>Inv_StockMovements</c> row with <c>Type=Receipt</c>.
    '''   </item>
    '''   <item>
    '''     <see cref="VerifySaleCompletedChainAsync"/> — insert <c>SalesTransaction</c> →
    '''     <c>IPaymentService.ProcessPaymentAsync</c> → <see cref="SaleCompletedEvent"/> →
    '''     <c>SaleCompletedHandler</c> → <c>Inv_StockMovements</c> row with <c>Type=Sale</c>.
    '''   </item>
    ''' </list>
    ''' Prerequisites: <see cref="VerifyGoodsReceivedChainAsync"/> must execute first on the
    ''' shared scratch DB so that positive stock is available for FIFO deduction in the sale chain.
    ''' All module services are real; the only stubs are <see cref="HarnessLowStockNotifier"/>
    ''' (no WPF dispatcher in the headless context) and the probe handlers.
    ''' </summary>
    Public Class EventChainVerificationHarness

        Private ReadOnly _host As IHost
        Private _scratchPath As String
        Private _scratchServiceProvider As ServiceProvider
        Private _probe As ChainProbe
        Private _seededProductId As Integer
        Private _seededVendorId As Integer

        Public Sub New(host As IHost)
            _host = host
        End Sub

        ''' <summary>
        ''' Verifies the GoodsReceived chain. Seeds a one-product / one-vendor scratch DB, creates a
        ''' PO (Draft → Submitted), calls <see cref="IGoodsReceivingService.ReceiveGoodsAsync"/>, then
        ''' confirms a <c>StockMovement</c> row with <c>Type=Receipt</c> exists in
        ''' <c>Inv_StockMovements</c>. The scratch DB is retained for
        ''' <see cref="VerifySaleCompletedChainAsync"/>.
        ''' </summary>
        Public Async Function VerifyGoodsReceivedChainAsync() As Task(Of ChainVerificationResult)
            Dim result As New ChainVerificationResult() With {
                .ChainName = "GoodsReceived",
                .ExpectedMovementType = "Receipt"
            }
            Dim sw = System.Diagnostics.Stopwatch.StartNew()
            Dim runError As Exception = Nothing

            Try
                _scratchPath = Path.Combine(Path.GetTempPath(),
                    $"vista-event-chain-harness-{Guid.NewGuid():N}.db")

                BuildScratchServices(_scratchPath)

                ' ── Schema creation (each DbContext owns its own table prefix) ──────────
                Using scope = _scratchServiceProvider.CreateScope()
                    Await scope.ServiceProvider.GetRequiredService(Of PurchasingDbContext)().
                        Database.EnsureCreatedAsync()
                    Await scope.ServiceProvider.GetRequiredService(Of InventoryDbContext)().
                        Database.EnsureCreatedAsync()
                    Await scope.ServiceProvider.GetRequiredService(Of POSDbContext)().
                        Database.EnsureCreatedAsync()
                    Await scope.ServiceProvider.GetRequiredService(Of AccountingDbContext)().
                        Database.EnsureCreatedAsync()
                End Using

                ' ── Seed: ProductCategory, Product (Inventory), Vendor (Purchasing) ─────
                Using scope = _scratchServiceProvider.CreateScope()
                    Dim invCtx = scope.ServiceProvider.GetRequiredService(Of InventoryDbContext)()
                    Dim purCtx = scope.ServiceProvider.GetRequiredService(Of PurchasingDbContext)()

                    Dim category As New ProductCategory() With {
                        .Name = "Harness-Category",
                        .Description = "Auto-seeded by EventChainVerificationHarness"
                    }
                    invCtx.ProductCategories.Add(category)
                    Await invCtx.SaveChangesAsync()

                    Dim product As New Product() With {
                        .Name = "Harness-Product",
                        .Sku = $"HAR-{Guid.NewGuid():N}".Substring(0, 20),
                        .CategoryId = category.Id,
                        .RetailPrice = 100D,
                        .Unit = "bag",
                        .HasExpiry = False,
                        .MinimumThreshold = 5,
                        .IsActive = True
                    }
                    invCtx.Products.Add(product)
                    Await invCtx.SaveChangesAsync()
                    _seededProductId = product.Id

                    Dim vendor As New Vendor() With {
                        .Name = $"Harness-Vendor-{Guid.NewGuid():N}".Substring(0, 30),
                        .ContactPerson = "Harness Contact",
                        .Phone = "09000000000",
                        .DefaultLeadTimeDays = 1
                    }
                    purCtx.Vendors.Add(vendor)
                    Await purCtx.SaveChangesAsync()
                    _seededVendorId = vendor.Id
                End Using

                ' ── GoodsReceived chain ───────────────────────────────────────────────
                Using scope = _scratchServiceProvider.CreateScope()
                    Dim poService = scope.ServiceProvider.GetRequiredService(Of IPurchaseOrderService)()
                    Dim grService = scope.ServiceProvider.GetRequiredService(Of IGoodsReceivingService)()
                    Dim invCtx = scope.ServiceProvider.GetRequiredService(Of InventoryDbContext)()

                    Dim poLines As New List(Of CreatePOLineDto) From {
                        New CreatePOLineDto() With {
                            .ProductId = _seededProductId,
                            .ProductName = "Harness-Product",
                            .QuantityOrdered = 10,
                            .UnitCost = 80D
                        }
                    }
                    Dim po = Await poService.CreateDraftAsync(_seededVendorId, poLines)
                    Await poService.SubmitAsync(po.Id)

                    ' ReceiveGoodsAsync publishes GoodsReceivedEvent → GoodsReceivedHandler
                    ' → StockService.AddStockBatchAsync → Inv_StockMovements (Type=Receipt)
                    Dim grLines As New List(Of ReceiveGoodsLineDto) From {
                        New ReceiveGoodsLineDto() With {
                            .ProductId = _seededProductId,
                            .ProductName = "Harness-Product",
                            .QuantityOrdered = 10,
                            .QuantityReceived = 10,
                            .UnitCost = 80D
                        }
                    }
                    Await grService.ReceiveGoodsAsync(po.Id, grLines)

                    Dim movementRow = Await invCtx.StockMovements.FirstOrDefaultAsync(
                        Function(m) m.ProductId = _seededProductId AndAlso
                                    m.MovementType = MovementType.Receipt)

                    result.PublisherFired = _probe.GoodsReceivedFired
                    result.HandlerExecuted = (movementRow IsNot Nothing)
                    result.StockMovementRowFound = (movementRow IsNot Nothing)
                    result.ActualMovementType = If(movementRow IsNot Nothing,
                        movementRow.MovementType.ToString(), "None")
                    result.Detail = If(movementRow IsNot Nothing,
                        $"StockMovement Id={movementRow.Id}, Qty=+{movementRow.Quantity}, OccurredAt={movementRow.OccurredAt:u}",
                        "No StockMovement row with Type=Receipt found.")
                End Using

            Catch ex As Exception
                runError = ex
                result.Detail = $"Exception: {ex.GetType().Name}: {ex.Message}"
            End Try

            sw.Stop()
            result.DurationMs = sw.ElapsedMilliseconds
            result.Passed = result.PublisherFired AndAlso
                            result.HandlerExecuted AndAlso
                            result.StockMovementRowFound AndAlso
                            runError Is Nothing

            If runError IsNot Nothing Then
                System.Console.WriteLine($"[EventChainHarness] GoodsReceived FAILED: {runError.Message}")
            End If

            Return result
        End Function

        ''' <summary>
        ''' Verifies the SaleCompleted chain. Inserts a <see cref="SalesTransaction"/> directly into
        ''' <c>POSDbContext</c> (bypassing <c>CartService</c> to avoid receipt-numbering infrastructure
        ''' not available in the harness context), then calls
        ''' <see cref="IPaymentService.ProcessPaymentAsync"/> which publishes
        ''' <see cref="SaleCompletedEvent"/> → <c>SaleCompletedHandler</c> →
        ''' <c>StockService.DeductStockFIFOAsync</c> → <c>Inv_StockMovements</c> row with
        ''' <c>Type=Sale</c> and negative quantity.
        ''' Must be called after <see cref="VerifyGoodsReceivedChainAsync"/> on the same scratch DB.
        ''' </summary>
        Public Async Function VerifySaleCompletedChainAsync() As Task(Of ChainVerificationResult)
            Dim result As New ChainVerificationResult() With {
                .ChainName = "SaleCompleted",
                .ExpectedMovementType = "Sale"
            }
            Dim sw = System.Diagnostics.Stopwatch.StartNew()
            Dim runError As Exception = Nothing

            Try
                If _scratchServiceProvider Is Nothing Then
                    Throw New InvalidOperationException(
                        "VerifyGoodsReceivedChainAsync must be called before VerifySaleCompletedChainAsync.")
                End If

                Using scope = _scratchServiceProvider.CreateScope()
                    Dim posCtx = scope.ServiceProvider.GetRequiredService(Of POSDbContext)()
                    Dim invCtx = scope.ServiceProvider.GetRequiredService(Of InventoryDbContext)()
                    Dim paymentService = scope.ServiceProvider.GetRequiredService(Of IPaymentService)()

                    ' Insert SalesTransaction directly — CartService.FinalizeAsync requires
                    ' receipt-numbering infrastructure (IReceiptService, IReceiptIntegrityService)
                    ' that is not part of the event chain under test.
                    Const saleQty As Integer = 3
                    Const unitPrice As Decimal = 100D
                    Dim txTotal = CDec(saleQty) * unitPrice

                    Dim tx As New SalesTransaction() With {
                        .TransactionNumber = $"TX-{DateTime.UtcNow.Year}-HARNESS",
                        .TransactionDate = DateTime.UtcNow,
                        .PaymentMethod = PaymentMethod.Cash,
                        .SubTotal = txTotal,
                        .DiscountAmount = 0D,
                        .VatAmount = 0D,
                        .TotalAmount = txTotal,
                        .AmountTendered = txTotal,
                        .ChangeAmount = 0D,
                        .IsVoided = False
                    }
                    tx.Lines.Add(New SalesTransactionLine() With {
                        .ProductId = _seededProductId,
                        .ProductName = "Harness-Product",
                        .Quantity = saleQty,
                        .UnitPrice = unitPrice,
                        .DiscountAmount = 0D,
                        .LineTotal = txTotal
                    })
                    posCtx.SalesTransactions.Add(tx)
                    Await posCtx.SaveChangesAsync()

                    ' ProcessPaymentAsync publishes SaleCompletedEvent → SaleCompletedHandler
                    ' → StockService.DeductStockFIFOAsync → Inv_StockMovements (Type=Sale, Qty=-3)
                    Await paymentService.ProcessPaymentAsync(tx.Id, PaymentMethod.Cash, txTotal)

                    Dim movementRow = Await invCtx.StockMovements.FirstOrDefaultAsync(
                        Function(m) m.ProductId = _seededProductId AndAlso
                                    m.MovementType = MovementType.Sale)

                    result.PublisherFired = _probe.SaleCompletedFired
                    result.HandlerExecuted = (movementRow IsNot Nothing)
                    result.StockMovementRowFound = (movementRow IsNot Nothing)
                    result.ActualMovementType = If(movementRow IsNot Nothing,
                        movementRow.MovementType.ToString(), "None")
                    result.Detail = If(movementRow IsNot Nothing,
                        $"StockMovement Id={movementRow.Id}, Qty={movementRow.Quantity}, OccurredAt={movementRow.OccurredAt:u}",
                        "No StockMovement row with Type=Sale found.")
                End Using

            Catch ex As Exception
                runError = ex
                result.Detail = $"Exception: {ex.GetType().Name}: {ex.Message}"
            End Try

            sw.Stop()
            result.DurationMs = sw.ElapsedMilliseconds
            result.Passed = result.PublisherFired AndAlso
                            result.HandlerExecuted AndAlso
                            result.StockMovementRowFound AndAlso
                            runError Is Nothing

            If runError IsNot Nothing Then
                System.Console.WriteLine($"[EventChainHarness] SaleCompleted FAILED: {runError.Message}")
            End If

            Return result
        End Function

        ''' <summary>
        ''' Disposes the scratch <see cref="ServiceProvider"/> and deletes all scratch DB files
        ''' (main, WAL, SHM). Cleanup exceptions are swallowed and logged to console so that a
        ''' locked file on Windows does not mask the harness result.
        ''' </summary>
        Public Async Function CleanupAsync() As Task
            If _scratchServiceProvider IsNot Nothing Then
                Await _scratchServiceProvider.DisposeAsync()
                _scratchServiceProvider = Nothing
            End If

            Dim cleanupEx As Exception = Nothing
            Try
                If _scratchPath IsNot Nothing AndAlso File.Exists(_scratchPath) Then
                    File.Delete(_scratchPath)
                End If
                Dim wal = _scratchPath & "-wal"
                Dim shm = _scratchPath & "-shm"
                If File.Exists(wal) Then File.Delete(wal)
                If File.Exists(shm) Then File.Delete(shm)
            Catch ex As Exception
                cleanupEx = ex
            End Try

            If cleanupEx IsNot Nothing Then
                System.Console.WriteLine($"[EventChainHarness] Warning: scratch DB cleanup failed: {cleanupEx.Message}")
            End If
        End Function

        ' ── Private helpers ──────────────────────────────────────────────────────────────

        Private Sub BuildScratchServices(scratchPath As String)
            Dim connStr = $"Data Source={scratchPath}"
            Dim services As New ServiceCollection()

            services.AddLogging(Sub(b) b.SetMinimumLevel(LogLevel.Warning))

            ' All four module DbContexts share the same scratch SQLite file via their table prefixes.
            services.AddDbContext(Of PurchasingDbContext)(Sub(o) o.UseSqlite(connStr), ServiceLifetime.Scoped)
            services.AddDbContext(Of InventoryDbContext)(Sub(o) o.UseSqlite(connStr), ServiceLifetime.Scoped)
            services.AddDbContext(Of POSDbContext)(Sub(o) o.UseSqlite(connStr), ServiceLifetime.Scoped)
            services.AddDbContext(Of AccountingDbContext)(Sub(o) o.UseSqlite(connStr), ServiceLifetime.Scoped)

            ' Probe registered as Singleton before MediatR so it is resolvable by probe handlers.
            _probe = New ChainProbe()
            services.AddSingleton(_probe)

            ' MediatR with all handler assemblies — mirrors MediatRConfig.AddMediatRServices().
            services.AddMediatR(Sub(cfg)
                                    cfg.RegisterServicesFromAssembly(
                                        GetType(GoodsReceivedEvent).Assembly)
                                    cfg.RegisterServicesFromAssembly(
                                        GetType(PurchasingDbContext).Assembly)
                                    cfg.RegisterServicesFromAssembly(
                                        GetType(InventoryDbContext).Assembly)
                                    cfg.RegisterServicesFromAssembly(
                                        GetType(POSDbContext).Assembly)
                                    cfg.RegisterServicesFromAssembly(
                                        GetType(AccountingDbContext).Assembly)
                                End Sub)

            ' Probe handlers appended after real handlers; MediatR invokes ALL handlers per event.
            services.AddTransient(Of INotificationHandler(Of GoodsReceivedEvent),
                                      GoodsReceivedProbeHandler)()
            services.AddTransient(Of INotificationHandler(Of SaleCompletedEvent),
                                      SaleCompletedProbeHandler)()

            ' Purchasing services
            services.AddScoped(Of IPurchaseOrderService, PurchaseOrderService)()
            services.AddScoped(Of IPriceChangeService, PriceChangeService)()
            services.AddScoped(Of IGoodsReceivingService, GoodsReceivingService)()
            services.AddScoped(Of GoodsReceiptVatCalculator)()

            ' Inventory services
            services.AddScoped(Of IStockService, StockService)()
            services.AddScoped(Of ILowStockAlertService, LowStockAlertService)()
            services.AddSingleton(Of ILowStockNotifier, HarnessLowStockNotifier)()

            ' POS / shared services
            services.AddScoped(Of IEventBus, MediatREventBus)()
            services.AddScoped(Of IPaymentService, PaymentService)()

            _scratchServiceProvider = services.BuildServiceProvider()
        End Sub

    End Class

    ''' <summary>
    ''' Result of a single event-chain verification pass.
    ''' Each Boolean property independently identifies which stage of the chain passed or failed,
    ''' enabling partial-pass diagnostics without re-running the full harness.
    ''' </summary>
    Public Class ChainVerificationResult
        Public Property ChainName As String
        Public Property Passed As Boolean
        Public Property PublisherFired As Boolean
        Public Property HandlerExecuted As Boolean
        Public Property StockMovementRowFound As Boolean
        Public Property ExpectedMovementType As String
        Public Property ActualMovementType As String
        Public Property DurationMs As Long
        Public Property Detail As String
    End Class

End Namespace
#End If
