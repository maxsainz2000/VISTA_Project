---
module: MerchSys.Infrastructure
plan-id: INFRA-20
title: "DA5 Data-Layer Write Rejection for Owner Role"
depends-on: [INFRA-15, INFRA-16]
estimated-files: 9
priority: high
---

# DA5 Data-Layer Write Rejection for Owner Role

## Context

OWASP DA5 (Improper Authorization) requires role-based write enforcement at the **data-access layer**, not only at the UI. INFRA-16 implemented UI-layer enforcement (navigation filtering + `CanEdit`/`IsManager` properties), but the repository and service layer does not yet reject writes from Owner sessions. This is documented as deferred in `Plans/Future/deferred-features-backlog.md` item #7 and called out at the end of `Progress/VISTA_Modules/Infrastructure/INFRA-16-summary.md` ("DA5 Compliance Note").

The single-user-kiosk operating model means a malicious Owner is unlikely in production, but defense in depth is the security model the project committed to: a misconfigured UI binding, a developer shortcut that calls a service from a wrong context, or a future feature that forgets the UI check should all still be rejected by the data layer. Closing this gap is the second OWASP-DA gap to fall after INFRA-19 closed DA2.

## Prerequisites

- **INFRA-15** (Login Form & User Authentication) — completed; provides `LoginSessionService.CurrentRole`
- **INFRA-16** (Owner Dashboard + UI Enforcement) — completed; established the `CanEdit`/`IsManager` UI pattern

## Wiki References

- `concepts/owasp-da-top10.md` — DA5 (Improper Authorization)
- `Sources/system_plan.md` — Section 7 (User Roles), Section 8 (Security Requirements)

## Deliverables

```
MerchSys.SharedKernel/Exceptions/
└── UnauthorizedWriteException.vb                ' New — explicit exception type

MerchSys.SharedKernel/Interfaces/
└── IWriteContextScope.vb                        ' New — user vs system write-context flag

MerchSys.SharedKernel/Data/
├── WriteContextScope.vb                         ' New — default scoped implementation
└── RoleGuardInterceptor.vb                      ' New — SaveChangesInterceptor that enforces DA5

MerchSys.App/Startup/
└── DatabaseConfig.vb                            ' Modified — register interceptor on all 4 module DbContexts

MerchSys.App/Services/
├── AuthenticationService.vb                     ' Modified — explicit role guard on raw-SQL write methods
└── (any other raw-SQL writer surfaced by the audit)

MerchSys.App/Application.xaml.vb                 ' Modified — DI registration of IWriteContextScope + RoleGuardInterceptor

Plans/VISTA_Modules/Infrastructure/
└── 20-da5-write-path-audit.md                   ' New — companion audit deliverable (see Phase A below)
```

`estimated-files: 9` assumes the audit surfaces 0–1 additional raw-SQL write site to modify. If the audit finds more, the file count increases accordingly and the summary should note it.

## Specification

### Phase A — Write-Path Audit (do this first)

Before any code changes, produce a write-path audit at `Plans/VISTA_Modules/Infrastructure/20-da5-write-path-audit.md` that catalogues every write site in the solution and classifies it. Use the existing `codebase-audit` skill if helpful, or grep manually for the patterns listed below. Classify each write site as one of:

| Class | Definition | DA5 treatment |
|---|---|---|
| **U** (user) | Originates from a user-initiated UI action — a ViewModel `[RelayCommand]`, a click handler, etc. | Must be rejected for Owner role |
| **S** (system) | Originates from a background service, scheduled timer, or a MediatR event handler responding to a U write | Must be allowed regardless of role |
| **A** (auth) | Owner's own credential management (password change for the Owner's account) | Must be allowed for the Owner only when the target row is the Owner's own row |

Patterns to grep:
- `SaveChangesAsync` — every EF Core write
- `SaveChangesWithJournalAsync` — module syncable repositories
- `ExecuteNonQueryAsync` / `command.ExecuteNonQuery` — raw-SQL writes
- `INSERT INTO` / `UPDATE ` / `DELETE FROM` (case-insensitive) inside `.vb` files — embedded SQL

Audit file format (one row per write site):

```markdown
| File:Line | Class | Justification |
|---|---|---|
| MerchSys.POS/ViewModels/SalesCartViewModel.vb:142 | U | Cart checkout — user click |
| MerchSys.Accounting/Handlers/SaleCompletedAccountingHandler.vb:55 | S | MediatR handler, system-triggered |
| MerchSys.App/Services/AuthenticationService.vb:210 | A | Owner changes own password |
| MerchSys.App/Services/SyncOrchestrator.vb:88 | S | Background sync flush |
| … | … | … |
```

The audit's output drives Phase B. **No code changes are made in Phase A.**

### Phase B — IWriteContextScope

```vb
Namespace Interfaces

    Public Enum WriteContextKind
        ''' <summary>Default. User-initiated UI write. Subject to role enforcement.</summary>
        User = 0
        ''' <summary>System-initiated (background service, MediatR handler). Bypasses role enforcement.</summary>
        System = 1
        ''' <summary>Auth-self-service (Owner changes own password). Caller carries Username assertion.</summary>
        AuthSelfService = 2
    End Enum

    Public Interface IWriteContextScope
        ''' <summary>The kind in force for the current async-flow scope.</summary>
        ReadOnly Property Current As WriteContextKind

        ''' <summary>
        ''' For <see cref="WriteContextKind.AuthSelfService"/> only — the username asserted by the
        ''' caller. The interceptor uses this to verify the caller owns the row being written.
        ''' </summary>
        ReadOnly Property SelfServiceUsername As String

        ''' <summary>
        ''' Enter a nested scope of the given kind. Disposing the returned scope restores the
        ''' previous kind. Implementations must be safe across async boundaries — use
        ''' <c>AsyncLocal(Of T)</c>, not thread-local.
        ''' </summary>
        Function Enter(kind As WriteContextKind, Optional selfServiceUsername As String = Nothing) As IDisposable
    End Interface

End Namespace
```

Default implementation lives in `MerchSys.SharedKernel/Data/WriteContextScope.vb`. The current kind is stored in an `AsyncLocal(Of Frame)` so it survives `Await` boundaries. The default frame is `WriteContextKind.User` — i.e., absence of an explicit `Enter(...)` means the caller is presumed user-initiated.

DI registration: **scoped** lifetime (per-DI-scope frame) is wrong here because background services typically own their own scope. Register as **singleton**; the `AsyncLocal` carries the per-flow state.

### Phase C — UnauthorizedWriteException

```vb
Namespace Exceptions

    Public Class UnauthorizedWriteException
        Inherits UnauthorizedAccessException

        Public Sub New(role As UserRole, tableNames As IReadOnlyList(Of String))
            MyBase.New($"Role '{role}' is not permitted to write to: {String.Join(", ", tableNames)}.")
            Me.Role = role
            Me.TableNames = tableNames
        End Sub

        Public ReadOnly Property Role As UserRole
        Public ReadOnly Property TableNames As IReadOnlyList(Of String)
    End Class

End Namespace
```

### Phase D — RoleGuardInterceptor

Lives in `MerchSys.SharedKernel/Data/RoleGuardInterceptor.vb`. Inherits `SaveChangesInterceptor`. Constructor injects `ISessionService` and `IWriteContextScope`.

**Behavior on `SavingChangesAsync`:**

1. If `_writeContext.Current = WriteContextKind.System` → return immediately (system writes are always allowed).
2. If `_session.CurrentRole = UserRole.Manager` → return immediately.
3. If `_session.CurrentRole = UserRole.Owner`:
   - Collect every tracked entry whose `State` is `Added`, `Modified`, or `Deleted`.
   - **AuthSelfService special-case:** if `_writeContext.Current = WriteContextKind.AuthSelfService`, allow exactly **one** entry of type `UserAccount` where `Username = _writeContext.SelfServiceUsername`. Reject if there are any other tracked entries.
   - Otherwise, throw `UnauthorizedWriteException(UserRole.Owner, distinctTableNames)`.
4. If the session is unauthenticated (no role set, e.g. during `LoginView` lifecycle) → **allow** — the only writes during that window are the password-change row for the user logging in, and login itself does not write. `LoginSessionService.IsAuthenticated = False` is the signal.

Implementation note: the interceptor must use the **synchronous** `SavingChanges` override as well as `SavingChangesAsync` to cover both code paths — EF Core does not unify them.

### Phase E — Wire interceptor into every module DbContext

In `MerchSys.App/Startup/DatabaseConfig.vb` (the existing `AddModuleDbContexts`), modify each module's `AddDbContext(Of T)` call to register the interceptor:

```vb
services.AddDbContext(Of PurchasingDbContext)(
    Sub(sp, options)
        options.UseSqlite(connectionString)
        options.AddInterceptors(sp.GetRequiredService(Of RoleGuardInterceptor)())
    End Sub)
' …same for Inventory, POS, Accounting…
```

Register `RoleGuardInterceptor` as **scoped** so it can resolve scoped `ISessionService` (the session is a singleton, so scoped is safe; but if `IWriteContextScope` is singleton with internal `AsyncLocal`, scoped resolution is fine too). Confirm during implementation: `RoleGuardInterceptor` should not be a singleton because EF Core's interceptor pipeline can hold references.

### Phase F — Wire system contexts

For each write site classified **S** in the audit, wrap the entry point in `Using _writeContext.Enter(WriteContextKind.System)`. The likely call sites:

- `MerchSys.App/Services/SyncOrchestrator.vb` — entire flush loop
- `MerchSys.Inventory/Services/LowStockAlertService.vb` — alert-row insert (verify with audit)
- Every Accounting handler in `MerchSys.Accounting/Handlers/*Handler.vb` — wrap the `Handle` body
- Any `IHostedService` discovered in the audit

For each write site classified **A** in the audit (`AuthenticationService.ChangePasswordAsync` is the prime suspect), wrap in `Using _writeContext.Enter(WriteContextKind.AuthSelfService, targetUsername)`.

The interceptor enforces the user-class default for everything else — no explicit `Enter(WriteContextKind.User)` is needed at U sites.

### Phase G — DI registration in Application.xaml.vb

```vb
' Infrastructure: Write context + role guard (DA5)
services.AddSingleton(Of IWriteContextScope, WriteContextScope)()
services.AddScoped(Of RoleGuardInterceptor)()
```

### Phase H — Operator checklist update

Replace the existing `⏳ Deferred` placeholder for DA5 in `Operator/INFRA-verification-checklist.md` (item INFRA-16 section) with a new test:

> ## INFRA-20 — DA5 Data-Layer Write Rejection (Owner Role)
>
> 1. Log in as Owner. Open the **Transaction History** view.
> 2. Right-click any transaction → in a DEBUG build, use the developer-tools "Force write" item (or open the existing `DebugServices` console and invoke a write against any module DbContext).
> 3. **Expected:** the write throws `UnauthorizedWriteException`; the UI surfaces a friendly error toast ("Owner accounts cannot modify data."). No row is inserted/modified/deleted in the underlying table.
> 4. Log in as Owner. In the Login flow, change your own password.
> 5. **Expected:** the password change succeeds (AuthSelfService path).
> 6. As Owner, attempt to change the Manager's password through any means.
> 7. **Expected:** the change is rejected.

## Implementation Notes

- **AsyncLocal is mandatory** in `WriteContextScope`. `ThreadLocal` and `ThreadStatic` will silently break across `Await` continuations on the WPF dispatcher.
- **Do not** put the role check inside `BaseDbContext.SaveChangesAsync` even though it would also work — interceptors are the EF Core-recommended pattern and the `AuditInterceptor` already exists as the alternative pathway. Keeping enforcement out of `BaseDbContext` keeps the responsibility separated.
- **Raw-SQL writes bypass the interceptor.** This is the whole reason Phase A audits for `ExecuteNonQueryAsync` and embedded SQL. Every raw-SQL writer must either (a) route through the EF context for that one operation, or (b) accept an explicit `ISessionService` and check the role itself before issuing the SQL. Document the decision per site in the audit and the summary.
- **MediatR handlers must wrap their `Handle` body** in `Using _writeContext.Enter(WriteContextKind.System)` (Phase F). Forgetting this causes the handler to throw under an Owner session even though no Owner UI initiated the handler — this would happen only if a Manager-initiated event handler runs while the role mid-flips, which is currently impossible (single-user kiosk) but documenting the rule keeps future authors safe.
- **Reserved-keyword traps:** `Enter` is fine as a method name on an interface (not a VB keyword). `kind` is fine. Watch for shadowing in the interceptor: `tableNames` parameter must not match a property in the exception.
- **`Await` in `Catch`/`Finally`:** the interceptor is synchronous decision logic; no `Await` inside the role check. If an audit-log write is later added to record the rejection, capture the rejection state before the `Catch` block, then await after — per the project trap rule.
- **`Console` namespace shadow:** if logging is added to the interceptor (recommended via `ILogger(Of RoleGuardInterceptor)`), do not fall back to `Console.WriteLine` — use the logger or `System.Console.WriteLine` explicitly.
- The interceptor must NOT block reads. EF Core's `SavingChangesAsync` only fires for writes, so this is implicit, but verify under a debug session that read-heavy Owner workflows (Owner Dashboard, Financial Overview) do not hit the interceptor at all.

## Acceptance Criteria

1. `dotnet build WPF_Applications/MerchSys/MerchSys.slnx` succeeds with 0 errors, 0 warnings.
2. The audit file `Plans/VISTA_Modules/Infrastructure/20-da5-write-path-audit.md` exists and classifies every write site as U / S / A.
3. As Manager, every existing operator checklist test passes unchanged — Manager writes are not affected by the interceptor.
4. As Owner, attempting any U-class write (any module) throws `UnauthorizedWriteException`. No row is persisted.
5. As Owner, changing one's own password succeeds (AuthSelfService path).
6. As Owner, attempting to change another user's password via any pathway is rejected.
7. MediatR handlers reacting to Manager-triggered events complete successfully — the system-context wrap is correctly applied at every S-class site.
8. The `SyncOrchestrator` flushes pending entries regardless of current session role — its system-context wrap is verified by allowing a sync run while an Owner is logged in.
9. Read paths (Owner Dashboard, Financial Overview, etc.) do not throw and do not log spurious interceptor invocations.
10. Raw-SQL write paths surfaced by the audit have explicit per-method role guards documented in the summary.
11. `Operator/INFRA-verification-checklist.md` no longer contains the `⏳ Deferred — requires follow-up plan to audit all module write paths` placeholder for DA5.

## Output Requirements

### Audit Deliverable
Create at: `Plans/VISTA_Modules/Infrastructure/20-da5-write-path-audit.md` per Phase A above. **This must be produced before any code changes** and committed alongside (or before) the implementation.

### Implementation Summary
Create at: `Progress/VISTA_Modules/Infrastructure/INFRA-20-summary.md` using `Progress/_template.md`. Document:
- Total write sites surveyed in the audit, broken down by class (U/S/A counts)
- Any write sites that resisted classification and the resolution
- Whether any raw-SQL writer required modification beyond `AuthenticationService`
- Confirmation that EF Core `SavingChangesAsync` and `SavingChanges` (synchronous) both invoke the interceptor

### Documentation
- XML doc on `IWriteContextScope` describing the three `WriteContextKind` values with a usage example for each
- XML doc on `RoleGuardInterceptor` citing OWASP DA5 and naming the chokepoint guarantee
- Header comment in `WriteContextScope.vb` noting why `AsyncLocal` (not `ThreadLocal`) is required

## Backlog Cleanup

On completion of INFRA-20:
1. Remove item **#7 DA5 Data-Layer Write Rejection for Owner Role** from `Plans/Future/deferred-features-backlog.md`.
2. Update the backlog file's frontmatter with `infra-20-completed: <YYYY-MM-DD>`.
3. Replace the `⏳ Deferred` placeholder for DA5 in `Operator/INFRA-verification-checklist.md` with the test described in Phase H.
4. Note in the summary whether item **#11** (CanEdit on Financial/Income/Sales ViewModels) became moot — it likely does, since the data layer now enforces the rule regardless of UI binding.
