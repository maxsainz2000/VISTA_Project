---
module: MerchSys.Accounting
plan-id: ACC-18
title: "Tamper Incident Report UI"
depends-on: [ACC-15]
estimated-files: 4
priority: low
---

# Tamper Incident Report UI

## Context

ACC-15 delivered the `ReceiptTamperAuditHandler` and the `ITamperAuditQueryService` for querying the `Acc_TamperAuditLog`. The handler consumes `ReceiptTamperDetectedEvent` (INFRA-07) and persists immutable audit rows. The 2026-05-15 Accounting audit notes that a **reporting UI** consuming `ITamperAuditQueryService` is explicitly documented as out of scope for ACC-15 and deferred to a future plan. This is that plan.

The report view allows the Owner and Manager to review tamper incidents — when they occurred, which receipts were affected, the expected vs. actual hash, and the severity. This is an audit compliance requirement: BIR may request evidence that tamper events are logged and reviewed.

## Prerequisites

- **ACC-15** (Receipt Tamper Audit Handler) — `ITamperAuditQueryService`, `TamperAuditEntry` entity, `Acc_TamperAuditLog` table

## Wiki References

- `concepts/bir-compliance.md` — Audit trail requirements for receipt integrity
- `concepts/owasp-da-top10.md` — Audit log visibility for financial state changes

## Deliverables

```
MerchSys.Accounting/ViewModels/
└── TamperAuditReportViewModel.vb               ' New

MerchSys.App/Views/Accounting/
├── TamperAuditReportView.xaml                  ' New
└── TamperAuditReportView.xaml.vb               ' New

MerchSys.App/Startup/
└── NavigationRegistration.vb                   ' Modified — add navigation item
```

## Specification

### TamperAuditReportViewModel

```vb
Public Class TamperAuditReportViewModel
    Inherits ViewModelBase

    Private ReadOnly _queryService As ITamperAuditQueryService

    Public Property Entries As ObservableCollection(Of TamperAuditEntryDto)
    Public Property DateFrom As DateTime
    Public Property DateTo As DateTime
    Public Property IsLoading As Boolean

    Public Async Function LoadAsync() As Task
        IsLoading = True
        Dim results = Await _queryService.GetEntriesAsync(DateFrom, DateTo)
        Entries = New ObservableCollection(Of TamperAuditEntryDto)(
            results.Select(Function(e) New TamperAuditEntryDto(e)))
        IsLoading = False
    End Function
End Class
```

### TamperAuditEntryDto

```vb
Public Class TamperAuditEntryDto
    Public Property DetectedAt As DateTime
    Public Property ReceiptId As Integer
    Public Property ReceiptNumber As String
    Public Property ExpectedHash As String
    Public Property ActualHash As String
    Public Property Severity As String         ' "Critical"
    Public Property Details As String
End Class
```

### TamperAuditReportView

A `DataGrid`-based view displaying tamper incidents with columns:
- Date/Time Detected
- Receipt Number
- Expected Hash (truncated, tooltip shows full)
- Actual Hash (truncated, tooltip shows full)
- Severity (colour-coded: red for Critical)
- Details

Date range filter controls at the top. Default range: last 30 days.

### Navigation Registration

Add a `NavigationItem` for the report view, accessible to **Manager** and **Owner** roles. Place in the Accounting sidebar section after the existing accounting views.

## Implementation Notes

- The view is **read-only** — no actions beyond viewing and filtering. No edit, delete, or export functionality in this plan.
- Hash values should be truncated to first 12 characters in the grid with full value in tooltip.
- If no tamper incidents exist, show an informational banner: "No tamper incidents detected in the selected period."
- Future enhancement: export to CSV/PDF for BIR auditor submission. Out of scope here.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. Navigation item for "Tamper Audit Report" appears in the Accounting sidebar.
3. The view loads and displays tamper incidents from `ITamperAuditQueryService`.
4. Date range filter correctly narrows results.
5. Empty state shows informational message.
6. Manager and Owner can both access the view.
7. No runtime XAML binding errors.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-18-summary.md` using `Progress/_template.md`.

### Documentation
- XML doc on `TamperAuditReportViewModel` describing data source and filtering behaviour.
