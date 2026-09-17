# MerchSys

MerchSys is the core application for **VISTA** (Villon Integrated Supply and Trade Application), a WPF desktop system for Villon Farm Supply. It is built in Visual Basic .NET 10 targeting .NET 10, following a modular monolith architecture where five class libraries (SharedKernel + four business modules) are composed by a single WPF startup project, with module boundaries enforced at the project reference level and cross-module communication handled exclusively through MediatR events and queries.

## Projects

| Project | Type | Description |
|---------|------|-------------|
| `MerchSys.App` | WPF executable | Startup project; hosts the main window, DI container, and navigation shell |
| `MerchSys.SharedKernel` | Class library | Shared base types: entities, events, queries, enums, interfaces |
| `MerchSys.Purchasing` | Class library | Vendor management, purchase orders, AP tracking, reorder engine |
| `MerchSys.Inventory` | Class library | Stock management, FIFO costing, expiry tracking, shrinkage alerts |
| `MerchSys.POS` | Class library | Sales transactions, informal credit (utang), receipts, returns |
| `MerchSys.Accounting` | Class library | Financial reports, KPIs, plain-language summaries |

## How to Build

```powershell
dotnet build MerchSys.slnx
```

## How to Run

```powershell
dotnet run --project src/MerchSys.App
```

## How to Restore Packages

```powershell
dotnet restore MerchSys.slnx
```
