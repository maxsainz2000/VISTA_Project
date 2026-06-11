# Code Audit Report: MerchSys.App

This report presents a thorough analysis of the `MerchSys.App` directory. Several issues, ranging from critical culture-sensitive crash bugs to resource leaks and performance Smells, have been identified.

## ✅ Verification Addendum — 2026-06-11

Verified against source under `WPF_Applications/MerchSys/src/MerchSys.App/`. One finding (2) is largely incorrect. **Where this addendum conflicts with a finding's severity or fix, the addendum governs.**

| # | Reported | Verified verdict & action |
|---|---|---|
| 1 Login datetime double-parse | Critical | **Low–Med.** The `CStr(...)→DateTime.Parse(...)` round-trip is real (`IAuthenticationService:211-217`) and the direct-cast fix is correct — but the "FormatException crash" rationale is wrong: `CStr` and `Parse` use the *same* current culture, so the round-trip succeeds; the real harm is sub-second/`Kind` precision loss. Apply the cast; downgrade severity. |
| 2 OwnerDashboard leak | High | **Reject as written.** `OwnerDashboardViewModel` already `Implements IDisposable` (Dispose stops the timer), `OwnerDashboardView` disposes it on `Unloaded`, and it uses a `DispatcherTimer`, not a background timer. The "leaks timers that query the DB" claim is false, and the proposed **Singleton fix would break** the dispose-on-Unloaded lifecycle. Only a minor truth remains (MS-DI tracks transient `IDisposable` resolved from the root provider) — the retained object is already disposed/inert. Severity → Low. |
| 3 CTS dispose no-op | Medium | **Confirmed.** `[Stop]()` nulls `_cts` before `Dispose()` runs, so the CTS is cancelled but never disposed. Fix is correct (dispose inside `[Stop]`). Real leak is small. |
| 4 Converter boxing | Low | **Confirmed** micro-optimization. Optional. |

**Missed by this report:** `AuthenticationService` also writes dates as `"yyyy-MM-dd HH:mm:ss"` strings into SQL params (lines 183/227/239/245) — MySQL-compatible so lower-risk than ISO `"o"`, but fold into the native-`DateTime` pass.

*Verified by Claude (Opus 4.8) on 2026-06-11. Original findings retained below for traceability.*

---

## Findings Summary Table

| Severity | Category | Component | Description |
| :--- | :--- | :--- | :--- |
| **Critical** | Stability & Bug | [IAuthenticationService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Services/IAuthenticationService.vb) | Culture-dependent double-conversion datetime parsing during login checks. |
| **High** | Memory & Resource Leak | [Application.xaml.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Application.xaml.vb) | Transient `IDisposable` leak of `OwnerDashboardViewModel` and `OwnerDashboardView` in the root DI container. |
| **Medium** | Resource Leak | [ConnectionHealthMonitor.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Services/ConnectionHealthMonitor.vb) | Un-disposed `CancellationTokenSource` leak during service shutdown. |
| **Low** | Performance Smell | [FormattingConverters.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Converters/FormattingConverters.vb) | Unnecessary boxing, string conversion, and parsing in value converters. |

---

## Detailed Findings & Proposed Diffs

### 1. Culture-Dependent Datetime Parsing during Login (Critical)

> [!WARNING]
> **Risk of Application Crashes in Production**  
> If this application is run on a workstation where the Windows date/time culture settings differ from the database format (e.g., `dd/MM/yyyy` on the client vs `yyyy-MM-dd` in the database), the login process will crash with a `FormatException`.

#### Root Cause
In [IAuthenticationService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Services/IAuthenticationService.vb#L211-L217):
```vb
u.LockedUntil = If(IsDBNull(rdr("LockedUntil")), Nothing, CType(DateTime.Parse(CStr(rdr("LockedUntil"))), DateTime?))
u.LastPasswordChangeAt = If(IsDBNull(rdr("LastPasswordChangeAt")), Nothing, CType(DateTime.Parse(CStr(rdr("LastPasswordChangeAt"))), DateTime?))
u.CreatedAt = DateTime.Parse(CStr(rdr("CreatedAt")))
u.ModifiedAt = If(IsDBNull(rdr("ModifiedAt")), Nothing, CType(DateTime.Parse(CStr(rdr("ModifiedAt"))), DateTime?))
```
* **Double Conversion**: Date columns (`DATETIME(6)`) are fetched from the database, converted to strings using VB's `CStr()` (which uses the active system thread culture), and then parsed back into a `DateTime` using `DateTime.Parse()`.
* **Vulnerability**: This string parsing is highly brittle and relies entirely on local user region settings.
* **Correction**: Since the underlying ADO.NET driver (`MySqlConnector`) natively returns date columns as .NET `System.DateTime` instances, they should be cast directly without any string conversions.

#### Proposed Correction
```diff
-u.LockedUntil = If(IsDBNull(rdr("LockedUntil")), Nothing, CType(DateTime.Parse(CStr(rdr("LockedUntil"))), DateTime?))
-u.LastPasswordChangeAt = If(IsDBNull(rdr("LastPasswordChangeAt")), Nothing, CType(DateTime.Parse(CStr(rdr("LastPasswordChangeAt"))), DateTime?))
-u.CreatedAt = DateTime.Parse(CStr(rdr("CreatedAt")))
-u.ModifiedAt = If(IsDBNull(rdr("ModifiedAt")), Nothing, CType(DateTime.Parse(CStr(rdr("ModifiedAt"))), DateTime?))
+u.LockedUntil = If(IsDBNull(rdr("LockedUntil")), Nothing, CType(rdr("LockedUntil"), DateTime?))
+u.LastPasswordChangeAt = If(IsDBNull(rdr("LastPasswordChangeAt")), Nothing, CType(rdr("LastPasswordChangeAt"), DateTime?))
+u.CreatedAt = CType(rdr("CreatedAt"), DateTime)
+u.ModifiedAt = If(IsDBNull(rdr("ModifiedAt")), Nothing, CType(rdr("ModifiedAt"), DateTime?))
```

---

### 2. Owner Dashboard Memory and DispatcherTimer Leak (High)

> 🔴 **VERIFICATION (2026-06-11) — largely incorrect; do NOT register as Singleton.** `OwnerDashboardViewModel` already implements `IDisposable` (Dispose stops the timer) and `OwnerDashboardView` disposes it on `Unloaded`; the timer is a UI-thread `DispatcherTimer`, not a background timer — so there is no timer/DB leak. A Singleton lifetime would be disposed on first navigation-away and then reused broken. Re-rated **Low**; see the Verification Addendum at the top.

> [!CAUTION]
> **Severe Memory and Database Resource Leak**  
> Resolving transient `IDisposable` instances from the root DI provider leads to a memory leak where objects are retained by the root container. This keeps the view models, views, and visual trees alive, and leaks background timers which repeatedly query the database.

#### Root Cause
1. In [Application.xaml.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Application.xaml.vb#L178):
   ```vb
   services.AddTransient(Of OwnerDashboardViewModel)()
   ```
2. The `OwnerDashboardViewModel` class implements `IDisposable` to control a 60-second `DispatcherTimer` refresh loop.
3. In `Microsoft.Extensions.DependencyInjection`, any `Transient` service implementing `IDisposable` has its reference tracked by the root container to ensure disposal when the application shuts down. This means they are **never** garbage collected while the app is running.
4. When navigating to the dashboard, the view is resolved, creating a new View and ViewModel.
5. In [OwnerDashboardView.xaml.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/OwnerDashboardView.xaml.vb#L26-L29), event handlers are registered:
   ```vb
   AddHandler viewModel.NavigateToPurchasingRequested, AddressOf OnNavigateToPurchasingRequested
   ```
6. Because the VM is permanently held in memory by the DI container, and the VM holds event delegate references to the View, the View and its entire visual tree (charts, UI controls) are also leaked in memory.
7. Over time, each navigation to the Owner Dashboard creates a new leaked VM + View combo, causing memory usage to compound continuously.

#### Proposed Correction
Register the `OwnerDashboardViewModel` as a **Singleton** instead. Since there is only one main window and one dashboard session active at any time, a singleton lifetime is the cleanest and most correct way to prevent duplicate/leaked VMs.

```diff
-services.AddTransient(Of OwnerDashboardViewModel)()
+services.AddSingleton(Of OwnerDashboardViewModel)()
```

---

### 3. ConnectionHealthMonitor CancellationTokenSource Leak (Medium)

> [!NOTE]
> **Operating System Handle Leak**  
> Setting the token source field to `Nothing` before calling `Dispose()` renders the disposal statement a no-op, leaking OS kernel handles.

#### Root Cause
In [ConnectionHealthMonitor.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Services/ConnectionHealthMonitor.vb#L52-L55):
```vb
Public Sub [Stop]() Implements IConnectionHealthMonitor.[Stop]
    _cts?.Cancel()
    _cts = Nothing
End Sub

Public Sub Dispose() Implements IDisposable.Dispose
    [Stop]()
    _cts?.Dispose()
    _retrySignal.Dispose()
End Sub
```
* `[Stop]()` clears the `_cts` field by setting it to `Nothing`.
* `Dispose()` calls `[Stop]()` first, causing `_cts?.Dispose()` to do absolutely nothing. The underlying `CancellationTokenSource` is never disposed.

#### Proposed Correction
Ensure `_cts` is safely cancelled and disposed within the `[Stop]` method before it is dereferenced.

```diff
 Public Sub [Stop]() Implements IConnectionHealthMonitor.[Stop]
-    _cts?.Cancel()
-    _cts = Nothing
+    If _cts IsNot Nothing Then
+        _cts.Cancel()
+        _cts.Dispose()
+        _cts = Nothing
+    End If
 End Sub

 Public Sub Dispose() Implements IDisposable.Dispose
     [Stop]()
-    _cts?.Dispose()
     _retrySignal.Dispose()
 End Sub
```

---

### 4. Inefficient Parsing in Value Converters (Low)

> [!TIP]
> **Performance Optimization Opportunity**  
> Cell values in tables (DataGrids) are converted on every render. Eliminating string formatting and parsing overhead keeps the scrolling experience smooth and buttery.

#### Root Cause
In [FormattingConverters.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Converters/FormattingConverters.vb#L16):
```vb
Dim decValue As Decimal
If Decimal.TryParse(value.ToString(), decValue) Then
    Return "₱" & decValue.ToString("N2", culture)
End If
```
* **Overhead**: `value.ToString()` constructs a string representation of the numeric value, only for `Decimal.TryParse()` to immediately parse it back. This is executed for every cell in a DataGrid, triggering unnecessary GC allocations and CPU overhead.
* **Correction**: Perform direct type checking and casting (`TypeOf value Is Decimal`, etc.) to process the native value immediately.

#### Proposed Correction
```diff
 Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
     If value Is Nothing OrElse IsDBNull(value) Then
         Return "₱0.00"
     End If
     
-    Dim decValue As Decimal
-    If Decimal.TryParse(value.ToString(), decValue) Then
-        Return "₱" & decValue.ToString("N2", culture)
-    End If
+    If TypeOf value Is Decimal Then
+        Return "₱" & DirectCast(value, Decimal).ToString("N2", culture)
+    ElseIf TypeOf value Is Double Then
+        Return "₱" & DirectCast(value, Double).ToString("N2", culture)
+    ElseIf TypeOf value Is Single Then
+        Return "₱" & DirectCast(value, Single).ToString("N2", culture)
+    ElseIf TypeOf value Is Integer Then
+        Return "₱" & DirectCast(value, Integer).ToString("N2", culture)
+    End If
+
+    ' Fallback for other convertible formats
+    Dim decValue As Decimal
+    If Decimal.TryParse(value.ToString(), decValue) Then
+        Return "₱" & decValue.ToString("N2", culture)
+    End If
     
     Return "₱0.00"
 End Function
```
*(Apply similar logic to `PesoNoDecimalConverter`, `SignedPesoConverter`, and `QuantityConverter` for complete optimization.)*
