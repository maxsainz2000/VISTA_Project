Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Services

Namespace ViewModels

    ''' <summary>
    ''' Represents one editable line in the PO editor.
    ''' LineTotal auto-recalculates when Qty or UnitCost changes.
    ''' </summary>
    Public Class POLineItem
        Inherits ObservableObject

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

        Private _quantityOrdered As Integer = 1
        Public Property QuantityOrdered As Integer
            Get
                Return _quantityOrdered
            End Get
            Set(value As Integer)
                If SetProperty(_quantityOrdered, value) Then
                    OnPropertyChanged(NameOf(LineTotal))
                End If
            End Set
        End Property

        Private _unitCost As Decimal
        Public Property UnitCost As Decimal
            Get
                Return _unitCost
            End Get
            Set(value As Decimal)
                If SetProperty(_unitCost, value) Then
                    OnPropertyChanged(NameOf(LineTotal))
                End If
            End Set
        End Property

        Public ReadOnly Property LineTotal As Decimal
            Get
                Return QuantityOrdered * UnitCost
            End Get
        End Property

        Private _selectedCatalogEntry As Dtos.VendorProductDto
        Public Property SelectedCatalogEntry As Dtos.VendorProductDto
            Get
                Return _selectedCatalogEntry
            End Get
            Set(value As Dtos.VendorProductDto)
                If SetProperty(_selectedCatalogEntry, value) AndAlso value IsNot Nothing Then
                    ProductId = value.ProductId
                    ProductName = value.ProductName
                    UnitCost = value.LastUnitCost
                End If
            End Set
        End Property

    End Class

    ''' <summary>
    ''' ViewModel for the PO editor panel (new and edit modes).
    ''' Manages vendor selection, editable line items, and running total.
    ''' Notes and ExpectedDeliveryDate are UI fields; persistence requires a future
    ''' service extension (IPurchaseOrderService.CreateDraftAsync does not accept them).
    ''' </summary>
    Public Class PurchaseOrderEditorViewModel
        Inherits ObservableObject

        Public Sub New()
            LineItems = New ObservableCollection(Of POLineItem)()
            Vendors = New ObservableCollection(Of Vendor)()
            VendorCatalog = New ObservableCollection(Of Dtos.VendorProductDto)()
            AddLineCommand = New RelayCommand(AddressOf AddEmptyLine)
            RemoveLineCommand = New RelayCommand(Of POLineItem)(AddressOf RemoveLineItem)
        End Sub

        ' ─── Vendors ─────────────────────────────────────────────────────────────

        Public Property Vendors As ObservableCollection(Of Vendor)
        Public Property VendorCatalog As ObservableCollection(Of Dtos.VendorProductDto)

        Private _selectedVendor As Vendor
        Public Property SelectedVendor As Vendor
            Get
                Return _selectedVendor
            End Get
            Set(value As Vendor)
                SetProperty(_selectedVendor, value)
            End Set
        End Property

        ' ─── Line Items ──────────────────────────────────────────────────────────

        Public Property LineItems As ObservableCollection(Of POLineItem)

        Public ReadOnly Property TotalAmount As Decimal
            Get
                Dim total As Decimal = 0D
                For Each line In LineItems
                    total += line.LineTotal
                Next
                Return total
            End Get
        End Property

        ' ─── Header Fields ────────────────────────────────────────────────────────

        Private _expectedDeliveryDate As DateTime?
        Public Property ExpectedDeliveryDate As DateTime?
            Get
                Return _expectedDeliveryDate
            End Get
            Set(value As DateTime?)
                SetProperty(_expectedDeliveryDate, value)
            End Set
        End Property

        Private _notes As String = String.Empty
        Public Property Notes As String
            Get
                Return _notes
            End Get
            Set(value As String)
                SetProperty(_notes, value)
            End Set
        End Property

        ' ─── State ────────────────────────────────────────────────────────────────

        Private _editingPOId As Integer?
        Public Property EditingPOId As Integer?
            Get
                Return _editingPOId
            End Get
            Set(value As Integer?)
                If SetProperty(_editingPOId, value) Then
                    OnPropertyChanged(NameOf(IsNewPO))
                    OnPropertyChanged(NameOf(EditorTitle))
                End If
            End Set
        End Property

        Public ReadOnly Property IsNewPO As Boolean
            Get
                Return Not EditingPOId.HasValue
            End Get
        End Property

        Public ReadOnly Property EditorTitle As String
            Get
                If IsNewPO Then
                    Return "New Purchase Order"
                Else
                    Return $"Edit Purchase Order"
                End If
            End Get
        End Property

        ' ─── Commands ────────────────────────────────────────────────────────────

        Public Property AddLineCommand As RelayCommand
        Public Property RemoveLineCommand As RelayCommand(Of POLineItem)

        ' ─── Public API ──────────────────────────────────────────────────────────

        Public Sub LoadVendors(vendorList As List(Of Vendor))
            Vendors.Clear()
            For Each v In vendorList
                Vendors.Add(v)
            Next
        End Sub

        Public Sub PrepareForNew(vendorList As List(Of Vendor))
            EditingPOId = Nothing
            SelectedVendor = Nothing
            ExpectedDeliveryDate = Nothing
            Notes = String.Empty
            UnwireAllLineHandlers()
            LineItems.Clear()
            LoadVendors(vendorList)
            OnPropertyChanged(NameOf(TotalAmount))
        End Sub

        Public Sub LoadFromPO(po As PurchaseOrder, vendorList As List(Of Vendor))
            EditingPOId = po.Id
            LoadVendors(vendorList)
            SelectedVendor = Vendors.FirstOrDefault(Function(v) v.Id = po.VendorId)
            ExpectedDeliveryDate = po.ExpectedDeliveryDate
            Notes = If(po.Notes, String.Empty)
            UnwireAllLineHandlers()
            LineItems.Clear()
            For Each line In po.Lines
                Dim item = New POLineItem With {
                    .ProductId = line.ProductId,
                    .ProductName = line.ProductName,
                    .QuantityOrdered = line.QuantityOrdered,
                    .UnitCost = line.UnitCost
                }
                WireLineHandler(item)
                LineItems.Add(item)
            Next
            OnPropertyChanged(NameOf(TotalAmount))
        End Sub

        Public Function ToLineDtos() As List(Of CreatePOLineDto)
            Return LineItems.Select(Function(li) New CreatePOLineDto With {
                .ProductId = li.ProductId,
                .ProductName = li.ProductName,
                .QuantityOrdered = li.QuantityOrdered,
                .UnitCost = li.UnitCost
            }).ToList()
        End Function

        ' ─── Private Helpers ─────────────────────────────────────────────────────

        Private Sub AddEmptyLine()
            Dim item = New POLineItem()
            WireLineHandler(item)
            LineItems.Add(item)
            OnPropertyChanged(NameOf(TotalAmount))
        End Sub

        Private Sub RemoveLineItem(item As POLineItem)
            If item Is Nothing Then Return
            UnwireLineHandler(item)
            LineItems.Remove(item)
            OnPropertyChanged(NameOf(TotalAmount))
        End Sub

        Private Sub WireLineHandler(item As POLineItem)
            AddHandler item.PropertyChanged, AddressOf OnLinePropertyChanged
        End Sub

        Private Sub UnwireLineHandler(item As POLineItem)
            RemoveHandler item.PropertyChanged, AddressOf OnLinePropertyChanged
        End Sub

        Private Sub UnwireAllLineHandlers()
            For Each item In LineItems
                UnwireLineHandler(item)
            Next
        End Sub

        Public Async Function LoadVendorCatalogAsync(vendorProductService As IVendorProductService, vendorId As Integer) As Task
            VendorCatalog.Clear()
            Dim catalog = Await vendorProductService.GetCatalogForVendorAsync(vendorId)
            For Each item In catalog
                VendorCatalog.Add(item)
            Next

            ' Wire up SelectedCatalogEntry for existing line items
            For Each item In LineItems
                item.SelectedCatalogEntry = VendorCatalog.FirstOrDefault(Function(c) c.ProductId = item.ProductId)
            Next
        End Function

        Private Sub OnLinePropertyChanged(sender As Object, e As PropertyChangedEventArgs)
            Dim item = DirectCast(sender, POLineItem)
            If e.PropertyName = NameOf(POLineItem.LineTotal) Then
                OnPropertyChanged(NameOf(TotalAmount))
            ElseIf e.PropertyName = NameOf(POLineItem.ProductId) Then
                ' Auto-populate when ProductId changes (e.g. from combobox selection)
                If VendorCatalog IsNot Nothing AndAlso item.ProductId <> 0 Then
                    Dim cat = VendorCatalog.FirstOrDefault(Function(c) c.ProductId = item.ProductId)
                    If cat IsNot Nothing Then
                        item.ProductName = cat.ProductName
                        item.UnitCost = cat.LastUnitCost
                        item.SelectedCatalogEntry = cat
                    End If
                End If
            End If
        End Sub

    End Class

End Namespace
