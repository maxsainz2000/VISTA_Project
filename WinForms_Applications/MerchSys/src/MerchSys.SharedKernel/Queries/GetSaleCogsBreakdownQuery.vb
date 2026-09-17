Imports System.Collections.Generic
Imports MediatR

Namespace Queries

    ''' <summary>
    ''' Sent by the Accounting module to retrieve the actual per-batch COGS breakdown
    ''' for a completed sale's line item. Mirrors the Inv_SaleCogs ledger written by
    ''' the Inventory module's SaleCompletedHandler.
    ''' Returns an empty list if the deduction has not been committed yet
    ''' (e.g., Accounting handler raced ahead of Inventory's SaveChanges) — callers must
    ''' treat that as a transient condition and fall back gracefully.
    ''' </summary>
    Public Class GetSaleCogsBreakdownQuery
        Implements IRequest(Of GetSaleCogsBreakdownResult)

        Public Property TransactionId As Integer
        Public Property ProductId As Integer
    End Class

    Public Class GetSaleCogsBreakdownResult
        ''' <summary>Total COGS for this (Transaction, Product) — SUM of per-batch COGS.</summary>
        Public Property TotalCogs As Decimal

        ''' <summary>Per-batch detail; empty when no Inv_SaleCogs rows match.</summary>
        Public Property Lines As List(Of SaleCogsLine) = New List(Of SaleCogsLine)()
    End Class

    Public Class SaleCogsLine
        Public Property BatchId As Integer
        Public Property QuantityDeducted As Integer
        Public Property UnitCost As Decimal
        Public Property Cogs As Decimal
    End Class

End Namespace
