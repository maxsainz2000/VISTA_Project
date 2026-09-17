Imports System
Imports MerchSys.SharedKernel.Entities

Namespace Entities

    ''' <summary>
    ''' Authoritative ledger record of which inventory stock batches were consumed
    ''' for a POS sale, preserving exact quantity and unit cost at sale time.
    ''' </summary>
    Public Class SaleCogsRecord
        Inherits BaseEntity

        ''' <summary>POS transaction this deduction belonged to. Cross-module reference (no FK).</summary>
        Public Property TransactionId As Integer

        ''' <summary>Product whose batches were drawn from.</summary>
        Public Property ProductId As Integer

        ''' <summary>FK to the specific StockBatch consumed. May refer to a fully-depleted batch.</summary>
        Public Property BatchId As Integer

        ''' <summary>Units taken from this batch for this sale.</summary>
        Public Property QuantityDeducted As Integer

        ''' <summary>Unit cost on the batch at the time of deduction. Frozen here so later batch edits cannot alter history.</summary>
        Public Property UnitCost As Decimal

        ''' <summary>QuantityDeducted * UnitCost. Stored, not computed, for trivial Accounting SUM.</summary>
        Public Property Cogs As Decimal

        ''' <summary>UTC timestamp the deduction was committed.</summary>
        Public Property DeductedAt As DateTime

        ''' <summary>The stock batch this record belongs to.</summary>
        Public Property Batch As StockBatch
    End Class

End Namespace
