Imports System.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Accounting.Services

Namespace Services

    ''' <summary>
    ''' Partial-class extension on <see cref="FinancialOverviewDto"/> adding the VAT Payable KPI
    ''' fields populated by <see cref="VatEnrichedFinancialOverviewService"/> (ACC-12).
    ''' Declared here so the ACC-03 source file (<c>IFinancialOverviewService.vb</c>) is not modified.
    ''' </summary>
    Partial Public Class FinancialOverviewDto
        ''' <summary>Net VAT payable (OutputVat − InputVat) or percentage-tax due for the open period.</summary>
        Public Property VatPayable As Decimal

        ''' <summary>"VAT Payable" for VAT-registered businesses; "Percentage Tax" for non-VAT filers.</summary>
        Public Property VatPayableLabel As String = String.Empty

        ''' <summary>BIR eFPS filing deadline for the current open period; Nothing when no return exists.</summary>
        Public Property VatFilingDueDate As DateTime?

        ''' <summary>Visual urgency level driving tile colour and "What This Means" sentence choice.</summary>
        Public Property VatPayableSeverity As KpiSeverity

        ''' <summary>True when the business is VAT-registered (2550M/Q path); False for 2551Q path.</summary>
        Public Property IsVatRegistered As Boolean

        ''' <summary>Human-readable period label, e.g. "May 2026" or "Q2 2026".</summary>
        Public Property VatPeriodDescription As String = String.Empty
    End Class

End Namespace

Namespace ViewModels

    ''' <summary>
    ''' Partial-class extension on <see cref="FinancialOverviewViewModel"/> exposing the VAT Payable
    ''' KPI fields as observable properties for binding by <c>VatPayableTile.xaml</c>.
    ''' Properties are populated via <see cref="ApplyVatDataFromServiceAsync"/> which is triggered
    ''' automatically each time the main data-load cycle completes (detected via <see cref="IsBusy"/>
    ''' transition from True to False through the <see cref="OnPropertyChanged"/> override).
    ''' ACC-07 source file is not modified.
    ''' </summary>
    Partial Public Class FinancialOverviewViewModel

        ' ─── VAT Observable Properties ────────────────────────────────────────────

        Private _vatPayable As Decimal
        Public Property VatPayable As Decimal
            Get
                Return _vatPayable
            End Get
            Set(value As Decimal)
                SetProperty(_vatPayable, value)
            End Set
        End Property

        Private _vatPayableLabel As String = String.Empty
        Public Property VatPayableLabel As String
            Get
                Return _vatPayableLabel
            End Get
            Set(value As String)
                SetProperty(_vatPayableLabel, value)
            End Set
        End Property

        Private _vatFilingDueDate As DateTime?
        Public Property VatFilingDueDate As DateTime?
            Get
                Return _vatFilingDueDate
            End Get
            Set(value As DateTime?)
                SetProperty(_vatFilingDueDate, value)
            End Set
        End Property

        Private _vatPayableSeverity As KpiSeverity
        Public Property VatPayableSeverity As KpiSeverity
            Get
                Return _vatPayableSeverity
            End Get
            Set(value As KpiSeverity)
                SetProperty(_vatPayableSeverity, value)
                OnPropertyChanged(NameOf(IsVatWarning))
                OnPropertyChanged(NameOf(IsVatCritical))
            End Set
        End Property

        Private _isVatRegistered As Boolean
        Public Property IsVatRegistered As Boolean
            Get
                Return _isVatRegistered
            End Get
            Set(value As Boolean)
                SetProperty(_isVatRegistered, value)
            End Set
        End Property

        Public ReadOnly Property IsVatWarning As Boolean
            Get
                Return _vatPayableSeverity = KpiSeverity.Warning
            End Get
        End Property

        Public ReadOnly Property IsVatCritical As Boolean
            Get
                Return _vatPayableSeverity = KpiSeverity.Critical
            End Get
        End Property

        ' ─── Navigation Command ───────────────────────────────────────────────────

        Private _navigateToVatReturnCommand As RelayCommand
        ''' <summary>
        ''' Navigates to VatReturnView for the current period.  Bound to the tile click action.
        ''' The VatReturnView is Manager-only (gated in ACC-11); the Owner can see the tile read-only.
        ''' The command raises a <see cref="NavigateToVatReturnRequested"/> event consumed by the
        ''' hosting window's navigation layer.
        ''' </summary>
        Public ReadOnly Property NavigateToVatReturnCommand As RelayCommand
            Get
                If _navigateToVatReturnCommand Is Nothing Then
                    _navigateToVatReturnCommand = New RelayCommand(Sub() RaiseEvent NavigateToVatReturnRequested(Me, EventArgs.Empty))
                End If
                Return _navigateToVatReturnCommand
            End Get
        End Property

        ''' <summary>Raised when the user clicks the VAT Payable tile to open the VAT Return view.</summary>
        Public Event NavigateToVatReturnRequested As EventHandler

        ' ─── Refresh Hook ─────────────────────────────────────────────────────────

        Private _prevIsBusy As Boolean = False

        ''' <summary>
        ''' Detects the IsBusy True→False transition that marks the end of every data-load cycle
        ''' and fires <see cref="ApplyVatDataFromServiceAsync"/> so the VAT KPI fields stay in sync
        ''' without modifying the private <c>LoadDataAsync</c> method in the ACC-07 source file.
        ''' </summary>
        Protected Overrides Sub OnPropertyChanged(e As PropertyChangedEventArgs)
            MyBase.OnPropertyChanged(e)
            If e.PropertyName = NameOf(IsBusy) Then
                If Not IsBusy AndAlso _prevIsBusy Then
                    Dim t = ApplyVatDataFromServiceAsync()
                End If
                _prevIsBusy = IsBusy
            End If
        End Sub

        Private Async Function ApplyVatDataFromServiceAsync() As Task
            Try
                Dim dto = Await _overviewService.GetOverviewAsync()
                VatPayable = dto.VatPayable
                VatPayableLabel = dto.VatPayableLabel
                VatFilingDueDate = dto.VatFilingDueDate
                VatPayableSeverity = dto.VatPayableSeverity
                IsVatRegistered = dto.IsVatRegistered
            Catch ex As Exception
                ' VAT KPI is non-critical; swallow to avoid cascading failures on the dashboard
            End Try
        End Function

    End Class

End Namespace
