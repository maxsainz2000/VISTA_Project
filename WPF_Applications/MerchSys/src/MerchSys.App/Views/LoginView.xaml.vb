Imports MerchSys.App.ViewModels

Namespace Views

    Partial Class LoginView
        Inherits Window

        Private ReadOnly _viewModel As LoginViewModel

        Public ReadOnly Property ViewModel As LoginViewModel
            Get
                Return _viewModel
            End Get
        End Property

        Public Sub New(viewModel As LoginViewModel)
            InitializeComponent()
            _viewModel = viewModel
            DataContext = viewModel
        End Sub

    End Class

End Namespace
