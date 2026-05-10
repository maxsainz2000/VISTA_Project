Imports System.Data
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.Json
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.POS.Data
Imports MerchSys.POS.Entities
Imports MerchSys.SharedKernel.Events

Namespace Services

    Public Class ReceiptIntegrityService
        Implements IReceiptIntegrityService

        Private Const AlgorithmId As String = "SHA-256-v1"
        Private Const MaxSequenceRetries As Integer = 10

        Private ReadOnly _context As POSDbContext
        Private ReadOnly _mediator As IMediator
        Private ReadOnly _logger As ILogger(Of ReceiptIntegrityService)

        Public Sub New(context As POSDbContext, mediator As IMediator, logger As ILogger(Of ReceiptIntegrityService))
            _context = context
            _mediator = mediator
            _logger = logger
        End Sub

        ''' <inheritdoc/>
        Public Async Function ComputeAndPersistAsync(receipt As OfficialReceipt) As Task(Of ReceiptIntegrity) Implements IReceiptIntegrityService.ComputeAndPersistAsync
            Dim fullReceipt = Await _context.OfficialReceipts.
                Include(Function(r) r.Transaction).
                    ThenInclude(Function(t) t.Lines).
                FirstAsync(Function(r) r.Id = receipt.Id)

            Dim previousHash = Await GetPreviousHashAsync(fullReceipt.IssueDate.Year, fullReceipt.Id)
            Dim canonical = BuildCanonicalPayload(fullReceipt)
            Dim hash = ComputeHash(canonical, previousHash)

            Dim integrity As New ReceiptIntegrity() With {
                .ReceiptId = fullReceipt.Id,
                .IntegrityHash = hash,
                .PreviousHash = previousHash,
                .RetentionExpiresAt = fullReceipt.IssueDate.AddYears(10),
                .IsImmutable = True,
                .HashAlgorithm = AlgorithmId,
                .CanonicalPayload = canonical
            }

            _context.ReceiptIntegrities.Add(integrity)
            Await _context.SaveChangesAsync()

            Return integrity
        End Function

        ''' <inheritdoc/>
        Public Async Function ValidateAsync(receiptId As Integer) As Task(Of IntegrityValidationResult) Implements IReceiptIntegrityService.ValidateAsync
            Dim integrity = Await _context.ReceiptIntegrities.
                FirstOrDefaultAsync(Function(i) i.ReceiptId = receiptId)

            If integrity Is Nothing Then
                Return New IntegrityValidationResult() With {
                    .IsValid = False, .ReceiptId = receiptId,
                    .ExpectedHash = String.Empty, .ActualHash = String.Empty
                }
            End If

            Dim receipt = Await _context.OfficialReceipts.
                Include(Function(r) r.Transaction).
                    ThenInclude(Function(t) t.Lines).
                FirstOrDefaultAsync(Function(r) r.Id = receiptId)

            If receipt Is Nothing Then
                Return New IntegrityValidationResult() With {
                    .IsValid = False, .ReceiptId = receiptId,
                    .ExpectedHash = integrity.IntegrityHash, .ActualHash = String.Empty
                }
            End If

            Dim canonical = BuildCanonicalPayload(receipt)
            Dim actualHash = ComputeHash(canonical, integrity.PreviousHash)
            Dim isValid = String.Equals(actualHash, integrity.IntegrityHash, StringComparison.Ordinal)

            If Not isValid Then
                Await PublishTamperEventAsync(receiptId, receipt.ReceiptNumber, integrity.IntegrityHash, actualHash)
            End If

            Return New IntegrityValidationResult() With {
                .IsValid = isValid, .ReceiptId = receiptId,
                .ExpectedHash = integrity.IntegrityHash, .ActualHash = actualHash
            }
        End Function

        ''' <inheritdoc/>
        Public Async Function ValidateChainAsync(year As Integer) As Task(Of ChainValidationResult) Implements IReceiptIntegrityService.ValidateChainAsync
            Dim integrities = Await _context.ReceiptIntegrities.
                Where(Function(i) i.Receipt.IssueDate.Year = year).
                OrderBy(Function(i) i.ReceiptId).
                Include(Function(i) i.Receipt).
                    ThenInclude(Function(r) r.Transaction).
                        ThenInclude(Function(t) t.Lines).
                ToListAsync()

            Dim result As New ChainValidationResult() With {
                .Year = year,
                .TotalChecked = integrities.Count,
                .IsValid = True
            }

            For Each integrity In integrities
                Dim canonical = BuildCanonicalPayload(integrity.Receipt)
                Dim actualHash = ComputeHash(canonical, integrity.PreviousHash)

                If Not String.Equals(actualHash, integrity.IntegrityHash, StringComparison.Ordinal) Then
                    result.IsValid = False
                    result.FirstFailedReceiptId = integrity.ReceiptId
                    Await PublishTamperEventAsync(integrity.ReceiptId, integrity.Receipt.ReceiptNumber, integrity.IntegrityHash, actualHash)
                    Exit For
                End If
            Next

            Return result
        End Function

        ''' <inheritdoc/>
        Public Async Function GetNextReceiptNumberAsync(year As Integer) As Task(Of String) Implements IReceiptIntegrityService.GetNextReceiptNumberAsync
            Dim nextValue As Integer = -1
            Dim success = False

            For attempt = 1 To MaxSequenceRetries
                Dim concurrencyFailed = False

                Try
                    Using txn = Await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
                        Dim sequence = Await _context.ReceiptSequences.
                            FirstOrDefaultAsync(Function(s) s.Year = year)

                        If sequence Is Nothing Then
                            sequence = New ReceiptSequence() With {
                                .Year = year,
                                .NextValue = 1,
                                .RowVersion = Guid.NewGuid().ToByteArray()
                            }
                            _context.ReceiptSequences.Add(sequence)
                        Else
                            sequence.NextValue += 1
                            sequence.RowVersion = Guid.NewGuid().ToByteArray()
                        End If

                        Await _context.SaveChangesAsync()
                        Await txn.CommitAsync()
                        nextValue = sequence.NextValue
                        success = True
                    End Using
                Catch ex As DbUpdateConcurrencyException
                    concurrencyFailed = True
                    _context.ChangeTracker.Clear()
                End Try

                If success Then Exit For
                If concurrencyFailed Then
                    _logger.LogWarning("Receipt sequence concurrency conflict on attempt {Attempt}/{Max} for year {Year}",
                        attempt, MaxSequenceRetries, year)
                End If
            Next

            If Not success Then
                Throw New InvalidOperationException(
                    $"Unable to acquire receipt sequence for year {year} after {MaxSequenceRetries} attempts.")
            End If

            Return $"OR-{year:D4}-{nextValue:D4}"
        End Function

        ' --- Private helpers ---

        Private Async Function GetPreviousHashAsync(year As Integer, currentReceiptId As Integer) As Task(Of String)
            Dim previous = Await _context.ReceiptIntegrities.
                Where(Function(i) i.Receipt.IssueDate.Year = year AndAlso i.ReceiptId < currentReceiptId).
                OrderByDescending(Function(i) i.ReceiptId).
                FirstOrDefaultAsync()
            Return If(previous Is Nothing, String.Empty, previous.IntegrityHash)
        End Function

        Private Async Function PublishTamperEventAsync(receiptId As Integer, receiptNumber As String, expectedHash As String, actualHash As String) As Task
            Dim tamperEvent As New ReceiptTamperDetectedEvent() With {
                .ReceiptId = receiptId,
                .ReceiptNumber = receiptNumber,
                .ExpectedHash = expectedHash,
                .ActualHash = actualHash,
                .DetectedAt = DateTime.UtcNow,
                .DetectedBy = NameOf(ReceiptIntegrityService)
            }
            Await _mediator.Publish(tamperEvent)
            _logger.LogWarning(
                "BIR receipt tamper detected: ReceiptId={ReceiptId} ReceiptNumber={ReceiptNumber} Expected={Expected} Actual={Actual}",
                receiptId, receiptNumber, expectedHash, actualHash)
        End Function

        ''' <summary>
        ''' Builds a deterministic JSON canonical payload from the current state of
        ''' <paramref name="receipt"/> and its transaction lines (sorted by ProductId).
        ''' The payload is hashed to produce <see cref="ReceiptIntegrity.IntegrityHash"/>.
        ''' </summary>
        Private Shared Function BuildCanonicalPayload(receipt As OfficialReceipt) As String
            Dim lines = If(receipt.Transaction?.Lines,
                          Enumerable.Empty(Of SalesTransactionLine)())

            Dim sortedItems = lines.
                OrderBy(Function(l) l.ProductId).
                Select(Function(l) New With {
                    Key .ProductId = l.ProductId,
                    Key .Qty = l.Quantity,
                    Key .UnitPrice = l.UnitPrice,
                    Key .LineTotal = l.LineTotal
                }).
                ToArray()

            Dim payloadObj = New With {
                Key .ReceiptNumber = receipt.ReceiptNumber,
                Key .IssueDate = receipt.IssueDate.ToString("o"),
                Key .TransactionId = receipt.TransactionId,
                Key .BusinessTIN = If(receipt.BusinessTIN, String.Empty),
                Key .TotalAmount = receipt.TotalAmount,
                Key .VatAmount = receipt.VatAmount,
                Key .Items = sortedItems
            }

            Return JsonSerializer.Serialize(payloadObj)
        End Function

        Private Shared Function ComputeHash(canonical As String, previousHash As String) As String
            Dim input = Encoding.UTF8.GetBytes(canonical & previousHash)
            Dim hashBytes = SHA256.HashData(input)
            Return Convert.ToHexString(hashBytes).ToLowerInvariant()
        End Function

    End Class

End Namespace
