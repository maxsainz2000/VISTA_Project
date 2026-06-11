# Code Audit Report: MerchSys.POS

This report presents a comprehensive code audit of the `MerchSys.POS` module. Several issues, ranging from a critical SQLite-specific runtime crash on MariaDB to fragile raw ADO.NET query dependencies and MVVM pattern deviations, have been identified.

## ✅ Verification Addendum — 2026-06-11

Verified against source under `WPF_Applications/MerchSys/src/MerchSys.POS/`. The finding 1 *bug* is real and critical, but its *fix* (and finding 2's) is **dangerous**. **Where this addendum conflicts with a finding's severity or fix, the addendum governs.**

> ⚠️ **Landmine — keep the raw ADO.NET.** Full-entity `ToListAsync()` (incl. `.Include`/`.Where`) silently returns an empty list in VB.NET + EF Core 10 (`LLM_Wiki/agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md`). Several "convert to EF LINQ" fixes below would silently disable the feature they touch. Scalar/anonymous projections, `FirstOrDefaultAsync`, and `CountAsync` are safe.

| # | Reported | Verified verdict & action |
|---|---|---|
| 1 `strftime` on MariaDB | Critical | **Bug confirmed; fix dangerous.** `strftime` crashes 100% on MariaDB. Do **not** apply the EF `.Include(...).Where(...).ToListAsync()` rewrite — it empties the result and turns the tamper-chain check into a silent no-op. Fix: keep raw ADO.NET, change `strftime('%Y', r.IssueDate)` → `YEAR(r.IssueDate)`; optionally make `_integrityChainList` a local. |
| 2 Raw ADO / column-index | High | **Smell valid; `.ToListAsync()` fix dangerous → Low-Med.** Keep raw ADO.NET. Column-index fragility is overstated (the SELECT defines order, robust to table changes). |
| 3 DB in CreditManagementViewModel | High | **Confirmed (MVVM violation).** Move to a service method, keeping raw ADO.NET / projection. |
| 4 `CountAsync` txn-number race | Medium | **Confirmed — and a genuine cross-terminal race** (unlike the class-field findings). Replace with a DB sequence table; `ReceiptIntegrityService.GetNextReceiptNumberAsync` already implements the serializable-sequence pattern to copy. Ensure a unique index on `TransactionNumber`. |
| 5 TIN regex mismatch | Medium | **Confirmed.** Even `999-999-999` is client-rejected/backend-accepted. Align the client `RegularExpression` to the backend `TinPattern`. |
| 6 SyncLock on SemaphoreSlim | Medium | **Confirmed.** `Invalidate()` (`SyncLock`) and `GetAsync()` (`WaitAsync`) don't mutually exclude. Use `_lock.Wait()/Release()` in `Invalidate`. |
| 7 Archival delete loop | Medium | **Confirmed** (perf). `IN`-clause batch is fine (IDs are ints). |
| 8 VAT centavo drift | Medium | **Confirmed.** `VatAwareReceiptService` never sets `transaction.VatAmount = totals.OutputVat`; sync the header (and `receipt.VatAmount`) to the line aggregate. |
| 9 Hardcoded "12%" label | Low | **Confirmed; systemic** (also `SalesCartView`). Use the snapshot/config rate. |
| 10 Unused `Items` column | Low | **Partly mitigated** — `ReceiptIntegrity.CanonicalPayload` already stores an immutable JSON snapshot of the lines. Dead-schema cleanup only. |

**Missed by this report:** the hardcoded `1.12D` VAT divisor (12% assumption) is systemic in the VAT math (Purchasing + display labels), broader than finding 9.

*Verified by Claude (Opus 4.8) on 2026-06-11. Original findings retained below for traceability.*

---

## Findings Summary Table

| Severity | Category | Component | Description |
| :--- | :--- | :--- | :--- |
| **Critical** | Stability & Bug | [ReceiptIntegrityService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/ReceiptIntegrityService.vb#L115) | SQLite-specific `strftime` function used in a raw database query on a production MariaDB connection, causing runtime crash. |
| **High** | Design Smell & Robustness | Multiple Services: [CreditService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/CreditService.vb), [CartService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/CartService.vb), [SalesReturnService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/SalesReturnService.vb), [DailySummaryService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/DailySummaryService.vb) | Direct raw ADO.NET SQL database calls and fragile column-index mapping (`r.GetInt32(0)`) bypassing Entity Framework Core. |
| **High** | MVVM Pattern Deviation | [CreditManagementViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/ViewModels/CreditManagementViewModel.vb#L460) | Presentation layer (ViewModel) directly instantiates DB connection, writes raw SQL, and queries database, violating MVC/MVVM separation. |
| **Medium** | Stability & Concurrency | [CartService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/CartService.vb#L401) | Non-thread-safe/non-concurrency-safe transaction number generation using `CountAsync()` which leads to duplicate numbers under load. |
| **Medium** | Stability & Bug | [VatSettingsViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/ViewModels/VatSettingsViewModel.vb#L48) & [IVatConfigurationWriter.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/IVatConfigurationWriter.vb#L90) | Mismatched and inconsistent TIN validation regex between the UI ViewModel (allows unhyphenated) and the backend (requires hyphens), plus UI rejection of valid legacy 15-digit branch TIN formats. |
| **Medium** | Concurrency Smell | [VatConfigurationLoader.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/VatConfigurationLoader.vb#L51) | Unsynchronized cache invalidation due to mismatched locking: using `SyncLock` (monitor lock) on a `SemaphoreSlim` object does not block threads using `_lock.WaitAsync()`. |
| **Medium** | Performance Smell | [ReceiptArchivalService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/Archival/ReceiptArchivalService.vb#L243) | Sub-optimal delete loop that issues a separate database call for every record, executing up to 2,000 roundtrips instead of a single bulk delete. |
| **Medium** | Stability & Bug | [CartService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/CartService.vb#L376) & [ReceiptService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/ReceiptService.vb#L67) | Centavo drift between the transaction's stored `VatAmount` (header-level decomposed) and the sum of its lines' `OutputVat` (line-level decomposed). |
| **Low** | Maintainability & Bug | [BirCompliantReceiptBodyComposer.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/ReceiptFormatting/BirCompliantReceiptBodyComposer.vb#L124) | Hardcoded "12%" output VAT string in receipt layout, neglecting to display the actual dynamic VAT rate stored in transaction or configuration. |
| **Low** | Design Smell | [OfficialReceipt.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Entities/OfficialReceipt.vb#L35) & [ReceiptService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/ReceiptService.vb) | Unused `Items` column in `OfficialReceipt` database table. Receipt reprinting queries live transaction tables instead of serializing a snapshot, exposing historical records to data mutations. |

---

## Detailed Findings & Proposed Diffs

### 1. SQLite-Specific `strftime` Query on MariaDB (Critical)

> 🔴 **VERIFICATION (2026-06-11) — bug is real, but the proposed EF rewrite is dangerous.** Keep the raw ADO.NET and replace `strftime('%Y', r.IssueDate)` with `YEAR(r.IssueDate)`. The proposed `.Include(...).Where(...).ToListAsync()` rewrite hits the VB.NET + EF Core 10 full-entity `ToListAsync()` empty-result bug, turning the tamper-chain validation into a silent no-op. See the Verification Addendum at the top.

> [!WARNING]
> **Definite Application Crash in Production**  
> Because the application relies on MariaDB (`MySqlConnector`/`MySqlConnection`) in production, executing `strftime` in `ValidateChainAsync` will throw a database syntax error (`MySqlException: FUNCTION strftime does not exist`), crashing the chain validation flow 100% of the time.

#### Root Cause
In [ReceiptIntegrityService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/ReceiptIntegrityService.vb#L112-L116):
```vb
vcCmd.CommandText = "SELECT ri.Id, ri.ReceiptId, ri.IntegrityHash, ri.PreviousHash " &
                     "FROM Pos_ReceiptIntegrity ri " &
                     "INNER JOIN Pos_OfficialReceipts r ON r.Id = ri.ReceiptId " &
                     "WHERE CAST(strftime('%Y', r.IssueDate) AS INTEGER) = @year " &
                     "ORDER BY ri.ReceiptId"
```
* **Analysis**: This method uses raw SQL. The function `strftime` is SQLite-specific. Under MariaDB/MySQL, the query fails immediately.
* **Correction**: By replacing this raw query with Entity Framework Core LINQ, the query is kept database-agnostic and clean: EF Core translates `.Year` to the correct native date extractor (`YEAR(...)` in MySQL, `strftime('%Y', ...)` in SQLite). It also eliminates raw ADO.NET connection management, reader loops, and manually joined queries.

#### Proposed Correction
```diff
         Public Async Function ValidateChainAsync(year As Integer) As Task(Of ChainValidationResult) Implements IReceiptIntegrityService.ValidateChainAsync
-            _integrityChainList = New List(Of ReceiptIntegrity)()
-            Dim vcConnStr = _context.Database.GetConnectionString()
-            Using vcConn As New MySqlConnection(vcConnStr)
-                Await vcConn.OpenAsync()
-
-                Using vcCmd = vcConn.CreateCommand()
-                    vcCmd.CommandText = "SELECT ri.Id, ri.ReceiptId, ri.IntegrityHash, ri.PreviousHash " &
-                                         "FROM Pos_ReceiptIntegrity ri " &
-                                         "INNER JOIN Pos_OfficialReceipts r ON r.Id = ri.ReceiptId " &
-                                         "WHERE CAST(strftime('%Y', r.IssueDate) AS INTEGER) = @year " &
-                                         "ORDER BY ri.ReceiptId"
-                    vcCmd.Parameters.Add(New MySqlParameter("@year", year))
-                    Using vcReader = vcCmd.ExecuteReader()
-                        While vcReader.Read()
-                            _integrityChainList.Add(New ReceiptIntegrity With {
-                                .Id = vcReader.GetInt32(0),
-                                .ReceiptId = vcReader.GetInt32(1),
-                                .IntegrityHash = vcReader.GetString(2),
-                                .PreviousHash = vcReader.GetString(3)
-                            })
-                        End While
-                    End Using
-                End Using
-
-                If _integrityChainList.Count > 0 Then
-                    Dim receiptIds = String.Join(",", _integrityChainList.Select(Function(i) i.ReceiptId))
-                    Dim receiptMap As New Dictionary(Of Integer, OfficialReceipt)()
-                    Using rCmd = vcConn.CreateCommand()
-                        rCmd.CommandText = "SELECT Id, TransactionId, ReceiptNumber, BusinessTIN, IssueDate, TotalAmount, VatAmount " &
-                                            $"FROM Pos_OfficialReceipts WHERE Id IN ({receiptIds})"
-                        Using rReader = rCmd.ExecuteReader()
-                            While rReader.Read()
-                                Dim receipt As New OfficialReceipt With {
-                                    .Id = rReader.GetInt32(0),
-                                    .TransactionId = rReader.GetInt32(1),
-                                    .ReceiptNumber = rReader.GetString(2),
-                                    .BusinessTIN = If(rReader.IsDBNull(3), Nothing, rReader.GetString(3)),
-                                    .IssueDate = rReader.GetDateTime(4),
-                                    .TotalAmount = rReader.GetDecimal(5),
-                                    .VatAmount = rReader.GetDecimal(6)
-                                }
-                                receiptMap(receipt.Id) = receipt
-                            End While
-                        End Using
-                    End Using
-
-                    Dim txIds = String.Join(",", receiptMap.Values.Select(Function(r) r.TransactionId).Distinct())
-                    Dim lineMap As New Dictionary(Of Integer, List(Of SalesTransactionLine))()
-                    Using lCmd = vcConn.CreateCommand()
-                        lCmd.CommandText = "SELECT TransactionId, ProductId, Quantity, UnitPrice, LineTotal " &
-                                            $"FROM Pos_SalesTransactionLines WHERE TransactionId IN ({txIds})"
-                        Using lReader = lCmd.ExecuteReader()
-                            While lReader.Read()
-                                Dim line As New SalesTransactionLine With {
-                                    .TransactionId = lReader.GetInt32(0),
-                                    .ProductId = lReader.GetInt32(1),
-                                    .Quantity = lReader.GetInt32(2),
-                                    .UnitPrice = lReader.GetDecimal(3),
-                                    .LineTotal = lReader.GetDecimal(4)
-                                }
-                                If Not lineMap.ContainsKey(line.TransactionId) Then lineMap(line.TransactionId) = New List(Of SalesTransactionLine)()
-                                lineMap(line.TransactionId).Add(line)
-                            End Using
-                        End Using
-                    End Using
-
-                    For Each receipt In receiptMap.Values
-                        Dim txLines As List(Of SalesTransactionLine) = Nothing
-                        If Not lineMap.TryGetValue(receipt.TransactionId, txLines) Then txLines = New List(Of SalesTransactionLine)()
-                        Dim tx As New SalesTransaction With {.Id = receipt.TransactionId}
-                        For Each ln In txLines : tx.Lines.Add(ln) : Next
-                        receipt.Transaction = tx
-                    Next
-
-                    For Each integrity In _integrityChainList
-                        Dim receipt As OfficialReceipt = Nothing
-                        If receiptMap.TryGetValue(integrity.ReceiptId, receipt) Then integrity.Receipt = receipt
-                    Next
-                End If
-            End Using
-            Dim integrities As List(Of ReceiptIntegrity) = _integrityChainList
+            Dim integrities = Await _context.ReceiptIntegrities _
+                .Include(Function(ri) ri.Receipt) _
+                    .ThenInclude(Function(r) r.Transaction) _
+                        .ThenInclude(Function(t) t.Lines) _
+                .Where(Function(ri) ri.Receipt.IssueDate.Year = year) _
+                .OrderBy(Function(ri) ri.ReceiptId) _
+                .ToListAsync()
 
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
```

---

### 2. Fragile Raw ADO.NET and Column-Index Mapping in Services (High)

> 🟠 **VERIFICATION (2026-06-11) — smell is real, but the `.ToListAsync()` conversion is dangerous.** These raw reader loops are the documented workaround for the full-entity `ToListAsync()` empty-result bug; converting them returns empty lists at runtime. Keep raw ADO.NET (or use scalar/anonymous projections). Column-index fragility is overstated — the `SELECT` defines column order, so it is robust to table changes. Re-rated **Low-Med**. See the Verification Addendum at the top.

> [!CAUTION]
> **Risk of Runtime Failures and Inhibited Automated Testing**  
> Hardcoding column indices (`r.GetString(1)`, etc.) will fail if any migration modifies or appends column definitions to the database schema. Furthermore, hardcoding `MySqlConnection` blocks in-memory and SQLite unit testing since the connection string and syntax fail in test contexts.

#### Root Cause
In [CreditService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/CreditService.vb#L58-L98) (and similarly in `CartService.vb`, `SalesReturnService.vb`, and `DailySummaryService.vb`), raw queries are run bypassing Entity Framework Core.
```vb
    Dim gaConnStr = _context.Database.GetConnectionString()
    Using gaConn As New MySqlConnection(gaConnStr)
        Await gaConn.OpenAsync()
        Using gaCmd = gaConn.CreateCommand()
            gaCmd.CommandText = "SELECT Id, CustomerName, Phone, Address, CurrentBalance, TotalCreditExtended, " &
                                ...
            Using gaReader = gaCmd.ExecuteReader()
                While gaReader.Read()
                    _creditAccountList.Add(ReadCreditAccount(gaReader))
```
* **Analysis**: `ReadCreditAccount` uses indices: `.CustomerName = r.GetString(1)`. Reordering columns breaks casting. The service is also strongly coupled to `MySqlConnector`.
* **Correction**: Replace these queries with EF Core LINQ.

#### Proposed Correction (Example for `CreditService.vb`)
```diff
         Public Async Function GetAllAccountsAsync() As Task(Of List(Of CreditAccount)) Implements ICreditService.GetAllAccountsAsync
-            _creditAccountList = New List(Of CreditAccount)()
-            Dim gaConnStr = _context.Database.GetConnectionString()
-            Using gaConn As New MySqlConnection(gaConnStr)
-                Await gaConn.OpenAsync()
-                Using gaCmd = gaConn.CreateCommand()
-                    gaCmd.CommandText = "SELECT Id, CustomerName, Phone, Address, CurrentBalance, TotalCreditExtended, " &
-                                        "TotalPaymentsReceived, IsBlocked, LastTransactionDate, Notes, " &
-                                        "IsDeleted, DeletedBy, DeletedAt, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
-                                        "FROM Pos_CreditAccounts WHERE IsDeleted = 0 ORDER BY CustomerName"
-                    Using gaReader = gaCmd.ExecuteReader()
-                        While gaReader.Read()
-                            _creditAccountList.Add(ReadCreditAccount(gaReader))
-                        End While
-                    End Using
-                End Using
-            End Using
-            Return _creditAccountList
+            Return Await _context.CreditAccounts _
+                .AsNoTracking() _
+                .Where(Function(a) Not a.IsDeleted) _
+                .OrderBy(Function(a) a.CustomerName) _
+                .ToListAsync()
         End Function
```
*(Apply matching LINQ transformations to similar methods in `CreditService`, `CartService`, `SalesReturnService`, and `DailySummaryService`.)*

---

### 3. Database Access inside Presentation Layer (High)

> [!CAUTION]
> **Severe Architecture Smell & MVVM Violation**  
> Resolving and instantiating database connections and parsing queries directly within a ViewModel violates separation of concerns. This locks the presentation module to a specific database backend and inhibits unit-testing ViewModels without a concrete DB running.

#### Root Cause
In [CreditManagementViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/ViewModels/CreditManagementViewModel.vb#L460-L480):
```vb
Dim htConnStr = _context.Database.GetConnectionString()
Using htConn As New MySqlConnection(htConnStr)
    Await htConn.OpenAsync()
    Using htCmd = htConn.CreateCommand()
        htCmd.CommandText = "SELECT TransactionDate, TransactionNumber, TotalAmount " &
                             "FROM Pos_SalesTransactions " &
                             "WHERE CustomerId = @accountId AND PaymentMethod = @creditMethod AND IsDeleted = 0 " &
                             "ORDER BY TransactionDate DESC"
```
* **Analysis**: The presentation layer directly implements data-access logic and holds hardcoded queries.
* **Correction**: Create a method on the `ICreditService` interface, e.g. `GetCreditTransactionsAsync(accountId As Integer) As Task(Of List(Of CreditTransactionItem))` and move this query to `CreditService` utilizing EF Core.

#### Proposed Correction
Move the database fetching to `CreditService` (returning structured objects), and refactor the ViewModel method as:
```diff
         Private Async Function LoadHistoryInternalAsync(account As CreditAccount) As Task
             Dim payments = Await _creditService.GetPaymentHistoryAsync(account.Id)
             PaymentHistory.Clear()
             For Each p In payments
                 PaymentHistory.Add(p)
             Next
 
-            _historyTxList = New List(Of SalesTransaction)()
-            Dim htConnStr = _context.Database.GetConnectionString()
-            Using htConn As New MySqlConnection(htConnStr)
-                Await htConn.OpenAsync()
-                Using htCmd = htConn.CreateCommand()
-                    htCmd.CommandText = "SELECT TransactionDate, TransactionNumber, TotalAmount " &
-                                         "FROM Pos_SalesTransactions " &
-                                         "WHERE CustomerId = @accountId AND PaymentMethod = @creditMethod AND IsDeleted = 0 " &
-                                         "ORDER BY TransactionDate DESC"
-                    htCmd.Parameters.Add(New MySqlParameter("@accountId", account.Id))
-                    htCmd.Parameters.Add(New MySqlParameter("@creditMethod", CInt(PaymentMethod.Credit)))
-                    Using htReader = htCmd.ExecuteReader()
-                        While htReader.Read()
-                            _historyTxList.Add(New SalesTransaction With {
-                                .TransactionDate = htReader.GetDateTime(0),
-                                .TransactionNumber = htReader.GetString(1),
-                                .TotalAmount = htReader.GetDecimal(2)
-                            })
-                        End While
-                    End Using
-                End Using
-            End Using
+            Dim transactions = Await _creditService.GetCreditTransactionsAsync(account.Id)
 
             CreditTransactions.Clear()
-            For Each t In _historyTxList
-                CreditTransactions.Add(New CreditTransactionItem() With {
-                    .TransactionDate = t.TransactionDate,
-                    .TransactionNumber = t.TransactionNumber,
-                    .Amount = t.TotalAmount
-                })
-            Next
+            For Each t In transactions
+                CreditTransactions.Add(t)
+            Next
         End Function
```

---

### 4. Non-Thread-Safe Transaction Number Generation (Medium)

> [!WARNING]
> **Unique Index Violation and Transaction Failure under Concurrency**  
> If two terminals check out at the same time, both `CountAsync()` statements can read the same transaction count. The resulting duplicate transaction numbers violate the unique database key constraint, aborting one of the sales.

#### Root Cause
In [CartService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/CartService.vb#L401-L406):
```vb
Private Async Function GenerateTransactionNumberAsync() As Task(Of String)
    Dim year = DateTime.Now.Year
    Dim count = Await _context.SalesTransactions.
        CountAsync(Function(t) t.TransactionDate.Year = year) + 1
    Return $"TX-{year}-{count:D4}"
End Function
```
* **Analysis**: Calculating sequential numbering via `CountAsync() + 1` is non-transactional and subject to standard race conditions.
* **Correction**: Implement transaction sequence logic or utilize DB-backed auto-increment variables.

---

### 5. Inconsistent TIN Validation Regex (Medium)

> [!WARNING]
> **UI Crashing / Data Invalidation**  
> The client-side validation regex allows unhyphenated TIN values (e.g. `123456789`). The backend validation regex (`TinPattern`) requires hyphens. If a user types `123456789`, the UI permits it, but the backend rejects the update, throwing validation errors. Additionally, the client-side regex rejects legacy 15-digit branch TIN formats which the backend explicitly accepts.

#### Root Cause
1. In [VatSettingsViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/ViewModels/VatSettingsViewModel.vb#L48):
   ```vb
   <RegularExpression("^\d{3}-\d{3}-\d{3}-\d{3}$|^\d{9}$|^\d{12}$", ErrorMessage:="TIN must be 9 or 12 digits, or formatted as XXX-XXX-XXX-XXX.")>
   ```
2. In [VatConfigurationWriter.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/IVatConfigurationWriter.vb#L90):
   ```vb
   Private Const TinPattern As String = "^\d{3}-\d{3}-\d{3}(-\d{3}|-\d{5})?$"
   ```
* **Analysis**: The client-side regex matches four groups of three digits (`XXX-XXX-XXX-XXX` or `999999999` or `12-digit`). The backend expects `999-999-999`, `999-999-999-000`, or `999-999-999-00000`. Legacy branch TINs (`999-999-999-00000`) fail client-side validation entirely, making them unconfigurable. Unhyphenated inputs fail backend checks.
* **Correction**: Set the client-side validation pattern to match the backend regex.

#### Proposed Correction
In `VatSettingsViewModel.vb`:
```diff
-        <RegularExpression("^\d{3}-\d{3}-\d{3}-\d{3}$|^\d{9}$|^\d{12}$", ErrorMessage:="TIN must be 9 or 12 digits, or formatted as XXX-XXX-XXX-XXX.")>
+        <RegularExpression("^\d{3}-\d{3}-\d{3}(-\d{3}|-\d{5})?$", ErrorMessage:="TIN must match BIR format: 999-999-999, 999-999-999-000, or 999-999-999-00000.")>
```

---

### 6. Unsynchronized Invalidation Lock in `VatConfigurationLoader` (Medium)

> [!NOTE]
> **Race Condition in Multi-Threaded Cache Clear**  
> `SyncLock _lock` inside `Invalidate()` behaves as a monitor lock on the `SemaphoreSlim` object itself. This does not block or wait for threads entering `_lock.WaitAsync()`, exposing the cache write `_cached = Nothing` to thread synchronization issues.

#### Root Cause
In [VatConfigurationLoader.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/VatConfigurationLoader.vb#L51-L55):
```vb
Public Sub Invalidate()
    SyncLock _lock
        _cached = Nothing
    End SyncLock
End Sub
```
* **Analysis**: `GetAsync()` protects cache population using `Await _lock.WaitAsync()`. `Invalidate()` uses `SyncLock _lock`. They do not synchronize with each other.
* **Correction**: Use standard synchronous `Wait()` and `Release()` on the semaphore in `Invalidate()`.

#### Proposed Correction
```diff
         Public Sub Invalidate()
-            SyncLock _lock
-                _cached = Nothing
-            End SyncLock
+            _lock.Wait()
+            Try
+                _cached = Nothing
+            Finally
+                _lock.Release()
+            End Try
         End Sub
```

---

### 7. Inefficient Archival Loop Queries (Medium)

> [!NOTE]
> **Performance Degradation**  
> Iterating over candidates and executing individual delete command requests (`ExecuteSqlRawAsync`) creates $2N$ queries. A batch of 500 records results in 1,000 DB trips, resulting in database overhead.

#### Root Cause
In [ReceiptArchivalService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/Archival/ReceiptArchivalService.vb#L243-L255):
```vb
For Each item In batch
    Await db.Database.ExecuteSqlRawAsync(
        "DELETE FROM `Pos_ReceiptIntegrity` WHERE `ReceiptId` = {0}",
        {item.Receipt.Id},
        cancellationToken)
Next
```
* **Analysis**: Deletes are executed in a loop.
* **Correction**: Compile receipt IDs into a string and issue a single SQL command with an `IN` clause.

#### Proposed Correction
```diff
-                For Each item In batch
-                    Await db.Database.ExecuteSqlRawAsync(
-                        "DELETE FROM `Pos_ReceiptIntegrity` WHERE `ReceiptId` = {0}",
-                        {item.Receipt.Id},
-                        cancellationToken)
-                Next
-
-                For Each item In batch
-                    Await db.Database.ExecuteSqlRawAsync(
-                        "DELETE FROM `Pos_OfficialReceipts` WHERE `Id` = {0}",
-                        {item.Receipt.Id},
-                        cancellationToken)
-                Next
+                Dim targetIds = String.Join(",", batch.Select(Function(x) x.Receipt.Id))
+                Await db.Database.ExecuteSqlRawAsync(
+                    $"DELETE FROM `Pos_ReceiptIntegrity` WHERE `ReceiptId` IN ({targetIds})",
+                    cancellationToken)
+                Await db.Database.ExecuteSqlRawAsync(
+                    $"DELETE FROM `Pos_OfficialReceipts` WHERE `Id` IN ({targetIds})",
+                    cancellationToken)
```

---

### 8. Centavo Drift / Inconsistent VAT Calculations (Medium)

> [!WARNING]
> **Accounting Mismatch between Header totals and Line details**  
> The transaction and receipt `VatAmount` headers are calculated by decomposing the transaction gross total amount. However, compliance demands that line items are decomposed individually and aggregated. Since Banker's rounding occurs at each line, the sum of lines (`totals.OutputVat`) can differ from the header-level calculation by several centavos, leaving inconsistent database columns.

#### Root Cause
1. In `CartService.RecalculateTotalsAsync` and `ReceiptService.GenerateReceiptAsync`, the VAT total is decomposed on the transaction gross total:
   ```vb
   vatAmount = transaction.TotalAmount - Math.Round(transaction.TotalAmount / (1D + vatRate), 2)
   ```
2. In `VatAwareReceiptService.GenerateReceiptAsync`, line VAT calculations are executed per-line using `VatCalculator` and aggregated:
   ```vb
   Dim totals = _vatCalculator.AggregateTransaction(transaction, config)
   ```
   However, the stored `transaction.VatAmount` (calculated at cart checkout) and `receipt.VatAmount` (calculated at receipt creation) are not updated to match `totals.OutputVat`.

#### Proposed Correction
Inside [VatAwareReceiptService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/VatAwareReceiptService.vb#L83-L91), update the header total columns to match the sum of items:
```diff
             ' Step 3 — aggregate to transaction level and capture config snapshots.
             Dim totals = _vatCalculator.AggregateTransaction(transaction, config)
             transaction.VatableSales = totals.VatableSales
             transaction.VatExemptSales = totals.VatExemptSales
             transaction.ZeroRatedSales = totals.ZeroRatedSales
             transaction.VatRateSnapshot = config.VatRate
             transaction.IsVatRegisteredSnapshot = config.IsVatRegistered
+            transaction.VatAmount = totals.OutputVat
```
Additionally, after invoking the inner service to issue the receipt, update `receipt.VatAmount` to preserve consistency:
```diff
             ' Step 5 — delegate to the inner ReceiptService to produce the OfficialReceipt.
             Dim receipt = Await _inner.GenerateReceiptAsync(transactionId)
+            receipt.VatAmount = totals.OutputVat
```

---

### 9. Hardcoded Output VAT Label Rate (Low)

> [!NOTE]
> **Confusing layout values if configuration changes**  
> Hardcoding `"Output VAT (12%)"` inside the composer label string results in incorrect print descriptions if the system admin updates the VAT rate to a different value (e.g. 15%) in settings.

#### Root Cause
In [BirCompliantReceiptBodyComposer.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.POS/Services/ReceiptFormatting/BirCompliantReceiptBodyComposer.vb#L124):
```vb
lines.Add(DisclosureLine("Output VAT (12%):", Money(outputVat)))
```
* **Correction**: Extract the rate dynamically from the configuration or transaction snapshot.

#### Proposed Correction
```diff
                 Dim outputVat As Decimal = receipt.VatAmount
+                Dim rate = If(txn IsNot Nothing, txn.VatRateSnapshot, vatConfig.VatRate)
+                Dim rateDisplay = (rate * 100D).ToString("0") & "%"
 
                 lines.Add(DisclosureLine("VATable Sales:", Money(vatableSales)))
                 lines.Add(DisclosureLine("VAT-Exempt Sales:", Money(vatExemptSales)))
                 lines.Add(DisclosureLine("Zero-Rated Sales:", Money(zeroRatedSales)))
                 lines.Add(New String(" "c, DisclosureLabelWidth) & "─────────")
-                lines.Add(DisclosureLine("Output VAT (12%):", Money(outputVat)))
+                lines.Add(DisclosureLine($"Output VAT ({rateDisplay}):", Money(outputVat)))
```

---

### 10. Unused `Items` Column in `OfficialReceipt` (Low)

> [!NOTE]
> **Data redundancy and reprint mutation risk**  
> `OfficialReceipt.Items` is a column that is never populated. When printing, the app queries the transaction lines table. If a transaction or product is deleted/edited, reprinting the receipt will print modified values instead of the original historical transaction lines.

#### Root Cause
In `OfficialReceipt.vb`, the `Items` field exists but is never populated.
* **Correction**: Either serialize the lines to JSON when the receipt is created, or clean up the column to avoid schema bloat.
