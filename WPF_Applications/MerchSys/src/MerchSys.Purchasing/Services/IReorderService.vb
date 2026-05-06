Imports MerchSys.Purchasing.Entities

Namespace Services

    Public Interface IReorderService

        ''' <summary>
        ''' Queries current stock levels via MediatR and generates <see cref="ReorderSuggestion"/> records
        ''' for every active <see cref="ReorderConfig"/> whose product stock is at or below its reorder point.
        ''' Skips products that already have a Pending suggestion.
        ''' </summary>
        Function GenerateSuggestionsAsync() As Task(Of List(Of ReorderSuggestion))

        ''' <summary>Returns all suggestions in the "Pending" state.</summary>
        Function GetPendingSuggestionsAsync() As Task(Of List(Of ReorderSuggestion))

        ''' <summary>
        ''' Marks the suggestion as Accepted and creates a draft <see cref="PurchaseOrder"/> from it.
        ''' </summary>
        ''' <param name="id">Primary key of the <see cref="ReorderSuggestion"/> to accept.</param>
        ''' <returns>The newly created draft <see cref="PurchaseOrder"/>.</returns>
        Function AcceptSuggestionAsync(id As Integer) As Task(Of PurchaseOrder)

        ''' <summary>Marks the suggestion as Dismissed without creating a purchase order.</summary>
        ''' <param name="id">Primary key of the <see cref="ReorderSuggestion"/> to dismiss.</param>
        Function DismissSuggestionAsync(id As Integer) As Task

        ''' <summary>Persists an updated <see cref="ReorderConfig"/> (upsert by Id).</summary>
        Function UpdateConfigAsync(config As ReorderConfig) As Task(Of ReorderConfig)

        ''' <summary>Returns all <see cref="ReorderConfig"/> records regardless of active state.</summary>
        Function GetAllConfigsAsync() As Task(Of List(Of ReorderConfig))

        ''' <summary>Returns all <see cref="ReorderSuggestion"/> records regardless of status.</summary>
        Function GetAllSuggestionsAsync() As Task(Of List(Of ReorderSuggestion))

    End Interface

End Namespace
