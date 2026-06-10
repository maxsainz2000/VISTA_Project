Namespace Paging

    ''' <summary>
    ''' One page of a keyset-paginated read, plus the cursor needed to fetch the next page.
    '''
    ''' <para><c>HasMore</c> is derived without a <c>COUNT(*)</c> (which is itself costly on a large
    ''' table): the read fetches <c>PageSize + 1</c> rows, and if the extra row came back the producer
    ''' sets <c>HasMore = True</c> and drops it from <c>Items</c>. <c>NextCursorDate</c>/<c>NextCursorId</c>
    ''' are the ordered-date/Id of the last <em>kept</em> row, to be passed back in the next
    ''' <see cref="PageRequest"/>.</para>
    ''' INFRA-34.
    ''' </summary>
    ''' <typeparam name="T">The row/entity type of the page.</typeparam>
    Public Class PagedResult(Of T)

        ''' <summary>The rows for this page (at most <c>PageRequest.PageSize</c>).</summary>
        Public Property Items As IReadOnlyList(Of T)

        ''' <summary>True when at least one more row exists beyond this page.</summary>
        Public Property HasMore As Boolean

        ''' <summary>Ordered-date cursor of the last kept row; pass into the next request. Nothing when empty.</summary>
        Public Property NextCursorDate As DateTime?

        ''' <summary>Id cursor of the last kept row; pass into the next request. Nothing when empty.</summary>
        Public Property NextCursorId As Long?

        Public Sub New()
            Items = New List(Of T)()
        End Sub

        Public Sub New(items As IReadOnlyList(Of T), hasMore As Boolean,
                       nextCursorDate As DateTime?, nextCursorId As Long?)
            Me.Items = items
            Me.HasMore = hasMore
            Me.NextCursorDate = nextCursorDate
            Me.NextCursorId = nextCursorId
        End Sub

        ''' <summary>An empty page with no further rows.</summary>
        Public Shared Function Empty() As PagedResult(Of T)
            Return New PagedResult(Of T)(New List(Of T)(), False, Nothing, Nothing)
        End Function

    End Class

End Namespace
