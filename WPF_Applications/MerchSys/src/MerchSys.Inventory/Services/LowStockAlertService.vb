Imports System.Threading
Imports MySqlConnector
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Inventory.Data
Imports MerchSys.Inventory.Entities
Imports MerchSys.SharedKernel.Persistence

Namespace Services

    Public Class LowStockAlertService
        Implements ILowStockAlertService

        Private ReadOnly _db As InventoryDbContext
        Private ReadOnly _stockService As IStockService
        Private ReadOnly _notifier As ILowStockNotifier
        Private ReadOnly _logger As ILogger(Of LowStockAlertService)
        Private _alertConfigList As List(Of StockAlertConfig)

        Public Sub New(db As InventoryDbContext,
                       stockService As IStockService,
                       notifier As ILowStockNotifier,
                       logger As ILogger(Of LowStockAlertService))
            _db = db
            _stockService = stockService
            _notifier = notifier
            _logger = logger
        End Sub

        ''' <summary>
        ''' Evaluates current stock against alert thresholds and fires a toast when alerts exist.
        ''' Call this after goods are sold or shrinkage is recorded.
        ''' </summary>
        Public Async Function CheckAndGenerateAlertsAsync() As Task(Of List(Of LowStockAlertDto)) Implements ILowStockAlertService.CheckAndGenerateAlertsAsync
            Dim alerts As List(Of LowStockAlertDto) = Await BuildAlertsAsync()
            If alerts.Count > 0 Then
                If _notifier IsNot Nothing Then
                    _notifier.NotifyLowStock(alerts.Count)
                End If
                _logger.LogWarning("Low stock check: {Count} product(s) at or below minimum threshold.", alerts.Count)
            End If
            Return alerts
        End Function

        Public Async Function GetCurrentAlertsAsync() As Task(Of List(Of LowStockAlertDto)) Implements ILowStockAlertService.GetCurrentAlertsAsync
            Return Await BuildAlertsAsync()
        End Function

        ''' <summary>
        ''' Upserts the StockAlertConfig for the product and keeps Product.MinimumThreshold in sync.
        ''' </summary>
        Public Async Function UpdateThresholdAsync(productId As Integer, newThreshold As Integer) As Task Implements ILowStockAlertService.UpdateThresholdAsync
            Dim config As StockAlertConfig = Await _db.StockAlertConfigs.FirstOrDefaultAsync(Function(c) c.ProductId = productId)

            If config Is Nothing Then
                config = New StockAlertConfig With {
                    .ProductId = productId,
                    .MinimumThreshold = newThreshold,
                    .IsAlertEnabled = True
                }
                _db.StockAlertConfigs.Add(config)
            Else
                config.MinimumThreshold = newThreshold
            End If

            Dim product As Product = Await _db.Products.FindAsync(productId)
            If product IsNot Nothing Then
                product.MinimumThreshold = newThreshold
            End If

            Await _db.SaveChangesAsync()
            _logger.LogInformation("Threshold updated: ProductId={ProductId}, Threshold={Threshold}", productId, newThreshold)
        End Function

        Private Async Function BuildAlertsAsync() As Task(Of List(Of LowStockAlertDto))
            Dim stockLevels As List(Of StockLevelDto) = Await _stockService.GetCurrentStockAsync(Nothing)

            _alertConfigList = New List(Of StockAlertConfig)()
            Dim acConnStr = _db.Database.GetConnectionString()
            Using acConn As New MySqlConnection(acConnStr)
                Await acConn.OpenAsync()
                Using acCmd = acConn.CreateCommand()
                    acCmd.CommandText = "SELECT Id, ProductId, MinimumThreshold, ExpiryAlertDays, IsAlertEnabled, " &
                                        "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                        "FROM Inv_StockAlertConfigs WHERE IsAlertEnabled = 1"
                    Using acReader = acCmd.ExecuteReader()
                        While acReader.Read()
                            _alertConfigList.Add(New StockAlertConfig With {
                                .Id = acReader.GetInt32(0),
                                .ProductId = acReader.GetInt32(1),
                                .MinimumThreshold = acReader.GetInt32(2),
                                .ExpiryAlertDays = acReader.GetInt32(3),
                                .IsAlertEnabled = acReader.GetBoolean(4),
                                .CreatedBy = acReader.GetString(5),
                                .CreatedAt = acReader.GetDateTime(6),
                                .ModifiedBy = If(acReader.IsDBNull(7), Nothing, acReader.GetString(7)),
                                .ModifiedAt = If(acReader.IsDBNull(8), Nothing, CType(acReader.GetDateTime(8), DateTime?))
                            })
                        End While
                    End Using
                End Using
            End Using
            Dim alertConfigs As Dictionary(Of Integer, StockAlertConfig) = _alertConfigList.ToDictionary(Function(c) c.ProductId)

            Dim lastRestockLookup As Dictionary(Of Integer, DateTime) =
                (Await _db.StockBatches.
                    GroupBy(Function(b) b.ProductId).
                    Select(Function(g) New With {
                        .ProductId = g.Key,
                        .LastDate = g.Max(Function(b) b.ReceiptDate)
                    }).
                    ToListAsync()).
                    ToDictionary(Function(x) x.ProductId, Function(x) x.LastDate)

            Dim alerts As New List(Of LowStockAlertDto)()

            For Each level In stockLevels
                Dim threshold As Integer
                Dim config As StockAlertConfig = Nothing

                If alertConfigs.TryGetValue(level.ProductId, config) Then
                    threshold = config.MinimumThreshold
                Else
                    threshold = level.MinimumThreshold
                End If

                If level.CurrentQuantity <= threshold Then
                    Dim lastRestock As DateTime? = Nothing
                    Dim lastRestockDate As DateTime
                    If lastRestockLookup.TryGetValue(level.ProductId, lastRestockDate) Then
                        lastRestock = lastRestockDate
                    End If

                    alerts.Add(New LowStockAlertDto With {
                        .ProductId = level.ProductId,
                        .ProductName = level.ProductName,
                        .CurrentStock = level.CurrentQuantity,
                        .MinimumThreshold = threshold,
                        .Deficit = threshold - level.CurrentQuantity,
                        .LastRestockDate = lastRestock
                    })
                End If
            Next

            Return alerts.OrderByDescending(Function(a) a.Deficit).ToList()
        End Function

    End Class

End Namespace
