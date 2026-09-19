Imports System.Windows.Forms

Public Class MainWindow
    Inherits Form

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()
        '
        'MainWindow
        '
        Me.ClientSize = New System.Drawing.Size(800, 450)
        Me.Name = "MainWindow"
        Me.Text = "MerchSys"
        Me.ResumeLayout(False)
    End Sub
End Class
