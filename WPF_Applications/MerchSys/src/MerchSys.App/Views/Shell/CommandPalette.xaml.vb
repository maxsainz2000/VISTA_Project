Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports MerchSys.App.ViewModels
Imports MerchSys.App.ViewModels.Shell

Namespace Views.Shell

    Public Class CommandPalette
        Inherits UserControl

        Private _previouslyFocusedElement As IInputElement

        Public Sub New()
            InitializeComponent()
        End Sub

        Public Sub ScrollSelectedIndexIntoView()
            Dim index = ResultsListBox.SelectedIndex
            If index >= 0 AndAlso index < ResultsListBox.Items.Count Then
                Dim selectedItem = ResultsListBox.Items(index)
                ResultsListBox.ScrollIntoView(selectedItem)
            End If
        End Sub

        Private Sub CommandPalette_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
            If CType(e.NewValue, Boolean) Then
                ' Save the currently focused element
                _previouslyFocusedElement = Keyboard.FocusedElement
                
                ' Set focus to the search text box after UI completes rendering
                Dim unused1 = Dispatcher.InvokeAsync(Sub()
                                           SearchTextBox.Focus()
                                           Keyboard.Focus(SearchTextBox)
                                       End Sub, System.Windows.Threading.DispatcherPriority.Input)
            Else
                ' Restore focus to the previously active control
                If _previouslyFocusedElement IsNot Nothing Then
                    Dim unused2 = Dispatcher.InvokeAsync(Sub()
                                               _previouslyFocusedElement.Focus()
                                               Keyboard.Focus(_previouslyFocusedElement)
                                           End Sub, System.Windows.Threading.DispatcherPriority.Input)
                End If
            End If
        End Sub

        Public Sub Background_MouseDown(sender As Object, e As MouseButtonEventArgs)
            Dim vm = TryCast(DataContext, CommandPaletteViewModel)
            If vm IsNot Nothing Then
                vm.CloseCommand.Execute(Nothing)
            End If
        End Sub

        Public Sub SearchPanel_MouseDown(sender As Object, e As MouseButtonEventArgs)
            ' Suppress bubbling to prevent click-away closing
            e.Handled = True
        End Sub

    End Class

End Namespace
