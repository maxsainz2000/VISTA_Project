# Code Audit Report: MerchSys.SharedKernel

This report presents a thorough analysis of the `MerchSys.SharedKernel` core library. Multiple issues, including critical silent audit/soft-delete bypasses, fail-open security checks, brittle reflection-based authorization logic, and silent model configuration failures have been identified.

## ✅ Verification Addendum — 2026-06-11

Verified against source under `WPF_Applications/MerchSys/src/MerchSys.SharedKernel/`. Most findings are real but **over-rated**; finding 4's fix is risky. **Where this addendum conflicts with a finding's severity or fix, the addendum governs.**

| # | Reported | Verified verdict & action |
|---|---|---|
| 1 Sync `SaveChanges` not overridden | Critical | **Low (latent).** True gap, but there are **zero call sites** for synchronous `.SaveChanges()` anywhere in the codebase, and `AuditInterceptor` is **unregistered dead code**. Optional defensive hardening (override sync `SaveChanges`), not an active data-loss risk. |
| 2 Fail-open role check | High | **Low practical / good hardening.** Structurally real (falls through to allow for any role that isn't Manager/Owner/Developer) but **unreachable** — only 3 roles exist (1/2/3), all handled, and unauthenticated returns early. Fail-closed (default-deny) is still the right change. |
| 3 Reflection-based self-service | High | **Low–Med.** Valid: `.Name="UserAccount"` + `GetProperty` is brittle (breaks under EF proxies). `UserAccount` is same-assembly, so `TypeOf ... Is UserAccount` is a strictly-better, low-risk refactor. Works correctly today. |
| 4 Silent RowVersion ignore | Medium | **Code reading correct; throw-at-startup is risky.** The silent ignore is deliberate — throwing would break startup for any `ConcurrencyAwareEntity` subclass that intentionally maps no token. Treat as a design decision (which entities must carry a token); prefer a startup *warning* over an exception. |
| 5 PageRequest bounds | Medium | **Low.** `PageSize` is documented trusted/VM-set, not user input (`CartService:242`). The clamp is harmless hardening. |
| 6 UserAccount not IAuditable | Low | **Speculative.** `UserAccount` is a plain ADO.NET DTO never registered in a DbContext; the "if ever registered" risk is hypothetical. Optional. |

**Missed by this report:** the real concurrency surface is the DI model — `Scoped` services and DbContexts are resolved from the **root** provider (`MainWindowViewModel:220`), so each module effectively runs on **one app-wide DbContext**. That, not the per-service class fields, is the structural smell worth a plan.

*Verified by Claude (Opus 4.8) on 2026-06-11. Original findings retained below for traceability.*

---

## Findings Summary Table

| Severity | Category | Component | Description |
| :--- | :--- | :--- | :--- |
| **Critical** | Data Integrity & Auditing | [BaseDbContext.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Data/BaseDbContext.vb), [AuditInterceptor.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Data/AuditInterceptor.vb) | Synchronous `SaveChanges()` is not overridden, allowing updates to silently bypass all soft-delete, logical interception, and auditing operations. |
| **High** | Security Vulnerability | [RoleGuardInterceptor.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Data/RoleGuardInterceptor.vb) | Fail-open role checking logic allows write operations by default for any unrecognized, uninitialized, or future user roles. |
| **High** | MVVM / Architecture Smell | [RoleGuardInterceptor.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Data/RoleGuardInterceptor.vb) | Brittle reflection-based type and property checking for self-service updates that fails if EF Core dynamic proxies are used and breaks refactoring. |
| **Medium** | Design & Reliability | [BaseDbContext.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Data/BaseDbContext.vb) | Model-finalizing convention silently ignores and removes `RowVersion` properties that are not configured, masking concurrency omissions. |
| **Medium** | Stability & Boundary Check | [PageRequest.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Paging/PageRequest.vb) | `PageSize` lacks input validation and boundary checks, permitting negative values or excessively large loads that cause runtime exceptions or resource exhaustion. |
| **Low** | Design Consistency | [UserAccount.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Entities/UserAccount.vb) | Inconsistent auditing hierarchy where user accounts declare custom date fields but bypass the standard `IAuditable` contract. |

---

## Detailed Findings & Proposed Diffs

### 1. Critical Soft-Delete and Auditing Bypass on Synchronous SaveChanges (Critical)

> [!WARNING]
> **Severe Risk of permanent data loss (hard-delete bypass) and audit log failures**  
> `BaseDbContext` only overrides `SaveChangesAsync`. If any developer, background service, or utility calls synchronous `SaveChanges()`, the audit columns (`CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`) will remain unpopulated, and soft-deletes (`ISoftDeletable`) will execute as actual SQL hard deletes. The same gap exists in `AuditInterceptor` (an alternative auditing provider).

#### Root Cause
In [BaseDbContext.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Data/BaseDbContext.vb#L73-L116):
Only `SaveChangesAsync` is overridden. Synchronous `SaveChanges` is left to its default behavior, completely ignoring intercepting logic:
```vb
        Public Overrides Async Function SaveChangesAsync(
            Optional cancellationToken As CancellationToken = Nothing) As Task(Of Integer)
            ' Audit and soft-delete logic...
            Return Await MyBase.SaveChangesAsync(cancellationToken)
        End Function
```
In [AuditInterceptor.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Data/AuditInterceptor.vb#L22-L32):
The class overrides `SavingChangesAsync` but fails to override `SavingChanges`.

#### Proposed Correction

Refactor `BaseDbContext` to centralize the audit and soft-delete operations, and override both methods:

```diff
     Public MustInherit Class BaseDbContext
         Inherits DbContext
 
         Friend Const DefaultUser As String = "Manager"
 
         Protected Sub New(options As DbContextOptions)
             MyBase.New(options)
         End Sub
+
+        Private Sub ApplyAuditAndSoftDelete()
+            Dim now = DateTime.UtcNow
+            For Each dbEntry In ChangeTracker.Entries().ToList()
+                Select Case dbEntry.State
+                    Case EntityState.Added
+                        If TypeOf dbEntry.Entity Is IAuditable Then
+                            Dim auditable = DirectCast(dbEntry.Entity, IAuditable)
+                            auditable.CreatedAt = now
+                            auditable.CreatedBy = DefaultUser
+                            auditable.ModifiedAt = now
+                            auditable.ModifiedBy = DefaultUser
+                        End If
+                    Case EntityState.Modified
+                        If TypeOf dbEntry.Entity Is IAuditable Then
+                            Dim auditable = DirectCast(dbEntry.Entity, IAuditable)
+                            auditable.ModifiedAt = now
+                            auditable.ModifiedBy = DefaultUser
+                        End If
+                    Case EntityState.Deleted
+                        If TypeOf dbEntry.Entity Is ISoftDeletable Then
+                            dbEntry.State = EntityState.Modified
+                            Dim softDel = DirectCast(dbEntry.Entity, ISoftDeletable)
+                            softDel.IsDeleted = True
+                            softDel.DeletedAt = now
+                            softDel.DeletedBy = DefaultUser
+                            If TypeOf dbEntry.Entity Is IAuditable Then
+                                Dim auditable = DirectCast(dbEntry.Entity, IAuditable)
+                                auditable.ModifiedAt = now
+                                auditable.ModifiedBy = DefaultUser
+                            End If
+                        End If
+                End Select
+            Next
+        End Sub
+
+        Public Overrides Function SaveChanges() As Integer
+            ApplyAuditAndSoftDelete()
+            Return MyBase.SaveChanges()
+        End Function
 
         Public Overrides Async Function SaveChangesAsync(
             Optional cancellationToken As CancellationToken = Nothing) As Task(Of Integer)
-
-            Dim now = DateTime.UtcNow
-
-            For Each dbEntry In ChangeTracker.Entries().ToList()
-                Select Case dbEntry.State
-
-                    Case EntityState.Added
-                        If TypeOf dbEntry.Entity Is IAuditable Then
-                            Dim auditable = DirectCast(dbEntry.Entity, IAuditable)
-                            auditable.CreatedAt = now
-                            auditable.CreatedBy = DefaultUser
-                            auditable.ModifiedAt = now
-                            auditable.ModifiedBy = DefaultUser
-                        End If
-
-                    Case EntityState.Modified
-                        If TypeOf dbEntry.Entity Is IAuditable Then
-                            Dim auditable = DirectCast(dbEntry.Entity, IAuditable)
-                            auditable.ModifiedAt = now
-                            auditable.ModifiedBy = DefaultUser
-                        End If
-
-                    Case EntityState.Deleted
-                        If TypeOf dbEntry.Entity Is ISoftDeletable Then
-                            ' Intercept the hard delete and convert to a logical delete.
-                            dbEntry.State = EntityState.Modified
-                            Dim softDel = DirectCast(dbEntry.Entity, ISoftDeletable)
-                            softDel.IsDeleted = True
-                            softDel.DeletedAt = now
-                            softDel.DeletedBy = DefaultUser
-                            If TypeOf dbEntry.Entity Is IAuditable Then
-                                Dim auditable = DirectCast(dbEntry.Entity, IAuditable)
-                                auditable.ModifiedAt = now
-                                auditable.ModifiedBy = DefaultUser
-                            End If
-                        End If
-
-                End Select
-            Next
-
+            ApplyAuditAndSoftDelete()
             Return Await MyBase.SaveChangesAsync(cancellationToken)
         End Function
```

*(Also, apply the corresponding synchronous `SavingChanges` override in `AuditInterceptor.vb` to execute `ApplyAudit`.)*

---

### 2. Fail-Open Role Checking Logic in Role Guard Interceptor (High)

> [!CAUTION]
> **Violation of Least Privilege and Fail-Safe Defaults (OWASP A01/A05)**  
> The role guard interceptor implements authorization by checking for explicitly restricted roles (`Owner`) and explicitly allowed roles (`Manager`, `Developer`). However, if a user has an unrecognized, unmapped, uninitialized (e.g., `0`), or future role (e.g., `Cashier`), the interceptor falls through and permits all write operations (Added, Modified, Deleted).

#### Root Cause
In [RoleGuardInterceptor.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Data/RoleGuardInterceptor.vb#L60-L113):
```vb
            ' 3. If the user is a Manager (or Developer, a Manager superset), they have full CRUD access. Allow.
            If _session.CurrentRole = UserRole.Manager OrElse _session.CurrentRole = UserRole.Developer Then
                Return
            End If

            ' 4. If the user is an Owner, restrict writes.
            If _session.CurrentRole = UserRole.Owner Then
                ' Restrict writes ...
            End If
            
            ' Fall-through: allowed by default!
```

#### Proposed Correction
Convert the logic to a fail-closed model (default-deny). Explicitly check for allowed roles (`Manager` and `Developer`), and apply write restrictions to all other roles:

```diff
             ' 3. If the user is a Manager (or Developer, a Manager superset), they have full CRUD access. Allow.
             If _session.CurrentRole = UserRole.Manager OrElse _session.CurrentRole = UserRole.Developer Then
                 Return
             End If
 
-            ' 4. If the user is an Owner, restrict writes.
-            If _session.CurrentRole = UserRole.Owner Then
-                Dim modifiedEntries = context.ChangeTracker.Entries().
-                    Where(Function(e) e.State = EntityState.Added OrElse
-                                      e.State = EntityState.Modified OrElse
-                                      e.State = EntityState.Deleted).
-                    ToList()
-
-                ' No actual changes to persist, allow.
-                If modifiedEntries.Count = 0 Then
-                    Return
-                End If
-
-                ' AuthSelfService special case: Owner updating their own credentials.
-                If _writeContext.Current = WriteContextKind.AuthSelfService Then
-                    ' Exactly one entry of type UserAccount must be tracked.
-                    If modifiedEntries.Count = 1 Then
-                        Dim singleEntry = modifiedEntries(0)
-                        Dim entityType = singleEntry.Entity.GetType()
-                        Dim isUserAccount = entityType.Name = "UserAccount" OrElse entityType.FullName.EndsWith(".UserAccount")
-
-                        If isUserAccount Then
-                            ' The UserAccount being modified must match the asserted SelfServiceUsername.
-                            Dim usernameProp = entityType.GetProperty("Username")
-                            If usernameProp IsNot Nothing Then
-                                Dim usernameValue = CStr(usernameProp.GetValue(singleEntry.Entity))
-                                If String.Equals(usernameValue, _writeContext.SelfServiceUsername, StringComparison.OrdinalIgnoreCase) Then
-                                    ' Allowed.
-                                    Return
-                                End If
-                            End If
-                        End If
-                    End If
-                End If
-
-                ' Extract distinct table names to report in the exception.
-                Dim distinctTables As New List(Of String)()
-                For Each entry In modifiedEntries
-                    Dim tableName = entry.Metadata.GetTableName()
-                    If String.IsNullOrWhiteSpace(tableName) Then
-                        tableName = entry.Entity.GetType().Name
-                    End If
-                    If Not distinctTables.Contains(tableName) Then
-                        distinctTables.Add(tableName)
-                    End If
-                Next
-
-                Throw New UnauthorizedWriteException(UserRole.Owner, distinctTables)
-            End If
+            ' 4. Fail-closed: Restrict writes for any other role (Owner, uninitialized, or future roles)
+            Dim modifiedEntries = context.ChangeTracker.Entries().
+                Where(Function(e) e.State = EntityState.Added OrElse
+                                  e.State = EntityState.Modified OrElse
+                                  e.State = EntityState.Deleted).
+                ToList()
+
+            ' No actual changes to persist, allow.
+            If modifiedEntries.Count = 0 Then
+                Return
+            End If
+
+            ' AuthSelfService special case: User updating their own credentials.
+            If _writeContext.Current = WriteContextKind.AuthSelfService Then
+                If modifiedEntries.Count = 1 Then
+                    Dim singleEntry = modifiedEntries(0)
+                    If TypeOf singleEntry.Entity Is UserAccount Then
+                        Dim userAcc = DirectCast(singleEntry.Entity, UserAccount)
+                        If String.Equals(userAcc.Username, _writeContext.SelfServiceUsername, StringComparison.OrdinalIgnoreCase) Then
+                            ' Allowed.
+                            Return
+                        End If
+                    End If
+                End If
+            End If
+
+            ' Extract distinct table names to report in the exception.
+            Dim distinctTables As New List(Of String)()
+            For Each entry In modifiedEntries
+                Dim tableName = entry.Metadata.GetTableName()
+                If String.IsNullOrWhiteSpace(tableName) Then
+                    tableName = entry.Entity.GetType().Name
+                End If
+                If Not distinctTables.Contains(tableName) Then
+                    distinctTables.Add(tableName)
+                End If
+            Next
+
+            Throw New UnauthorizedWriteException(_session.CurrentRole, distinctTables)
```

---

### 3. Brittle Reflection-Based Self-Service Type Resolution (High)

> [!CAUTION]
> **Robustness, Performance, and Refactoring Vulnerability**  
> The self-service password update exception in `RoleGuardInterceptor` resolves the modified entity class type and `Username` property using reflection string matches (`entityType.Name = "UserAccount"` and `.GetProperty("Username")`). This reflection bypass is fragile, breaks when developers rename fields (compile-time safety is bypassed), fails completely if EF Core dynamic proxies (lazy-loading proxies) are enabled, and degrades performance.

#### Root Cause
In [RoleGuardInterceptor.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Data/RoleGuardInterceptor.vb#L83-L96):
```vb
                        Dim entityType = singleEntry.Entity.GetType()
                        Dim isUserAccount = entityType.Name = "UserAccount" OrElse entityType.FullName.EndsWith(".UserAccount")

                        If isUserAccount Then
                            ' The UserAccount being modified must match the asserted SelfServiceUsername.
                            Dim usernameProp = entityType.GetProperty("Username")
                            If usernameProp IsNot Nothing Then
                                Dim usernameValue = CStr(usernameProp.GetValue(singleEntry.Entity))
```
Since `UserAccount` is declared in the same project (`MerchSys.SharedKernel.Entities`), there is no structural block or circular dependency preventing direct, compile-time type checks.

#### Proposed Correction
Import `MerchSys.SharedKernel.Entities` in `RoleGuardInterceptor.vb` and perform compile-time checked type casting:

```diff
 Imports Microsoft.EntityFrameworkCore.Diagnostics
 Imports MerchSys.SharedKernel.Enums
 Imports MerchSys.SharedKernel.Interfaces
 Imports MerchSys.SharedKernel.Exceptions
+Imports MerchSys.SharedKernel.Entities
```
```diff
-                        Dim entityType = singleEntry.Entity.GetType()
-                        Dim isUserAccount = entityType.Name = "UserAccount" OrElse entityType.FullName.EndsWith(".UserAccount")
-
-                        If isUserAccount Then
-                            ' The UserAccount being modified must match the asserted SelfServiceUsername.
-                            Dim usernameProp = entityType.GetProperty("Username")
-                            If usernameProp IsNot Nothing Then
-                                Dim usernameValue = CStr(usernameProp.GetValue(singleEntry.Entity))
-                                If String.Equals(usernameValue, _writeContext.SelfServiceUsername, StringComparison.OrdinalIgnoreCase) Then
-                                    ' Allowed.
-                                    Return
-                                End If
-                            End If
-                        End If
+                        If TypeOf singleEntry.Entity Is UserAccount Then
+                            Dim userAcc = DirectCast(singleEntry.Entity, UserAccount)
+                            If String.Equals(userAcc.Username, _writeContext.SelfServiceUsername, StringComparison.OrdinalIgnoreCase) Then
+                                ' Allowed.
+                                Return
+                            End If
+                        End If
```

---

### 4. Silent Concurrency Token Configuration Bypass (Medium)

> 🟠 **VERIFICATION (2026-06-11) — code reading correct, but the throw-at-startup fix is risky.** Throwing would break startup for any `ConcurrencyAwareEntity` subclass that intentionally maps no token. Confirm which entities *must* carry a token (a design decision) before implementing, or emit a startup warning instead of an exception. See the Verification Addendum at the top.

> [!WARNING]
> **Risk of silent concurrency protection omission and lost updates**  
> The `IgnoreNonTokenRowVersionConvention` is registered to ignore the `RowVersion` property on any entity that inherits from `ConcurrencyAwareEntity` if the developer forgot to configure `.IsRowVersion()` or `.IsConcurrencyToken()` in the entity configurations. This silences configuration errors and allows writes to execute without optimistic concurrency validation, risking lost updates without warnings.

#### Root Cause
In [BaseDbContext.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Data/BaseDbContext.vb#L122-L135):
```vb
        Private NotInheritable Class IgnoreNonTokenRowVersionConvention
            Implements IModelFinalizingConvention

            Public Sub ProcessModelFinalizing(modelBuilder As IConventionModelBuilder,
                                              context As IConventionContext(Of IConventionModelBuilder)) _
                                              Implements IModelFinalizingConvention.ProcessModelFinalizing
                For Each et In modelBuilder.Metadata.GetEntityTypes()
                    Dim rv = et.FindProperty("RowVersion")
                    If rv IsNot Nothing AndAlso Not rv.IsConcurrencyToken() Then
                        et.Builder.Ignore("RowVersion", fromDataAnnotation:=False)
                    End If
                Next
            End Sub
        End Class
```

#### Proposed Correction
If an entity class inherits from `ConcurrencyAwareEntity`, it is explicitly designed to have concurrency validation. Instead of silently ignoring it, the convention should throw a detailed startup validation exception to warn the developer, or print a warning:

```diff
                 For Each et In modelBuilder.Metadata.GetEntityTypes()
                     Dim rv = et.FindProperty("RowVersion")
                     If rv IsNot Nothing AndAlso Not rv.IsConcurrencyToken() Then
-                        et.Builder.Ignore("RowVersion", fromDataAnnotation:=False)
+                        Throw New InvalidOperationException($"Entity '{et.ClrType.Name}' inherits from ConcurrencyAwareEntity but 'RowVersion' was not explicitly configured as a concurrency token. Register it using builder.Property(Function(e) e.RowVersion).IsRowVersion() to prevent silent concurrency check bypass.")
                     End If
                 Next
```

---

### 5. Keyset PageRequest Size Parameter Lack of Boundaries (Medium)

> [!WARNING]
> **Risk of Database Starvation, Exception Errors, or Memory Exhaustion**  
> `PageRequest` exposes `PageSize` as an auto-implemented property without limits or bounds check in the setter. A client or developer could inadvertently pass `PageSize = 0` or a negative value, triggering database exceptions, or set an excessively high value (e.g. `1,000,000`), pulling entire massive datasets into memory at once.

#### Root Cause
In [PageRequest.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Paging/PageRequest.vb#L19):
```vb
        Public Property PageSize As Integer = 100
```

#### Proposed Correction
Refactor `PageSize` with a backing field and validation boundaries (e.g. clamp values to a safe range of 1 to 1000, falling back to 100 on invalid entries):

```diff
-        Public Property PageSize As Integer = 100
+        Private _pageSize As Integer = 100
+
+        Public Property PageSize As Integer
+            Get
+                Return _pageSize
+            End Get
+            Set(value As Integer)
+                If value <= 0 Then
+                    _pageSize = 100
+                ElseIf value > 1000 Then
+                    _pageSize = 1000
+                Else
+                    _pageSize = value
+                End If
+            End Set
+        End Property
```

---

### 6. UserAccount Audit Field Inconsistency (Low)

> [!NOTE]
> **Inconsistent Auditing Contract**  
> `UserAccount.vb` declares standard audit timestamps `CreatedAt` and `ModifiedAt` directly. However, it does not implement `IAuditable` or inherit from `AuditableEntity`. 

#### Root Cause
In [UserAccount.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Entities/UserAccount.vb#L5-L21):
```vb
    Public Class UserAccount
        ' Custom properties instead of inheriting BaseEntity and implementing IAuditable
        Public Property Id As Integer
        ...
        Public Property CreatedAt As DateTime
        Public Property ModifiedAt As DateTime?
    End Class
```
If `UserAccount` is ever registered inside a DbContext in the future, its timestamps will not be automatically populated by `BaseDbContext.SaveChangesAsync` because the type check `TypeOf dbEntry.Entity Is IAuditable` will evaluate to false.

#### Proposed Correction
Have `UserAccount` implement the standard `IAuditable` contract, or inherit from `AuditableEntity` (or a customized base class), ensuring clean and unified architectural structure.
