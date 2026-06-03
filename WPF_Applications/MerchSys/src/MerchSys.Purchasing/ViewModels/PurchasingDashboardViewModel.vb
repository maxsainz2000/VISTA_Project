Imports System.Collections.ObjectModel
Imports System.Threading.Tasks
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Purchasing.Data
Imports MerchSys.Purchasing.Services
Imports MerchSys.SharedKernel.Enums
Imports MySqlConnector
Imports Microsoft.EntityFrameworkCore

Namespace ViewModels

    ''' <summary>
    ''' Flat item for the 6-month purchasing spend trend bar chart.
    ''' Bar heights are pre-computed relative to the period's maximum spend (max = 120px).
    ''' </summary>
    Public Class TrendBarItem
        Public Property Month As String
        Public Property Spend As Decimal
        Public Property SpendBarHeight As Double
    End Class

    ''' <summary>
    ''' Flat item for the top vendors by spend table.
    ''' </summary>
    Public Class TopVendorItem
        Public Property VendorId As Integer
        Public Property VendorName As String
        Public Property POCount As Integer
        Public Property TotalSpend As Decimal
    End Class

    ''' <summary>
    ''' Flat display row for pending and overdue purchase orders.
    ''' </summary>
    Public Class PendingPurchaseOrderRow
        Public Property Id As Integer
        Public Property OrderNumber As String
        Public Property VendorName As String
        Public Property OrderDate As DateTime
        Public Property ExpectedDeliveryDate As DateTime?
        Public Property TotalAmount As Decimal
        Public Property Status As PurchaseOrderStatus
        Public ReadOnly Property IsOverdue As Boolean
            Get
                Return Status = PurchaseOrderStatus.Submitted AndAlso ExpectedDeliveryDate.HasValue AndAlso ExpectedDeliveryDate.Value.Date < DateTime.UtcNow.Date
            End Get
        End Property
    End Class

    ''' <summary>
    ''' ViewModel for the Purchasing Dashboard View.
    ''' Aggregates KPIs, 6-month spend trend, top vendors by spend, and pending/overdue POs.
    ''' </summary>
    Public Class PurchasingDashboardViewModel
        Inherits ObservableObject

        Private ReadOnly _db As PurchasingDbContext
        Private ReadOnly _poService As IPurchaseOrderService
        Private ReadOnly _apService As IAccountsPayableService
        Private ReadOnly _vendorService As IVendorService
        Private ReadOnly _reorderService As IReorderService

        Public Event NavigateToViewRequested As EventHandler(Of String)

        Public Sub New(db As PurchasingDbContext,
                       poService As IPurchaseOrderService,
                       apService As IAccountsPayableService,
                       vendorService As IVendorService,
                       reorderService As IReorderService)

            _db = db
            _poService = poService
            _apService = apService
            _vendorService = vendorService
            _reorderService = reorderService

            MonthlyTrend = New ObservableCollection(Of TrendBarItem)()
            TopVendors = New ObservableCollection(Of TopVendorItem)()
            PendingPurchaseOrders = New ObservableCollection(Of PendingPurchaseOrderRow)()

            RefreshCommand = New AsyncRelayCommand(AddressOf LoadDataAsync)
            NavigateToViewCommand = New RelayCommand(Of String)(AddressOf NavigateToView)

            Dim initTask = LoadDataAsync()
        End Sub

        ' ─── KPI Properties ───────────────────────────────────────────────────────

        Private _totalOutstandingAP As Decimal
        Public Property TotalOutstandingAP As Decimal
            Get
                Return _totalOutstandingAP
            End Get
            Set(value As Decimal)
                SetProperty(_totalOutstandingAP, value)
            End Set
        End Property

        Private _overdueAPAmount As Decimal
        Public Property OverdueAPAmount As Decimal
            Get
                Return _overdueAPAmount
            End Get
            Set(value As Decimal)
                SetProperty(_overdueAPAmount, value)
                OnPropertyChanged(NameOf(HasOverdueAP))
            End Set
        End Property

        Private _overdueAPCount As Integer
        Public Property OverdueAPCount As Integer
            Get
                Return _overdueAPCount
            End Get
            Set(value As Integer)
                SetProperty(_overdueAPCount, value)
            End Set
        End Property

        Public ReadOnly Property HasOverdueAP As Boolean
            Get
                Return OverdueAPAmount > 0
            End Get
        End Property

        Private _pendingDeliveriesCount As Integer
        Public Property PendingDeliveriesCount As Integer
            Get
                Return _pendingDeliveriesCount
            End Get
            Set(value As Integer)
                SetProperty(_pendingDeliveriesCount, value)
            End Set
        End Property

        Private _reorderSuggestionsCount As Integer
        Public Property ReorderSuggestionsCount As Integer
            Get
                Return _reorderSuggestionsCount
            End Get
            Set(value As Integer)
                SetProperty(_reorderSuggestionsCount, value)
            End Set
        End Property

        Private _activeVendorsCount As Integer
        Public Property ActiveVendorsCount As Integer
            Get
                Return _activeVendorsCount
            End Get
            Set(value As Integer)
                SetProperty(_activeVendorsCount, value)
            End Set
        End Property

        Private _averageLeadTimeDays As Double
        Public Property AverageLeadTimeDays As Double
            Get
                Return _averageLeadTimeDays
            End Get
            Set(value As Double)
                SetProperty(_averageLeadTimeDays, value)
            End Set
        End Property

        ' ─── PO Status Counts ─────────────────────────────────────────────────────

        Private _draftPOCount As Integer
        Public Property DraftPOCount As Integer
            Get
                Return _draftPOCount
            End Get
            Set(value As Integer)
                SetProperty(_draftPOCount, value)
            End Set
        End Property

        Private _submittedPOCount As Integer
        Public Property SubmittedPOCount As Integer
            Get
                Return _submittedPOCount
            End Get
            Set(value As Integer)
                SetProperty(_submittedPOCount, value)
            End Set
        End Property

        Private _receivedPOCount As Integer
        Public Property ReceivedPOCount As Integer
            Get
                Return _receivedPOCount
            End Get
            Set(value As Integer)
                SetProperty(_receivedPOCount, value)
            End Set
        End Property

        Private _verifiedPOCount As Integer
        Public Property VerifiedPOCount As Integer
            Get
                Return _verifiedPOCount
            End Get
            Set(value As Integer)
                SetProperty(_verifiedPOCount, value)
            End Set
        End Property

        ' ─── Plain-Language Insights ──────────────────────────────────────────────

        Private _whatThisMeansText As String = String.Empty
        Public Property WhatThisMeansText As String
            Get
                Return _whatThisMeansText
            End Get
            Set(value As String)
                SetProperty(_whatThisMeansText, value)
            End Set
        End Property

        ' ─── Collections ──────────────────────────────────────────────────────────

        Public Property MonthlyTrend As ObservableCollection(Of TrendBarItem)
        Public Property TopVendors As ObservableCollection(Of TopVendorItem)
        Public Property PendingPurchaseOrders As ObservableCollection(Of PendingPurchaseOrderRow)

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
                Return PendingPurchaseOrders.Count = 0 AndAlso Not IsBusy AndAlso Not IsError
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

        ' ─── Commands ─────────────────────────────────────────────────────────────

        Public Property RefreshCommand As AsyncRelayCommand
        Public Property NavigateToViewCommand As RelayCommand(Of String)

        ' ─── Navigation ───────────────────────────────────────────────────────────

        Private Sub NavigateToView(viewName As String)
            If Not String.IsNullOrEmpty(viewName) Then
                RaiseEvent NavigateToViewRequested(Me, viewName)
            End If
        End Sub

        ' ─── Data Loading ─────────────────────────────────────────────────────────

        Private Async Function LoadDataAsync() As Task
            IsError = False
            IsBusy = True
            Dim connStr As String = _db.Database.GetConnectionString()
            Dim errMessage As String = Nothing

            Try
                ' 1. Load Outstanding AP & Overdue AP (using services)
                TotalOutstandingAP = Await _apService.GetTotalOutstandingAsync()

                Dim overdueEntries = Await _apService.GetOverdueAsync()
                OverdueAPAmount = overdueEntries.Sum(Function(e) e.Balance)
                OverdueAPCount = overdueEntries.Count

                ' 2. Load Reorder Suggestions Count
                Dim pendingSuggestions = Await _reorderService.GetPendingSuggestionsAsync()
                ReorderSuggestionsCount = pendingSuggestions.Count

                ' 3. Load Active Vendors & Average Lead Time
                Dim vendors = Await _vendorService.GetAllAsync()
                ActiveVendorsCount = vendors.Count
                AverageLeadTimeDays = If(vendors.Any(), Math.Round(vendors.Average(Function(v) CDbl(v.DefaultLeadTimeDays)), 1), 0.0)

                ' 4. Load POs (Status Counts + Details)
                Dim allPOs = Await _poService.GetAllAsync()
                DraftPOCount = Enumerable.Count(allPOs, Function(p) p.Status = PurchaseOrderStatus.Draft)
                SubmittedPOCount = Enumerable.Count(allPOs, Function(p) p.Status = PurchaseOrderStatus.Submitted)
                ReceivedPOCount = Enumerable.Count(allPOs, Function(p) p.Status = PurchaseOrderStatus.Received)
                VerifiedPOCount = Enumerable.Count(allPOs, Function(p) p.Status = PurchaseOrderStatus.Verified)

                ' Pending Deliveries is count of POs that are Submitted
                PendingDeliveriesCount = SubmittedPOCount

                ' Pending & Overdue PO detail rows
                PendingPurchaseOrders.Clear()
                Dim filteredPOs = allPOs.Where(Function(p) p.Status = PurchaseOrderStatus.Submitted OrElse p.Status = PurchaseOrderStatus.Received OrElse p.Status = PurchaseOrderStatus.Verified).
                                         OrderBy(Function(p) If(p.ExpectedDeliveryDate, DateTime.MaxValue)).
                                         Take(20) ' cap to 20 rows
                For Each po In filteredPOs
                    PendingPurchaseOrders.Add(New PendingPurchaseOrderRow With {
                        .Id = po.Id,
                        .OrderNumber = po.OrderNumber,
                        .VendorName = If(po.Vendor IsNot Nothing, po.Vendor.Name, $"Vendor #{po.VendorId}"),
                        .OrderDate = po.OrderDate,
                        .ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                        .TotalAmount = po.TotalAmount,
                        .Status = po.Status
                    })
                Next

                ' 5. Load 6-Month Spend Trend via raw SQL query reader loop
                Dim trendList = Await LoadMonthlyTrendAsync(connStr)
                MonthlyTrend.Clear()
                For Each item In trendList
                    MonthlyTrend.Add(item)
                Next

                ' 6. Load Top Vendors by Spend via raw SQL query reader loop
                Dim topVendorsList = Await LoadTopVendorsAsync(connStr)
                TopVendors.Clear()
                For Each item In topVendorsList
                    TopVendors.Add(item)
                Next

                ' 7. Build Plain-Language Insights
                WhatThisMeansText = BuildWhatThisMeansText()

                LastRefreshed = $"Refreshed {DateTime.Now:HH:mm:ss}"
            Catch ex As Exception
                errMessage = ex.Message
            Finally
                IsBusy = False
            End Try

            ' Handle error if any occurred (avoids await inside catch block)
            If errMessage IsNot Nothing Then
                LastRefreshed = $"Load failed: {errMessage}"
                ErrorMessage = errMessage
                IsError = True
            End If
        End Function

        Private Async Function LoadMonthlyTrendAsync(connStr As String) As Task(Of List(Of TrendBarItem))
            Dim trendList As New List(Of TrendBarItem)()
            Dim today = DateTime.UtcNow.Date
            Dim trendStart As DateTime = New DateTime(today.Year, today.Month, 1).AddMonths(-5)

            Using conn As New MySqlConnection(connStr)
                Await conn.OpenAsync()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT YEAR(OrderDate) as Yr, MONTH(OrderDate) as Mth, SUM(TotalAmount) as Total " &
                                      "FROM Pur_PurchaseOrders " &
                                      "WHERE IsDeleted = 0 AND Status IN (2,3,4,5) AND OrderDate >= @trendStart " &
                                      "GROUP BY YEAR(OrderDate), MONTH(OrderDate) " &
                                      "ORDER BY Yr ASC, Mth ASC"
                    cmd.Parameters.Add(New MySqlParameter("@trendStart", trendStart))
                    Using reader = Await cmd.ExecuteReaderAsync()
                        Dim dbResults As New Dictionary(Of String, Decimal)()
                        While Await reader.ReadAsync()
                            Dim yr = reader.GetInt32(0)
                            Dim mth = reader.GetInt32(1)
                            Dim total = reader.GetDecimal(2)
                            Dim key = $"{yr}-{mth:D2}"
                            dbResults(key) = total
                        End While

                        For i As Integer = 5 To 0 Step -1
                            Dim targetMonth = today.AddMonths(-i)
                            Dim key = $"{targetMonth.Year}-{targetMonth.Month:D2}"
                            Dim totalAmount = If(dbResults.ContainsKey(key), dbResults(key), 0D)
                            trendList.Add(New TrendBarItem With {
                                .Month = targetMonth.ToString("MMM yyyy"),
                                .Spend = totalAmount
                            })
                        Next
                    End Using
                End Using
            End Using

            ' Pre-compute bar heights relative to max spend (max = 120px)
            Const MaxBarPx As Double = 120.0
            Dim maxSpend As Decimal = trendList.Select(Function(t) t.Spend).DefaultIfEmpty(1D).Max()
            If maxSpend <= 0 Then maxSpend = 1D
            For Each item In trendList
                item.SpendBarHeight = Math.Max(2.0, CDbl(item.Spend / maxSpend) * MaxBarPx)
            Next

            Return trendList
        End Function

        Private Async Function LoadTopVendorsAsync(connStr As String) As Task(Of List(Of TopVendorItem))
            Dim list As New List(Of TopVendorItem)()
            Using conn As New MySqlConnection(connStr)
                Await conn.OpenAsync()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT v.Id, v.Name, COUNT(po.Id) as POCount, SUM(po.TotalAmount) as TotalSpend " &
                                      "FROM Pur_Vendors v " &
                                      "JOIN Pur_PurchaseOrders po ON v.Id = po.VendorId " &
                                      "WHERE v.IsDeleted = 0 AND po.IsDeleted = 0 AND po.Status IN (2,3,4,5) " &
                                      "GROUP BY v.Id, v.Name " &
                                      "ORDER BY TotalSpend DESC " &
                                      "LIMIT 5"
                    Using reader = Await cmd.ExecuteReaderAsync()
                        While Await reader.ReadAsync()
                            list.Add(New TopVendorItem With {
                                .VendorId = reader.GetInt32(0),
                                .VendorName = reader.GetString(1),
                                .POCount = reader.GetInt32(2),
                                .TotalSpend = reader.GetDecimal(3)
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Private Function BuildWhatThisMeansText() As String
            Dim insights As New List(Of String)()

            If OverdueAPAmount > 0 Then
                insights.Add($"CRITICAL: You have ₱{OverdueAPAmount:N0} across {OverdueAPCount} unpaid bills that are OVERDUE. Please prioritize settling these accounts immediately to avoid vendor friction.")
            ElseIf TotalOutstandingAP > 0 Then
                insights.Add($"You have ₱{TotalOutstandingAP:N0} in accounts payable outstanding. Fortunately, no payments are overdue at this time.")
            Else
                insights.Add("All accounts payable obligations are fully settled. Excellent job!")
            End If

            If PendingDeliveriesCount > 0 Then
                insights.Add($"There are currently {PendingDeliveriesCount} purchase order(s) submitted to vendors and awaiting delivery.")
            Else
                insights.Add("There are no active purchase orders currently pending delivery.")
            End If

            If ReorderSuggestionsCount > 0 Then
                insights.Add($"The reorder engine has detected {ReorderSuggestionsCount} item(s) at or below their reorder points. Review Reorder Suggestions to generate replenishment orders.")
            End If

            Return String.Join(" " & vbCrLf, insights)
        End Function

    End Class

End Namespace
