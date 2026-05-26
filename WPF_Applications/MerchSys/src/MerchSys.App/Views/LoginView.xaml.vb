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

        Public Sub New(vm As LoginViewModel)
            InitializeComponent()
            _viewModel = vm
            DataContext = vm
        End Sub

    End Class

End Namespace
