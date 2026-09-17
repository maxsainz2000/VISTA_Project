Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Inventory.Data

Namespace ViewModels

    Public Class PriceHistoryRowItem
        Public Property ChangedAt As DateTime
        Public Property OldPrice As Decimal
        Public Property NewPrice As Decimal
        Public Property Delta As Decimal
        Public Property ChangedBy As String
        Public Property Reason As String

        Public ReadOnly Property IsIncrease As Boolean
            Get
                Return Delta > 0
            End Get
        End Property

        Public ReadOnly Property IsDecrease As Boolean
            Get
                Return Delta < 0
            End Get
        End Property
    End Class

    Public Class ProductPriceHistoryViewModel
        Inherits ObservableObject

        Private ReadOnly _db As InventoryDbContext

        Public Sub New(db As InventoryDbContext)
            _db = db
            HistoryItems = New ObservableCollection(Of PriceHistoryRowItem)()
        End Sub

        Private _productId As Integer
        Public Property ProductId As Integer
            Get
                Return _productId
            End Get
            Set(value As Integer)
                SetProperty(_productId, value)
            End Set
        End Property

        Private _productName As String = String.Empty
        Public Property ProductName As String
            Get
                Return _productName
            End Get
            Set(value As String)
                SetProperty(_productName, value)
            End Set
        End Property

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
                Return HistoryItems.Count = 0 AndAlso Not IsBusy AndAlso Not IsError
            End Get
        End Property

        Public Property HistoryItems As ObservableCollection(Of PriceHistoryRowItem)

        Public Async Function LoadHistoryAsync() As Task
            IsError = False
            IsBusy = True
            Try
                ' Project to an explicit DTO inside the query to bypass the VB.NET silent ToListAsync() bug.
                Dim list = Await _db.ProductPriceHistory.
                    Where(Function(p) p.ProductId = ProductId).
                    OrderByDescending(Function(p) p.ChangedAt).
                    Select(Function(p) New PriceHistoryRowItem With {
                        .ChangedAt = p.ChangedAt,
                        .OldPrice = p.OldPrice,
                        .NewPrice = p.NewPrice,
                        .Delta = p.NewPrice - p.OldPrice,
                        .ChangedBy = p.ChangedBy,
                        .Reason = If(p.Reason, String.Empty)
                    }).
                    ToListAsync()

                HistoryItems.Clear()
                For Each item In list
                    HistoryItems.Add(item)
                Next
            Catch ex As Exception
                ErrorMessage = ex.Message
                IsError = True
            Finally
                IsBusy = False
            End Try
        End Function

    End Class

End Namespace
