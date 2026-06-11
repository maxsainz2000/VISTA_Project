Imports System.IO
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Documents
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports Microsoft.Win32
Imports MerchSys.Accounting.Services.Reporting

Namespace Views.Shell

    ''' <summary>
    ''' Shared print-preview window for A4 PDF reports (Income Statement, VAT Relief Report).
    ''' Shows rendered PDF page images; supports printing via PrintDialog and saving as PDF.
    ''' </summary>
    Partial Class ReportPreviewWindow
        Inherits Window

        Private ReadOnly _pdfBytes As Byte()
        Private ReadOnly _pageImages As IReadOnlyList(Of Byte())
        Private ReadOnly _suggestedFileName As String

        Public Sub New(reportTitle As String, result As ReportPdfResult)
            InitializeComponent()
            Me.Title = $"Preview — {reportTitle}"
            ReportTitleBlock.Text = reportTitle
            _pdfBytes = result.PdfBytes
            _pageImages = result.PageImages
            _suggestedFileName = result.SuggestedFileName

            ' Convert page image byte arrays to BitmapImage sources for binding
            Dim bitmaps As New List(Of BitmapImage)()
            For Each imgBytes In _pageImages
                Dim bmp As New BitmapImage()
                Using ms As New MemoryStream(imgBytes)
                    bmp.BeginInit()
                    bmp.CacheOption = BitmapCacheOption.OnLoad
                    bmp.StreamSource = ms
                    bmp.EndInit()
                    bmp.Freeze()
                End Using
                bitmaps.Add(bmp)
            Next
            PageItemsControl.ItemsSource = bitmaps

            AddHandler Me.Loaded, Sub() PreviewScrollViewer.Focus()
        End Sub

        Private Sub PrintButton_Click(sender As Object, e As RoutedEventArgs)
            Dim dlg As New PrintDialog()
            If dlg.ShowDialog() <> True Then Return

            ' Print each page image as a scaled visual
            For i = 0 To _pageImages.Count - 1
                Dim bmp As New BitmapImage()
                Using ms As New MemoryStream(_pageImages(i))
                    bmp.BeginInit()
                    bmp.CacheOption = BitmapCacheOption.OnLoad
                    bmp.StreamSource = ms
                    bmp.EndInit()
                    bmp.Freeze()
                End Using

                Dim img As New Image()
                img.Source = bmp
                img.Stretch = Stretch.Uniform

                ' Scale to printable area
                Dim printableWidth = dlg.PrintableAreaWidth
                Dim printableHeight = dlg.PrintableAreaHeight
                img.Width = printableWidth
                img.Height = printableHeight
                img.Measure(New Size(printableWidth, printableHeight))
                img.Arrange(New Rect(0, 0, printableWidth, printableHeight))

                dlg.PrintVisual(img, $"{Me.Title} — Page {i + 1}")
            Next
        End Sub

        Private Sub SaveButton_Click(sender As Object, e As RoutedEventArgs)
            Dim dlg As New SaveFileDialog() With {
                .Filter = "PDF files (*.pdf)|*.pdf",
                .FileName = _suggestedFileName,
                .OverwritePrompt = True
            }
            If dlg.ShowDialog() = True Then
                Dim saveErr As String = String.Empty
                Try
                    File.WriteAllBytes(dlg.FileName, _pdfBytes)
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
