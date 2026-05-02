---
module: MerchSys.Purchasing
plan-id: PUR-05
title: "Vendor Directory"
depends-on: [PUR-01, PUR-02]
estimated-files: 2
---

# Vendor Directory

## Context

Implements vendor management — CRUD operations for suppliers including contact details, pricing history tracking, and lead time management.

## Prerequisites

- **PUR-01** (Domain Models) — `Vendor` entity exists
- **PUR-02** (Data Access) — `PurchasingDbContext` configured

## Wiki References

- `sources/purchasing-module-paper.md` — "Vendor Directory: contact details, pricing history, lead time, multi-supplier support"

## Deliverables

```
MerchSys.Purchasing/Services/
├── IVendorService.vb
└── VendorService.vb
```

## Specification

### IVendorService

```
CreateAsync(dto As CreateVendorDto) As Task(Of Vendor)
GetByIdAsync(id As Integer) As Task(Of Vendor)
GetAllAsync() As Task(Of List(Of Vendor))
UpdateAsync(id As Integer, dto As UpdateVendorDto) As Task(Of Vendor)
DeleteAsync(id As Integer) As Task(Of Boolean)         ' Soft delete
SearchAsync(searchTerm As String) As Task(Of List(Of Vendor))
GetVendorWithPurchaseHistoryAsync(id As Integer) As Task(Of VendorDetailDto)
```

### DTOs

**CreateVendorDto / UpdateVendorDto:** Name, ContactPerson, Phone, Email, Address, DefaultLeadTimeDays, Notes

**VendorDetailDto:** Vendor + TotalPurchaseOrders, TotalSpent, LastOrderDate, AverageLeadTimeDays

### Business Rules

1. Vendor `Name` must be unique (case-insensitive)
2. `Phone` is required
3. `DefaultLeadTimeDays` must be > 0
4. Delete is soft delete
5. Search matches on Name, ContactPerson, Phone (case-insensitive)

## Acceptance Criteria

1. `dotnet build` succeeds
2. CRUD operations work; duplicate names rejected
3. Soft delete works correctly
4. Search is case-insensitive

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Purchasing/PUR-05-summary.md`
