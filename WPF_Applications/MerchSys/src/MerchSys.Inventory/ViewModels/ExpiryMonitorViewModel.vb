Imports System.Collections.ObjectModel
Imports System.Threading
Imports System.Timers
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Inventory.Services

Namespace ViewModels

    ''' <summary>
    ''' Flat bindable row for the expiry monitor DataGrids.
    ''' UrgencyLevel drives row color: "Red" (&lt;=7 days or expired), "Orange" (8-14 days), "Yellow" (15-30 days).
    ''' </summary>
    Public Class ExpiryRowItem
        Inherits ObservableObject

        Public Property BatchId As Integer
        Public Property ProductName As String
        Public Property QtyRemaining As Integer
        Public Property UnitCost As Decimal
        Public Property Value As Decimal
        Public Property ExpiryDate As DateTime
        Public Property DaysRemaining As Integer
        Public Property UrgencyLevel As String

    End Class

    ''' <summary>
    ''' ViewModel for the Expiry Monitor screen.
    ''' Shows near-expiry and expired batches; provides one-click write-off to shrinkage.
    ''' Auto-refreshes every 60 seconds.
    ''' </summary>
    Public Class ExpiryMonitorViewModel
        Inherits ObservableObject

        Private ReadOnly _expiryService As IExpiryTrackingService
        Private ReadOnly _refreshTimer As System.Timers.Timer
        Private ReadOnly _uiContext As SynchronizationContext

        Public Sub New(expiryService As IExpiryTrackingService)
            _expiryService = expiryService
            _uiContext = SynchronizationContext.Current

            NearExpiryBatches = New ObservableCollection(Of ExpiryRowItem)()
            ExpiredBatches = New ObservableCollection(Of ExpiryRowItem)()

            RefreshCommand = New AsyncRelayCommand(AddressOf LoadDataAsync)
            WriteOffCommand = New AsyncRelayCommand(Of ExpiryRowItem)(AddressOf ExecuteWriteOffAsync)

            _refreshTimer = New System.Timers.Timer(60_000) With {.AutoReset = True}
            AddHandler _refreshTimer.Elapsed, AddressOf OnRefreshTick
            _refreshTimer.Start()

            Dim initTask = LoadDataAsync()
        End Sub

        ' ─── Summary Cards ────────────────────────────────────────────────────────

        Private _nearExpiryCount As Integer
        Public Property NearExpiryCount As Integer
            Get
                Return _nearExpiryCount
            End Get
            Set(value As Integer)
                SetProperty(_nearExpiryCount, value)
            End Set
        End Property

        Private _expiredCount As Integer
        Public Property ExpiredCount As Integer
            Get
                Return _expiredCount
            End Get
            Set(value As Integer)
                SetProperty(_expiredCount, value)
            End Set
        End Property

        Private _totalValueAtRisk As Decimal
        Public Property TotalValueAtRisk As Decimal
            Get
                Return _totalValueAtRisk
            End Get
            Set(value As Decimal)
                SetProperty(_totalValueAtRisk, value)
            End Set
        End Property

        ' ─── Threshold ────────────────────────────────────────────────────────────

        Private _daysThreshold As Integer = 30
        Public Property DaysThreshold As Integer
            Get
                Return _daysThreshold
            End Get
            Set(value As Integer)
                Dim clamped As Integer = Math.Max(1, Math.Min(365, value))
                If SetProperty(_daysThreshold, clamped) Then
                    Dim t = LoadDataAsync()
                End If
            End Set
        End Property

        ' ─── Grid Data ────────────────────────────────────────────────────────────

        Public Property NearExpiryBatches As ObservableCollection(Of ExpiryRowItem)
        Public Property ExpiredBatches As ObservableCollection(Of ExpiryRowItem)

        ' ─── Status ───────────────────────────────────────────────────────────────

        Private _isBusy As Boolean
        Public Property IsBusy As Boolean
            Get
                Return _isBusy
            End Get
            Set(value As Boolean)
                SetProperty(_isBusy, value)
                OnPropertyChanged(NameOf(IsEmpty))
            End Set
        End Property

        Private _isError As Boolean
        Public Property IsError As Boolean
            Get
                Return _isError
            End Get
            Set(value As Boolean)
                SetProperty(_isError, value)
                OnPropertyChanged(NameOf(IsEmpty))
            End Set
        End Property

        Private _errorMessage As String = String.Empty
        Public Property ErrorMessage As String
            Get
                Return _errorMessage
            End Get
            Set(value As String)
                SetProperty(_errorMessage, value)
            End Set
        End Property

        Public ReadOnly Property IsEmpty As Boolean
            Get
                Return NearExpiryBatches.Count = 0 AndAlso ExpiredBatches.Count = 0 AndAlso Not IsBusy AndAlso Not IsError
            End Get
        End Property

        Private _lastRefreshed As String = String.Empty
        Public Property LastRefreshed As String
            Get
                Return _lastRefreshed
            End Get
            Set(value As String)
                SetProperty(_lastRefreshed, value)
            End Set
        End Property

        Private _statusMessage As String = String.Empty
        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Set(value As String)
                SetProperty(_statusMessage, value)
            End Set
        End Property

        Private _isStatusError As Boolean
        Public Property IsStatusError As Boolean
            Get
                Return _isStatusError
            End Get
            Set(value As Boolean)
                SetProperty(_isStatusError, value)
            End Set
        End Property

        ' ─── Commands ─────────────────────────────────────────────────────────────

        Public Property RefreshCommand As AsyncRelayCommand
        Public Property WriteOffCommand As AsyncRelayCommand(Of ExpiryRowItem)

        ' ─── Data Loading ─────────────────────────────────────────────────────────

        Private Async Function LoadDataAsync() As Task
            IsError = False
            IsBusy = True
            Try
                Dim nearTask = _expiryService.GetNearExpiryBatchesAsync(DaysThreshold)
                Dim expiredTask = _expiryService.GetExpiredBatchesAsync()
                Await Task.WhenAll(nearTask, expiredTask)

                Dim nearDtos = nearTask.Result
                Dim expiredDtos = expiredTask.Result

                NearExpiryBatches.Clear()
                For Each dto In nearDtos
                    NearExpiryBatches.Add(New ExpiryRowItem With {
                        .BatchId = dto.BatchId,
                        .ProductName = dto.ProductName,
                        .QtyRemaining = dto.QtyRemaining,
                        .UnitCost = dto.UnitCost,
                        .Value = dto.TotalValue,
                        .ExpiryDate = dto.ExpiryDate,
                        .DaysRemaining = dto.DaysUntilExpiry,
                        .UrgencyLevel = GetUrgencyLevel(dto.DaysUntilExpiry)
                    })
                Next

                ExpiredBatches.Clear()
                For Each dto In expiredDtos
                    ExpiredBatches.Add(New ExpiryRowItem With {
                        .BatchId = dto.BatchId,
                        .ProductName = dto.ProductName,
                        .QtyRemaining = dto.QtyRemaining,
                        .UnitCost = dto.UnitCost,
                        .Value = dto.TotalValue,
                        .ExpiryDate = dto.ExpiryDate,
                        .DaysRemaining = dto.DaysUntilExpiry,
                        .UrgencyLevel = "Red"
                    })
                Next

                NearExpiryCount = NearExpiryBatches.Count
                ExpiredCount = ExpiredBatches.Count

                Dim nearValue As Decimal = 0D
                For Each b In NearExpiryBatches
                    nearValue += b.Value
                Next
                Dim expiredValue As Decimal = 0D
                For Each b In ExpiredBatches
                    expiredValue += b.Value
                Next
                TotalValueAtRisk = nearValue + expiredValue

                LastRefreshed = $"Refreshed {DateTime.Now:HH:mm:ss}"
                IsError = False

            Catch ex As Exception
                ErrorMessage = ex.Message
                IsError = True
            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function ExecuteWriteOffAsync(row As ExpiryRowItem) As Task
            If row Is Nothing Then Return
            IsBusy = True
            StatusMessage = String.Empty
            IsStatusError = False
            Try
                Await _expiryService.WriteOffExpiredBatchAsync(row.BatchId, "Written off via Expiry Monitor")
                StatusMessage = $"Batch #{row.BatchId} ({row.ProductName}) written off. ₱{row.Value:N2} recorded as shrinkage."
                IsStatusError = False
                Await LoadDataAsync()
            Catch ex As Exception
                StatusMessage = $"Write-off failed: {ex.Message}"
                IsStatusError = True
            Finally
                IsBusy = False
            End Try
        End Function

        Private Shared Function GetUrgencyLevel(daysRemaining As Integer) As String
            If daysRemaining <= 7 Then Return "Red"
            If daysRemaining <= 14 Then Return "Orange"
            Return "Yellow"
        End Function

        Private Sub OnRefreshTick(sender As Object, e As ElapsedEventArgs)
            If _uiContext IsNot Nothing Then
                _uiContext.Post(
                    Sub(o)
                        Dim t = LoadDataAsync()
                    End Sub, Nothing)
            End If
        End Sub

    End Class

End Namespace
