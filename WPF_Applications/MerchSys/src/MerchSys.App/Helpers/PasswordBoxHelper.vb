Imports System.Windows
Imports System.Windows.Controls

Namespace Helpers

    ''' <summary>
    ''' Attached-property bridge that binds <see cref="PasswordBox.Password"/> to a ViewModel string.
    ''' WPF intentionally blocks direct binding on PasswordBox to keep the password in a SecureString.
    ''' The bridge converts to String at the point of PasswordChanged; the ViewModel then passes it
    ''' to Argon2id for hashing. SecureString cannot be preserved end-to-end because Argon2id
    ''' requires a byte array — this is the standard WPF trade-off.
    ''' Usage in XAML:
    '''   helpers:PasswordBoxHelper.IsMonitoring="True"
    '''   helpers:PasswordBoxHelper.BoundPassword="{Binding Password, Mode=TwoWay}"
    ''' </summary>
    Public Class PasswordBoxHelper

        ' ── IsMonitoring — enables the PasswordChanged event subscription ───────

        Public Shared ReadOnly IsMonitoringProperty As DependencyProperty =
            DependencyProperty.RegisterAttached(
                "IsMonitoring",
                GetType(Boolean),
                GetType(PasswordBoxHelper),
                New PropertyMetadata(False, AddressOf OnIsMonitoringChanged))

        Public Shared Function GetIsMonitoring(dp As DependencyObject) As Boolean
            Return CBool(dp.GetValue(IsMonitoringProperty))
        End Function

        Public Shared Sub SetIsMonitoring(dp As DependencyObject, value As Boolean)
            dp.SetValue(IsMonitoringProperty, value)
        End Sub

        ' ── BoundPassword — the bindable string property ─────────────────────

        Public Shared ReadOnly BoundPasswordProperty As DependencyProperty =
            DependencyProperty.RegisterAttached(
                "BoundPassword",
                GetType(String),
                GetType(PasswordBoxHelper),
                New PropertyMetadata(String.Empty, AddressOf OnBoundPasswordChanged))

        Public Shared Function GetBoundPassword(dp As DependencyObject) As String
            Return CStr(dp.GetValue(BoundPasswordProperty))
        End Function

        Public Shared Sub SetBoundPassword(dp As DependencyObject, value As String)
            dp.SetValue(BoundPasswordProperty, value)
        End Sub

        ' ── Sync logic ────────────────────────────────────────────────────────

        ' Guard against recursive updates when the property and the box sync each other.
        <ThreadStatic>
        Private Shared _updating As Boolean

        Private Shared Sub OnIsMonitoringChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim box = TryCast(d, PasswordBox)
            If box Is Nothing Then Return
            If CBool(e.NewValue) Then
                AddHandler box.PasswordChanged, AddressOf HandlePasswordChanged
            Else
                RemoveHandler box.PasswordChanged, AddressOf HandlePasswordChanged
            End If
        End Sub

        Private Shared Sub HandlePasswordChanged(sender As Object, e As RoutedEventArgs)
            Dim box = DirectCast(sender, PasswordBox)
            _updating = True
            SetBoundPassword(box, box.Password)
            _updating = False
        End Sub

        Private Shared Sub OnBoundPasswordChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            If _updating Then Return
            Dim box = TryCast(d, PasswordBox)
            If box Is Nothing Then Return
            box.Password = If(e.NewValue IsNot Nothing, CStr(e.NewValue), String.Empty)
        End Sub

    End Class

End Namespace
