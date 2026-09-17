Namespace Paging

    ''' <summary>
    ''' A keyset (seek) pagination request for an unbounded, date-ordered read path
    ''' (transaction history, shrinkage history, stock-audit history, per-account credit history).
    '''
    ''' Keyset paging is used instead of OFFSET paging because it stays O(page) regardless of how
    ''' deep the caller pages — the cursor is the last row already shown, so the database seeks
    ''' straight to the next slice via the ordered index rather than counting past skipped rows.
    '''
    ''' The cursor is the composite <c>(CursorDate, CursorId)</c> of the last row returned. The
    ''' <c>Id</c> tie-breaker is required because the ordered date column is not unique — two rows
    ''' sharing a timestamp would otherwise be dropped or duplicated across page boundaries.
    ''' INFRA-34.
    ''' </summary>
    Public Class PageRequest

        Private _pageSize As Integer = 100

        ''' <summary>Maximum rows to return for this page. Defaults to 100.</summary>
        Public Property PageSize As Integer
            Get
                Return _pageSize
            End Get
            Set(value As Integer)
                _pageSize = If(value <= 0, 100, If(value > 1000, 1000, value))
            End Set
        End Property

        ''' <summary>
        ''' Keyset cursor — the ordered-date value of the last row already shown. Nothing on the
        ''' first page (no rows seen yet).
        ''' </summary>
        Public Property CursorDate As DateTime?

        ''' <summary>
        ''' Keyset cursor tie-breaker — the <c>Id</c> of the last row already shown. Declared as
        ''' <c>Long?</c> so it accommodates both Integer and BIGINT identity columns. Nothing on the
        ''' first page.
        ''' </summary>
        Public Property CursorId As Long?

        ''' <summary>Optional inclusive lower bound on the ordered date column (date-range filter).</summary>
        Public Property FromUtc As DateTime?

        ''' <summary>Optional inclusive upper bound on the ordered date column (date-range filter).</summary>
        Public Property ToUtc As DateTime?

        Public Sub New()
        End Sub

        Public Sub New(pageSize As Integer)
            Me.PageSize = If(pageSize > 0, pageSize, 100)
        End Sub

        ''' <summary>True when no cursor is set — i.e. this is the first page of a fresh load.</summary>
        Public ReadOnly Property IsFirstPage As Boolean
            Get
                Return Not CursorId.HasValue
            End Get
        End Property

    End Class

End Namespace
