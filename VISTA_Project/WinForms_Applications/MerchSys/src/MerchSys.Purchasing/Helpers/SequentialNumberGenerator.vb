Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Purchasing.Data
Imports MerchSys.Purchasing.Entities

Namespace Helpers

    ''' <summary>
    ''' Generates sequential identifiers in PREFIX-YYYY-XXXX format.
    ''' Concurrency-safe sequence generation backed by Pur_OrderSequences table.
    ''' </summary>
    Public Class SequentialNumberGenerator

        ''' <summary>
        ''' Generates the next sequential number for the given prefix and year in a serialized transaction with retries.
        ''' </summary>
        Public Shared Async Function GetNextNumberAsync(db As PurchasingDbContext, prefix As String, year As Integer) As Task(Of String)
            Dim seqKey As String = $"{prefix}-{year}"
            Dim nextValue As Integer = -1
            Dim success = False
            Dim MaxSequenceRetries As Integer = 10

            For attempt = 1 To MaxSequenceRetries
                Dim concurrencyFailed = False

                Try
                    Using txn = Await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)
                        Dim sequence = Await db.OrderSequences.
                            FirstOrDefaultAsync(Function(s) s.SeqKey = seqKey)

                        If sequence Is Nothing Then
                            sequence = New OrderSequence() With {
                                .SeqKey = seqKey,
                                .NextValue = 1
                            }
                            db.OrderSequences.Add(sequence)
                        Else
                            sequence.NextValue += 1
                        End If

                        Await db.SaveChangesAsync()
                        Await txn.CommitAsync()
                        nextValue = sequence.NextValue
                        success = True
                    End Using
                Catch ex As DbUpdateConcurrencyException
                    ' Update-path race: another client advanced the row's RowVersion. Retry.
                    concurrencyFailed = True
                    db.ChangeTracker.Clear()
                Catch ex As DbUpdateException
                    ' First-insert race: two clients created the same SeqKey row concurrently.
                    ' The loser gets a duplicate-key violation (a DbUpdateException, not a
                    ' concurrency exception). Clear the tracker and retry — the row now exists,
                    ' so the next attempt takes the increment path.
                    concurrencyFailed = True
                    db.ChangeTracker.Clear()
                End Try

                If success Then Exit For
            Next

            If Not success Then
                Throw New InvalidOperationException(
                    $"Unable to acquire purchasing sequence for key {seqKey} after {MaxSequenceRetries} attempts.")
            End If

            Return $"{prefix}-{year}-{nextValue:D4}"
        End Function

    End Class

End Namespace
