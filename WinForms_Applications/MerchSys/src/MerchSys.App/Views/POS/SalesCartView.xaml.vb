Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Documents
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Linq
Imports MerchSys.POS.Entities
Imports MerchSys.POS.ViewModels

Namespace Views.POS

    ''' <summary>
    ''' Primary POS transaction screen.
    ''' DataContext is set via constructor injection — resolved from DI by the host.
    ''' </summary>
    Partial Class SalesCartView
        Inherits UserControl

        Public Sub New(viewModel As SalesCartViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

        ' ── Event Handlers & Focus Management ─────────────────────────────────────

        Private Sub OnTransactionCompleted(sender As Object, e As EventArgs)
            Dispatcher.BeginInvoke(Sub()
                                       ProductSearchBox.Focus()
                                   End Sub, System.Windows.Threading.DispatcherPriority.Input)
        End Sub

        Private Sub CartLines_CollectionChanged(sender As Object, e As Specialized.NotifyCollectionChangedEventArgs)
            Dispatcher.BeginInvoke(Sub()
                                       ProductSearchBox.Focus()
                                   End Sub, System.Windows.Threading.DispatcherPriority.Input)
        End Sub

        Private Sub SalesCartView_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            Dim vm = TryCast(DataContext, SalesCartViewModel)
            If vm IsNot Nothing Then
                ' Ensure clean registration to avoid duplicates
                RemoveHandler vm.TransactionCompleted, AddressOf OnTransactionCompleted
                RemoveHandler vm.CartLines.CollectionChanged, AddressOf CartLines_CollectionChanged
                AddHandler vm.TransactionCompleted, AddressOf OnTransactionCompleted
                AddHandler vm.CartLines.CollectionChanged, AddressOf CartLines_CollectionChanged
            End If
            Dispatcher.BeginInvoke(Sub()
                                       ProductSearchBox.Focus()
                                   End Sub, System.Windows.Threading.DispatcherPriority.Input)
        End Sub

        Private Sub SalesCartView_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
            Dim vm = TryCast(DataContext, SalesCartViewModel)
            If vm IsNot Nothing Then
                RemoveHandler vm.TransactionCompleted, AddressOf OnTransactionCompleted
                RemoveHandler vm.CartLines.CollectionChanged, AddressOf CartLines_CollectionChanged
            End If
        End Sub

        ' ── Keyboard Shortcuts (Ctrl+Q, Ctrl+D, Ctrl+T) ─────────────────────────

        Private Sub SalesCartView_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles Me.PreviewKeyDown
            ' Cart-scoped hotkeys that do not bubble to the window
            If (Keyboard.Modifiers And ModifierKeys.Control) = ModifierKeys.Control Then
                If e.Key = Key.Q Then
                    FocusCartGridCell("Qty")
                    e.Handled = True
                ElseIf e.Key = Key.D Then
                    FocusCartGridCell("Discount")
                    e.Handled = True
                ElseIf e.Key = Key.T Then
                    AmountTenderedTextBox.Focus()
                    AmountTenderedTextBox.SelectAll()
                    e.Handled = True
                End If
            End If
        End Sub

        Private Sub FocusCartGridCell(columnName As String)
            If CartDataGrid.SelectedItem Is Nothing AndAlso CartDataGrid.Items.Count > 0 Then
                CartDataGrid.SelectedIndex = 0
            End If
            If CartDataGrid.SelectedItem IsNot Nothing Then
                Dim selectedItem = CartDataGrid.SelectedItem
                CartDataGrid.Focus()
                Dim column = CartDataGrid.Columns.FirstOrDefault(Function(c) c.Header.ToString() = columnName)
                If column IsNot Nothing Then
                    CartDataGrid.CurrentCell = New DataGridCellInfo(selectedItem, column)
                    CartDataGrid.BeginEdit()
                End If
            End If
        End Sub

        ' Toolbar click handlers
        Private Sub SetQtyButton_Click(sender As Object, e As RoutedEventArgs)
            FocusCartGridCell("Qty")
        End Sub

        Private Sub ApplyDiscountButton_Click(sender As Object, e As RoutedEventArgs)
            FocusCartGridCell("Discount")
        End Sub

        ' ── DataGrid row-edit events ──────────────────────────────────────────────

        ''' <summary>
        ''' Routes committed cell edits to the correct service call:
        '''   Qty     → UpdateQuantityCommand  → CartService.UpdateLineQuantityAsync
        '''   Discount → ApplyDiscountCommand   → CartService.ApplyLineDiscountAsync
        ''' </summary>
        Private Sub CartDataGrid_CellEditEnding(sender As Object, e As DataGridCellEditEndingEventArgs) _
            Handles CartDataGrid.CellEditEnding

            Dim vm = TryCast(DataContext, SalesCartViewModel)
            If vm Is Nothing Then Return

            Dim line = TryCast(e.Row.Item, CartLineItem)
            If line Is Nothing Then Return

            If e.EditAction = DataGridEditAction.Commit Then
                Dim columnHeader = TryCast(e.Column.Header, String)

                ' Dispatch after the DataGrid finishes its commit so bindings
                ' have pushed the new value to the CartLineItem source property.
                If String.Equals(columnHeader, "Discount", StringComparison.OrdinalIgnoreCase) Then
                    Dispatcher.InvokeAsync(Async Function()
                                               Await vm.ApplyDiscountCommand.ExecuteAsync(line)
                                               ProductSearchBox.Focus()
                                           End Function)
                Else
                    Dispatcher.InvokeAsync(Async Function()
                                               Await vm.UpdateQuantityCommand.ExecuteAsync(line)
                                               ProductSearchBox.Focus()
                                           End Function)
                End If
            Else
                ' If canceled, just return focus to the scan field
                Dispatcher.InvokeAsync(Sub()
                                           ProductSearchBox.Focus()
                                       End Sub)
            End If
        End Sub

        ' ── Product list double-click → add to cart ───────────────────────────────

        ''' <summary>
        ''' Double-clicking a product in the search results list adds it to the cart.
        ''' </summary>
        Private Sub ProductList_MouseDoubleClick(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) _
            Handles ProductList.MouseDoubleClick

            Dim vm = TryCast(DataContext, SalesCartViewModel)
            If vm Is Nothing Then Return

            Dim product = TryCast(ProductList.SelectedItem, ProductSearchItem)
            If product Is Nothing Then Return

            Dim addTask = vm.AddToCartCommand.ExecuteAsync(product)
        End Sub

        ' ── ProductList KeyDown (Enter to add) ───────────────────────────────────

        Private Sub ProductList_KeyDown(sender As Object, e As KeyEventArgs) Handles ProductList.KeyDown
            If e.Key = Key.Enter Then
                Dim vm = TryCast(DataContext, SalesCartViewModel)
                If vm Is Nothing Then Return

                Dim product = TryCast(ProductList.SelectedItem, ProductSearchItem)
                If product Is Nothing Then Return

                Dim addTask = vm.AddToCartCommand.ExecuteAsync(product)
                e.Handled = True
            End If
        End Sub

        ' ── Product search/add on Enter key ───────────────────────────────────────

        Private Sub ProductSearchBox_KeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs) _
            Handles ProductSearchBox.KeyDown

            If e.Key = System.Windows.Input.Key.Enter Then
                Dim vm = TryCast(DataContext, SalesCartViewModel)
                If vm IsNot Nothing Then
                    Dispatcher.InvokeAsync(Async Function()
                                               Await vm.SearchAndAddProductCommand.ExecuteAsync(Nothing)
                                               ProductSearchBox.Focus()
                                           End Function)
                    e.Handled = True
                End If
            End If
        End Sub

        ' ── Credit Customer Search KeyDown (Enter to search) ──────────────────────

        Private Sub CreditCustomerSearch_KeyDown(sender As Object, e As KeyEventArgs) Handles CreditCustomerSearch.KeyDown
            If e.Key = Key.Enter Then
                Dim vm = TryCast(DataContext, SalesCartViewModel)
                vm?.SearchCreditCustomerCommand.Execute(Nothing)
                e.Handled = True
            End If
        End Sub

        ' ── Print Official Receipt ────────────────────────────────────────────────

        Private Sub PrintOrButton_Click(sender As Object, e As RoutedEventArgs)
            Dim vm = TryCast(DataContext, SalesCartViewModel)
            If vm Is Nothing OrElse vm.CurrentReceipt Is Nothing Then Return

            Dim receipt = vm.CurrentReceipt
            Dim lineItems = vm.LastCartLines

            Dim flowDoc As New FlowDocument()
            flowDoc.PagePadding = New Thickness(60, 40, 60, 40)
            flowDoc.FontFamily = New FontFamily("Courier New")
            flowDoc.FontSize = 11
            ' A5 portrait approximation — 559 WPF units ≈ 148mm; thermal receipt is narrower
            flowDoc.PageWidth = 480

            BuildOrFlowDocument(flowDoc, receipt, lineItems)

            Dim dlg As New PrintDialog()
            If dlg.ShowDialog() = True Then
                dlg.PrintDocument(
                    CType(flowDoc, IDocumentPaginatorSource).DocumentPaginator,
                    $"BIR Official Receipt {receipt.ReceiptNumber}")
            End If
        End Sub

        ''' <summary>
        ''' Populates <paramref name="flowDoc"/> with a BIR-compliant OR layout:
        ''' header block, line items, totals, VAT disclosure, and footer.
        ''' Numbers are formatted with N2; peso symbol is included inline.
        ''' </summary>
        Private Shared Sub BuildOrFlowDocument(flowDoc As FlowDocument,
                                               receipt As OfficialReceipt,
                                               lineItems As IReadOnlyList(Of CartLineItem))
            Const Sep As String = "────────────────────────────────────────────────"

            ' ── Business header ────────────────────────────────────────────────
            Dim hdr As New Paragraph() With {.TextAlignment = TextAlignment.Center, .Margin = New Thickness(0, 0, 0, 2)}
            hdr.Inlines.Add(New Bold(New Run(receipt.BusinessName)))
            flowDoc.Blocks.Add(hdr)

            If Not String.IsNullOrWhiteSpace(receipt.BusinessAddress) Then
                For Each addrLine In receipt.BusinessAddress.Split(
                        New String() {Environment.NewLine, vbCrLf, vbLf},
                        StringSplitOptions.RemoveEmptyEntries)
                    Dim ap As New Paragraph(New Run(addrLine.Trim())) With {
                        .TextAlignment = TextAlignment.Center,
                        .FontSize = 10,
                        .Margin = New Thickness(0)
                    }
                    flowDoc.Blocks.Add(ap)
                Next
            End If

            Dim vatStatusLine = If(receipt.IsVatRegistered, "VAT-Registered Taxpayer", "Non-VAT Taxpayer")
            flowDoc.Blocks.Add(New Paragraph(New Run(vatStatusLine)) With {
                .TextAlignment = TextAlignment.Center, .FontSize = 10, .Margin = New Thickness(0)
            })

            If Not String.IsNullOrWhiteSpace(receipt.BusinessTIN) Then
                flowDoc.Blocks.Add(New Paragraph(New Run($"TIN: {receipt.BusinessTIN}")) With {
                    .TextAlignment = TextAlignment.Center, .FontSize = 10, .Margin = New Thickness(0, 0, 0, 2)
                })
            End If

            flowDoc.Blocks.Add(New Paragraph(New Run(Sep)) With {.Margin = New Thickness(0, 4, 0, 4)})

            ' ── OR number and issue date ────────────────────────────────────────
            Dim orPara As New Paragraph() With {.Margin = New Thickness(0, 0, 0, 2)}
            orPara.Inlines.Add(New Run("OR No.: "))
            orPara.Inlines.Add(New Bold(New Run(receipt.ReceiptNumber)))
            flowDoc.Blocks.Add(orPara)

            flowDoc.Blocks.Add(New Paragraph(
                New Run($"Date:   {receipt.IssueDate.ToLocalTime():MM/dd/yyyy HH:mm}")) With {
                .Margin = New Thickness(0, 0, 0, 4)
            })

            flowDoc.Blocks.Add(New Paragraph(New Run(Sep)) With {.Margin = New Thickness(0, 0, 0, 4)})

            ' ── Line items ─────────────────────────────────────────────────────
            flowDoc.Blocks.Add(New Paragraph(New Run(
                $"{"Item",-20}{"Qty",4}{"Unit Price",11}{"Total",10}")) With {
                .Margin = New Thickness(0)
            })
            flowDoc.Blocks.Add(New Paragraph(New Run(Sep)) With {.Margin = New Thickness(0, 0, 0, 2)})

            Dim subtotal As Decimal = 0D
            If lineItems IsNot Nothing AndAlso lineItems.Count > 0 Then
                For Each li In lineItems
                    Dim nm = If(li.ProductName.Length > 20, li.ProductName.Substring(0, 20), li.ProductName)
                    flowDoc.Blocks.Add(New Paragraph(New Run(
                        $"{nm,-20}{li.Quantity,4}{li.UnitPrice,11:N2}{li.LineTotal,10:N2}")) With {
                        .Margin = New Thickness(0)
                    })
                    If li.DiscountAmount > 0D Then
                        flowDoc.Blocks.Add(New Paragraph(New Run(
                            $"  Discount: -₱{li.DiscountAmount:N2}")) With {
                            .Margin = New Thickness(0)
                        })
                    End If
                    subtotal += li.LineTotal
                Next
            Else
                ' No line snapshot (e.g. reprint) — fall back to the receipt total.
                subtotal = receipt.TotalAmount
            End If

            flowDoc.Blocks.Add(New Paragraph(New Run(Sep)) With {.Margin = New Thickness(0, 4, 0, 4)})

            ' ── Totals ─────────────────────────────────────────────────────────
            flowDoc.Blocks.Add(New Paragraph(New Run(
                $"{"Subtotal:",-38}₱{subtotal,10:N2}")) With {.Margin = New Thickness(0)})
            flowDoc.Blocks.Add(New Paragraph(New Bold(New Run(
                $"{"TOTAL:",-38}₱{receipt.TotalAmount,10:N2}"))) With {.Margin = New Thickness(0, 2, 0, 4)})

            flowDoc.Blocks.Add(New Paragraph(New Run(Sep)) With {.Margin = New Thickness(0, 0, 0, 4)})

            ' ── BIR VAT disclosure block ────────────────────────────────────────
            If receipt.IsVatRegistered Then
                Dim vatableSalesNet = receipt.TotalAmount - receipt.VatAmount
                flowDoc.Blocks.Add(New Paragraph(New Run(
                    $"{"VATable Sales:",-38}₱{vatableSalesNet,10:N2}")) With {.Margin = New Thickness(0)})
                flowDoc.Blocks.Add(New Paragraph(New Run(
                    $"{"VAT-Exempt Sales:",-38}₱{0D,10:N2}")) With {.Margin = New Thickness(0)})
                flowDoc.Blocks.Add(New Paragraph(New Run(
                    $"{"Zero-Rated Sales:",-38}₱{0D,10:N2}")) With {.Margin = New Thickness(0, 0, 0, 2)})
                flowDoc.Blocks.Add(New Paragraph(New Run(
                    $"{"Output VAT (12%):",-38}₱{receipt.VatAmount,10:N2}")) With {.Margin = New Thickness(0, 0, 0, 4)})
            Else
                flowDoc.Blocks.Add(New Paragraph(New Run(
                    $"{"Gross Sales:",-38}₱{receipt.TotalAmount,10:N2}")) With {.Margin = New Thickness(0, 0, 0, 4)})
            End If

            flowDoc.Blocks.Add(New Paragraph(New Run(Sep)) With {.Margin = New Thickness(0, 0, 0, 8)})

            ' ── Footer ─────────────────────────────────────────────────────────
            flowDoc.Blocks.Add(New Paragraph(New Run("Thank you for your business.")) With {
                .TextAlignment = TextAlignment.Center, .FontStyle = FontStyles.Italic, .Margin = New Thickness(0, 0, 0, 2)
            })
            flowDoc.Blocks.Add(New Paragraph(New Run("This serves as your Official Receipt.")) With {
                .TextAlignment = TextAlignment.Center, .FontStyle = FontStyles.Italic, .Margin = New Thickness(0)
            })
        End Sub

    End Class

End Namespace
