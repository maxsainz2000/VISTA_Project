Imports System.Data
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.Json
Imports MediatR
Imports Microsoft.Data.Sqlite
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
        Private _integrityChainList As List(Of ReceiptIntegrity)

        Public Sub New(context As POSDbContext, mediator As IMediator, logger As ILogger(Of ReceiptIntegrityService))
            _context = context
            _mediator = mediator
            _logger = logger
        End Sub

        ''' <inheritdoc/>
        Public Async Function ComputeAndPersistAsync(receipt As OfficialReceipt) As Task(Of ReceiptIntegrity) Implements IReceiptIntegrityService.ComputeAndPersistAsync
            ' Idempotency guard: if integrity already exists for this receipt, return it.
            Dim existing = Await _context.ReceiptIntegrities.
                FirstOrDefaultAsync(Function(i) i.ReceiptId = receipt.Id)
            If existing IsNot Nothing Then
                Return existing
            End If

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
            _integrityChainList = New List(Of ReceiptIntegrity)()
            Dim vcConnStr = _context.Database.GetConnectionString()
            Using vcConn As New SqliteConnection(vcConnStr)
                Await vcConn.OpenAsync()

                Using vcCmd = vcConn.CreateCommand()
                    vcCmd.CommandText = "SELECT ri.Id, ri.ReceiptId, ri.IntegrityHash, ri.PreviousHash " &
                                         "FROM Pos_ReceiptIntegrity ri " &
                                         "INNER JOIN Pos_OfficialReceipts r ON r.Id = ri.ReceiptId " &
                                         "WHERE CAST(strftime('%Y', r.IssueDate) AS INTEGER) = @year " &
                                         "ORDER BY ri.ReceiptId"
                    vcCmd.Parameters.Add(New SqliteParameter("@year", year))
                    Using vcReader = vcCmd.ExecuteReader()
                        While vcReader.Read()
                            _integrityChainList.Add(New ReceiptIntegrity With {
                                .Id = vcReader.GetInt32(0),
                                .ReceiptId = vcReader.GetInt32(1),
                                .IntegrityHash = vcReader.GetString(2),
                                .PreviousHash = vcReader.GetString(3)
                            })
                        End While
                    End Using
                End Using

                If _integrityChainList.Count > 0 Then
                    Dim receiptIds = String.Join(",", _integrityChainList.Select(Function(i) i.ReceiptId))
                    Dim receiptMap As New Dictionary(Of Integer, OfficialReceipt)()
                    Using rCmd = vcConn.CreateCommand()
                        rCmd.CommandText = "SELECT Id, TransactionId, ReceiptNumber, BusinessTIN, IssueDate, TotalAmount, VatAmount " &
                                            $"FROM Pos_OfficialReceipts WHERE Id IN ({receiptIds})"
                        Using rReader = rCmd.ExecuteReader()
                            While rReader.Read()
                                Dim receipt As New OfficialReceipt With {
                                    .Id = rReader.GetInt32(0),
                                    .TransactionId = rReader.GetInt32(1),
                                    .ReceiptNumber = rReader.GetString(2),
                                    .BusinessTIN = If(rReader.IsDBNull(3), Nothing, rReader.GetString(3)),
                                    .IssueDate = rReader.GetDateTime(4),
                                    .TotalAmount = rReader.GetDecimal(5),
                                    .VatAmount = rReader.GetDecimal(6)
                                }
                                receiptMap(receipt.Id) = receipt
                            End While
                        End Using
                    End Using

                    Dim txIds = String.Join(",", receiptMap.Values.Select(Function(r) r.TransactionId).Distinct())
                    Dim lineMap As New Dictionary(Of Integer, List(Of SalesTransactionLine))()
                    Using lCmd = vcConn.CreateCommand()
                        lCmd.CommandText = "SELECT TransactionId, ProductId, Quantity, UnitPrice, LineTotal " &
                                            $"FROM Pos_SalesTransactionLines WHERE TransactionId IN ({txIds})"
                        Using lReader = lCmd.ExecuteReader()
                            While lReader.Read()
                                Dim line As New SalesTransactionLine With {
                                    .TransactionId = lReader.GetInt32(0),
                                    .ProductId = lReader.GetInt32(1),
                                    .Quantity = lReader.GetInt32(2),
                                    .UnitPrice = lReader.GetDecimal(3),
                                    .LineTotal = lReader.GetDecimal(4)
                                }
                                If Not lineMap.ContainsKey(line.TransactionId) Then lineMap(line.TransactionId) = New List(Of SalesTransactionLine)()
                                lineMap(line.TransactionId).Add(line)
                            End While
                        End Using
                    End Using

                    For Each receipt In receiptMap.Values
                        Dim txLines As List(Of SalesTransactionLine) = Nothing
                        If Not lineMap.TryGetValue(receipt.TransactionId, txLines) Then txLines = New List(Of SalesTransactionLine)()
                        Dim tx As New SalesTransaction With {.Id = receipt.TransactionId}
                        For Each ln In txLines : tx.Lines.Add(ln) : Next
                        receipt.Transaction = tx
                    Next

                    For Each integrity In _integrityChainList
                        Dim receipt As OfficialReceipt = Nothing
                        If receiptMap.TryGetValue(integrity.ReceiptId, receipt) Then integrity.Receipt = receipt
                    Next
                End If
            End Using
            Dim integrities As List(Of ReceiptIntegrity) = _integrityChainList

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
