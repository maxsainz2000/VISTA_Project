Imports System.Windows.Controls
Imports MerchSys.Purchasing.ViewModels

Namespace Views.Purchasing

    ''' <summary>
    ''' Reorder Suggestions screen — two-tab layout for reviewing suggestions
    ''' (generate / accept / dismiss) and editing per-product reorder configuration.
    ''' DataContext is resolved from DI via constructor injection.
    ''' </summary>
    Partial Class ReorderSuggestionsView
        Inherits UserControl

        Public Sub New(viewModel As ReorderSuggestionsViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

    End Class

End Namespace
