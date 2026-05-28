Imports System
Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.ProviderSpike.Spike

Namespace Spike

    Public Module Program

        Public Sub Main()
            MainAsync().GetAwaiter().GetResult()
        End Sub

        Private Async Function MainAsync() As Task
            System.Console.WriteLine("INFRA-23 Spike: Starting Verification...")

            Using db As New SpikeContext()
                ' 1. Recreate database schema for spike tables
                Await db.Database.EnsureDeletedAsync()
                Await db.Database.EnsureCreatedAsync()
                System.Console.WriteLine("OK: EnsureCreated schema completed.")

                ' 2. Insert test product and batches
                Dim prod As New SpikeProduct With {.Name = "Test Product"}
                db.Products.Add(prod)
                Await db.SaveChangesAsync()

                Dim b1 As New SpikeBatch With {.ProductId = prod.Id, .QuantityRemaining = 10}
                Dim b2 As New SpikeBatch With {.ProductId = prod.Id, .QuantityRemaining = 5}
                db.Batches.Add(b1)
                db.Batches.Add(b2)
                Await db.SaveChangesAsync()
                System.Console.WriteLine("OK: CRUD inserts completed.")

                ' 3. Query with Include using ToListAsync
                Dim queryResult = Await db.Products.
                    Include(Function(p) p.Batches).
                    Where(Function(p) p.Name = "Test Product").
                    ToListAsync()

                If Enumerable.Count(queryResult) = 0 Then
                    Throw New Exception("Materialization failed: product list empty.")
                End If

                Dim loadedProduct = queryResult(0)
                If loadedProduct.Batches.Count <> 2 Then
                    Throw New Exception("Materialization failed: batches navigation not loaded correctly.")
                End If
                System.Console.WriteLine("OK: LINQ query and ToListAsync navigation load completed.")

                ' 4. Optimistic Concurrency Check
                Dim bId = b1.Id
                Using dbA As New SpikeContext()
                    Using dbB As New SpikeContext()
                        Dim batchA = Await dbA.Batches.FindAsync(bId)
                        Dim batchB = Await dbB.Batches.FindAsync(bId)

                        batchA.QuantityRemaining -= 2
                        Await dbA.SaveChangesAsync()
                        System.Console.WriteLine("OK: Context A saved changes successfully.")

                        batchB.QuantityRemaining -= 3
                        Try
                            Await dbB.SaveChangesAsync()
                            Throw New Exception("Concurrency check failed: Context B did not throw DbUpdateConcurrencyException.")
                        Catch ex As DbUpdateConcurrencyException
                            System.Console.WriteLine("OK: Context B threw DbUpdateConcurrencyException as expected.")
                        End Try
                    End Using
                End Using

                ' 5. Pessimistic Lock FOR UPDATE Check
                Using tx = Await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted)
                    System.Console.WriteLine("Starting SELECT FOR UPDATE locking query...")
                    Dim lockedBatch = Await db.Batches.
                        FromSqlRaw("SELECT * FROM Spike_Batches WHERE Id = {0} FOR UPDATE", bId).
                        SingleOrDefaultAsync()

                    If lockedBatch Is Nothing Then
                        Throw New Exception("SELECT FOR UPDATE returned null.")
                    End If
                    System.Console.WriteLine("OK: SELECT FOR UPDATE executed and locked successfully.")
                    Await tx.CommitAsync()
                End Using

                ' Clean up spike tables
                Await db.Database.EnsureDeletedAsync()
                System.Console.WriteLine("OK: Schema cleanup completed successfully.")
            End Using

            System.Console.WriteLine("ALL SPIKE CHECKS: OK")
        End Function

    End Module

End Namespace
