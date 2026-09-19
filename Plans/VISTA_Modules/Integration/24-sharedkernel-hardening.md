---
module: MerchSys.Integration
plan-id: INT-24
title: "SharedKernel security & robustness hardening"
depends-on: [INT-18, INT-22]
estimated-files: 4
priority: medium
---

# INT-24: SharedKernel security & robustness hardening

## Context

Promotes the deferred SharedKernel hardening cluster (SK-1, SK-2, SK-3, SK-5, SK-6, plus the **safe** variant of SK-4) from the **INT-18** backlog into a dedicated batch. These are defensive / robustness improvements — verification (INT-18) confirmed **none is an active bug today** — so the theme is closing latent gaps without behavioural regressions.

Re-ratings carried from INT-18 (the original report over-rated these):

- **SK-1** was rated Critical; verified **Low (latent)** — synchronous `SaveChanges()` has zero call sites, and `AuditInterceptor` is unregistered dead code.
- **SK-2 / SK-3** were rated High; verified **Low–Med** — the fail-open path is unreachable with the current three-role set, and the reflection-based self-service check works today.

Depends on **INT-22**, which makes the `BaseDbContext` audit stamping session-aware; SK-1 extends that same code path.

## Prerequisites

- **INT-18** — remediation index (scope authority).
- **INT-22** — session-aware `BaseDbContext` stamping (SK-1 builds on it).

## Wiki References

- `LLM_Wiki/wiki/concepts/owasp-da-top10.md` — A01 least privilege / A05 fail-safe defaults.
- `CLAUDE.md` — optimistic concurrency tokens are required on `Inv_StockBatches.QuantityRemaining`, `Pur_AccountsPayable.OutstandingBalance`, `Pos_CreditAccounts.OutstandingBalance`.

## Deliverables

```
MerchSys.SharedKernel/Data/BaseDbContext.vb            ' Modified — sync SaveChanges parity (SK-1) + RowVersion convention warning (SK-4 safe)
MerchSys.SharedKernel/Data/RoleGuardInterceptor.vb     ' Modified — fail-closed default (SK-2) + typed self-service (SK-3)
MerchSys.SharedKernel/Paging/PageRequest.vb            ' Modified — PageSize clamp (SK-5)
MerchSys.SharedKernel/Entities/UserAccount.vb          ' Modified — implement IAuditable (SK-6, optional)
```

## Specification

### SK-1 — synchronous `SaveChanges` parity

After INT-22's session-aware stamping is in place, extract the audit + soft-delete loop from `SaveChangesAsync` into a private `ApplyAuditAndSoftDelete()` and call it from **both** entry points:

```vb
Private Sub ApplyAuditAndSoftDelete()
    ' (the session-aware stamping + hard-delete→soft-delete conversion from INT-22)
End Sub

Public Overrides Function SaveChanges() As Integer
    ApplyAuditAndSoftDelete()
    Return MyBase.SaveChanges()
End Function

Public Overrides Async Function SaveChangesAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of Integer)
    ApplyAuditAndSoftDelete()
    Return Await MyBase.SaveChangesAsync(cancellationToken)
End Function
```

`RoleGuardInterceptor` already overrides both `SavingChanges` and `SavingChangesAsync`, so the role guard is unaffected. Do **not** revive the dead `AuditInterceptor`.

### SK-2 — fail-closed role default (`RoleGuardInterceptor.CheckRole`)

Convert the trailing `If _session.CurrentRole = UserRole.Owner Then … End If` (which falls through to *allow* for any other role) into a default-deny. Keep the early allows intact (System context, unauthenticated, Manager, Developer); then apply the write restriction to **every** remaining role:

```vb
' (after the Manager/Developer allow)
' Fail-closed: restrict writes for any other role (Owner, uninitialised, or future roles).
Dim modifiedEntries = context.ChangeTracker.Entries().
    Where(Function(e) e.State = EntityState.Added OrElse
                      e.State = EntityState.Modified OrElse
                      e.State = EntityState.Deleted).ToList()
If modifiedEntries.Count = 0 Then Return
' ... AuthSelfService exception (see SK-3) ...
' ... build distinctTables ...
Throw New UnauthorizedWriteException(_session.CurrentRole, distinctTables)
```

Throw with `_session.CurrentRole` (not a hardcoded `UserRole.Owner`).

### SK-3 — typed self-service check (same method)

Replace the reflection-based identity check with a compile-time-checked cast. Add `Imports MerchSys.SharedKernel.Entities` and:

```vb
If TypeOf singleEntry.Entity Is UserAccount Then
    Dim userAcc = DirectCast(singleEntry.Entity, UserAccount)
    If String.Equals(userAcc.Username, _writeContext.SelfServiceUsername, StringComparison.OrdinalIgnoreCase) Then
        Return   ' allowed self-service password change
    End If
End If
```

(`UserAccount` is in the same assembly, so no new dependency. This also survives EF dynamic proxies, which the `entityType.Name = "UserAccount"` check would not.)

### SK-4 — RowVersion convention: warn, never throw

Do **not** implement the report's throw. Instead:

1. **Audit** every entity that inherits `ConcurrencyAwareEntity`. Confirm the concurrency token is configured for the three required rows (`Inv_StockBatches.QuantityRemaining`, `Pur_AccountsPayable.OutstandingBalance`, `Pos_CreditAccounts.OutstandingBalance`) and list any other subclass whose `RowVersion` is unconfigured. Record the list in the summary; configure the token where it is clearly intended and missing.
2. Change `IgnoreNonTokenRowVersionConvention` to **still ignore** the property (so startup never breaks) but **accumulate** the offending entity names and surface a **single startup warning** (via `ILogger`/`Debug.WriteLine`) listing them — never an exception.

If wiring a logger into a model-finalizing convention is awkward, collect the names into a `Shared` list the convention exposes and log them once from the schema-initialization path at startup. The acceptance bar is: a developer who forgets to configure a token gets a visible warning, and the app still starts.

### SK-5 — `PageRequest.PageSize` clamp

`PageSize` is documented as trusted/VM-set (`CartService:242`), so this is defensive only. Add a backing field with a clamping setter:

```vb
Private _pageSize As Integer = 100
Public Property PageSize As Integer
    Get
        Return _pageSize
    End Get
    Set(value As Integer)
        _pageSize = If(value <= 0, 100, If(value > 1000, 1000, value))
    End Set
End Property
```

### SK-6 — `UserAccount : IAuditable` (optional, lowest priority)

`UserAccount` declares `CreatedAt`/`ModifiedAt` but does not implement `IAuditable`. Implement the interface (add `CreatedBy`/`ModifiedBy`). **Note:** `UserAccount` is read/written via raw ADO.NET against `Sys_UserAccounts` and is **not** registered in any DbContext, so this only unifies the contract — the auth path is unaffected and will not auto-populate the new columns. Include only if it builds cleanly; otherwise record as skipped.

## Implementation Notes

- These are defensive changes — the priority is **no behavioural regression**. After SK-2, manually confirm the Owner self-service password-change path still succeeds (the `AuthSelfService` write context + typed `UserAccount` check).
- SK-4 must never throw at startup.
- Build per file (`dotnet build MerchSys.slnx`). Per `CLAUDE.md`, document non-trivial build errors in the summary and stop rather than deep-troubleshooting.

## Acceptance Criteria

1. `BaseDbContext` overrides **both** `SaveChanges` and `SaveChangesAsync` through a shared `ApplyAuditAndSoftDelete()` (session-aware stamping + soft-delete); `AuditInterceptor` is not revived.
2. `RoleGuardInterceptor` is default-deny: only System/unauthenticated/Manager/Developer (plus the typed `AuthSelfService` self-update) may write; no reflection is used; the exception reports `_session.CurrentRole`.
3. The Owner self-service password change still works end to end.
4. `IgnoreNonTokenRowVersionConvention` emits a startup warning for any unconfigured `ConcurrencyAwareEntity` token and never throws; the three required tokens are confirmed configured.
5. `PageRequest.PageSize` is clamped to `[1, 1000]` (invalid → 100).
6. (Optional) `UserAccount` implements `IAuditable`, or the skip is recorded.
7. `dotnet build MerchSys.slnx` — target 0 errors, 0 warnings (or documented per protocol).

## Output Requirements

Create a progress report at `Progress/VISTA_Modules/Integration/INT-24-summary.md` using `Progress/_template.md`. Record the `ConcurrencyAwareEntity` token audit (which entities carry tokens, which were warned), confirm the role-guard self-service path still works, and note the SK-6 decision.
