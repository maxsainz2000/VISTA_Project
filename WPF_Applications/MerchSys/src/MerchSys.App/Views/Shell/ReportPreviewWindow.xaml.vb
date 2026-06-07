Imports System.IO
Imports System.Windows
Imports System.Windows.Documents
Imports System.Windows.Media
Imports Microsoft.Win32

Namespace Views.Shell

    ''' <summary>
    ''' Shared print-preview window for plain-text reports (Income Statement, VAT Relief Report).
    ''' Shows formatted text content; supports printing via PrintDialog and saving as a text file.
    ''' </summary>
    Partial Class ReportPreviewWindow
        Inherits Window

        Private ReadOnly _suggestedFileName As String

        Public Sub New(reportTitle As String, content As String, suggestedFileName As String)
            InitializeComponent()
            Me.Title = $"Preview — {reportTitle}"
            ReportTitleBlock.Text = reportTitle
            PreviewTextBox.Text = content
            _suggestedFileName = suggestedFileName
            AddHandler Me.Loaded, Sub() PreviewTextBox.Focus()
        End Sub

        Private Sub PrintButton_Click(sender As Object, e As RoutedEventArgs)
            Dim flowDoc As New FlowDocument()
            flowDoc.PagePadding = New Thickness(50)
            flowDoc.FontFamily = New FontFamily("Courier New")
            flowDoc.FontSize = 10

            Dim para As New Paragraph(New Run(PreviewTextBox.Text))
            para.LineHeight = 15
            flowDoc.Blocks.Add(para)

            Dim dlg As New PrintDialog()
            If dlg.ShowDialog() = True Then
                dlg.PrintDocument(
                    CType(flowDoc, IDocumentPaginatorSource).DocumentPaginator,
                    Me.Title)
            End If
        End Sub

        Private Sub SaveButton_Click(sender As Object, e As RoutedEventArgs)
            Dim dlg As New SaveFileDialog() With {
                .Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                .FileName = _suggestedFileName,
                .OverwritePrompt = True
            }
            If dlg.ShowDialog() = True Then
                Dim saveErr As String = String.Empty
                Try
                    File.WriteAllText(dlg.FileName, PreviewTextBox.Text, System.Text.Encoding.UTF8)
                Catch ex As Exception
                    saveErr = ex.Message
                End Try
                If Not String.IsNullOrEmpty(saveErr) Then
                    MessageBox.Show($"Save failed: {saveErr}", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error)
                End If
            End If
        End Sub

        Private Sub CloseButton_Click(sender As Object, e As RoutedEventArgs)
            Close()
        End Sub

    End Class

End Namespace
