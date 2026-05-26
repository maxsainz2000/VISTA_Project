Imports MerchSys.App.ViewModels

Namespace Views

    Partial Class SessionTimeoutWarningView
        Inherits Window

        Private ReadOnly _viewModel As SessionTimeoutWarningViewModel
        Private _decisionMade As Boolean

        Public ReadOnly Property ViewModel As SessionTimeoutWarningViewModel
            Get
                Return _viewModel
            End Get
        End Property

        Public Sub New(vm As SessionTimeoutWarningViewModel)
            InitializeComponent()
            _viewModel = vm
            DataContext = vm
        End Sub

        ''' <summary>
        ''' Called by Application.xaml.vb after the user makes an explicit choice (Stay or Sign out).
        ''' Prevents OnClosing from treating a programmatic close as a sign-out.
        ''' </summary>
        Public Sub MarkDecisionMade()
            _decisionMade = True
        End Sub

        Protected Overrides Sub OnClosing(e As System.ComponentModel.CancelEventArgs)
            MyBase.OnClosing(e)
            If _decisionMade Then Return
            ' User closed via X without an explicit decision — treat as Sign Out per DA2 spec.
            ' Set flag first to prevent re-entrance if CloseWarningDialogIfOpen calls Close() again.
            _decisionMade = True
            _viewModel.SignOutCommand.Execute(Nothing)
        End Sub

    End Class

End Namespace
