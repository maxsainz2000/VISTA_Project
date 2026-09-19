---
module: Infrastructure
plan-id: INFRA-01
title: "Solution Scaffold"
depends-on: []
estimated-files: 12
---

# Solution Scaffold

## Context

This is the **first plan** in the VISTA build sequence. It creates the Visual Studio solution, all project files, installs NuGet packages, and establishes the folder conventions that every subsequent plan depends on.

VISTA is a WinForms desktop application built in VB.NET 10 on .NET 10 following a **modular monolith** architecture. The solution contains one WinForms startup project and five class libraries (SharedKernel + 4 business modules).

## Prerequisites

None — this is the first plan.

## Wiki References

Consult these files in `LLM_Wiki/wiki/` for domain context:
- `concepts/modular-monolith.md` — architecture pattern
- `concepts/client-server-wpf.md` — WPF stack overview
- `analysis/tech-stack-reference.md` — full technology list with versions

## Deliverables

Create the following structure under `WPF_Applications/MerchSys/`:

```
WPF_Applications/MerchSys/
├── MerchSys.slnx
│
├── src/
│   ├── MerchSys.App/                         ← WinForms startup project
│   │   ├── MerchSys.App.vbproj
│   │   ├── Program.vb
│   │   ├── MainWindow.vb
│   │   ├── MainWindow.Designer.vb
│   │   └── Views/                            ← empty folder for module views
│   │
│   ├── MerchSys.SharedKernel/                ← Shared types class library
│   │   ├── MerchSys.SharedKernel.vbproj
│   │   ├── Entities/                         ← empty
│   │   ├── Events/                           ← empty
│   │   ├── Queries/                          ← empty
│   │   ├── Enums/                            ← empty
│   │   └── Interfaces/                       ← empty
│   │
│   ├── MerchSys.Purchasing/                  ← Purchasing class library
│   │   ├── MerchSys.Purchasing.vbproj
│   │   ├── Entities/
│   │   ├── Services/
│   │   ├── Data/
│   │   ├── Handlers/
│   │   └── ViewModels/
│   │
│   ├── MerchSys.Inventory/                   ← Inventory class library
│   │   ├── MerchSys.Inventory.vbproj
│   │   ├── Entities/
│   │   ├── Services/
│   │   ├── Data/
│   │   ├── Handlers/
│   │   └── ViewModels/
│   │
│   ├── MerchSys.POS/                         ← POS class library
│   │   ├── MerchSys.POS.vbproj
│   │   ├── Entities/
│   │   ├── Services/
│   │   ├── Data/
│   │   ├── Handlers/
│   │   └── ViewModels/
│   │
│   └── MerchSys.Accounting/                  ← Accounting class library
│       ├── MerchSys.Accounting.vbproj
│       ├── Entities/
│       ├── Services/
│       ├── Data/
│       ├── Handlers/
│       └── ViewModels/
```

## Specification

### Solution File (`MerchSys.slnx`)
- Create via `dotnet new sln` in `WPF_Applications/MerchSys/`
- Add all 6 projects to the solution

### WinForms Startup Project (`MerchSys.App`)
- Create via `dotnet new winforms --language VB` targeting `.NET 10`
- This is the only executable project; all others are class libraries
- Add project references to all 5 class libraries
- `Program.vb` should have a minimal startup that sets up DI (placeholder for now)
- `MainWindow.vb` should be a basic shell with a navigation sidebar (placeholder — later plans will populate it)
- CRITICAL: Must not contain any WPF elements. No `<UseWPF>true</UseWPF>`, no `.xaml` files, no `System.Windows` namespaces. Explicitly delete any `.xaml` files that may exist.

### SharedKernel Library (`MerchSys.SharedKernel`)
- Create via `dotnet new classlib --language VB` targeting `.NET 10`
- NuGet packages: `MediatR` (latest stable)
- This project is referenced by all other projects
- No WinForms dependency — pure .NET library

### Module Class Libraries (Purchasing, Inventory, POS, Accounting)
- Create each via `dotnet new classlib --language VB` targeting `.NET 10`
- Each references `MerchSys.SharedKernel` only — **no cross-module references**
- NuGet packages per module library:
  - `Microsoft.EntityFrameworkCore` (10.x)
  - `Microsoft.EntityFrameworkCore.Sqlite` (10.x)
  - `CommunityToolkit.Mvvm` (latest stable)
  - `MediatR` (latest stable)
- Create the standard subfolder structure: `Entities/`, `Services/`, `Data/`, `Handlers/`, `ViewModels/`

### WinForms App Additional NuGet Packages
- `Microsoft.Extensions.DependencyInjection` (latest)
- `Microsoft.Extensions.Hosting` (latest)
- `Notification.Wpf` (latest stable) — for desktop toast alerts
- All packages from module libraries are transitively available

## Implementation Notes

- Use `dotnet` CLI commands to create projects, then add to the solution
- All projects must target `net10.0`
- All projects must use VB.NET (`--language VB`)
- Ensure the `.slnx` file lives at `WPF_Applications/MerchSys/MerchSys.slnx`
- The `src/` subfolder keeps source projects organized
- Do NOT create any test projects — testing is handled in a separate session
- Ensure no `.xaml` files exist in the solution!

## Acceptance Criteria

1. `dotnet build MerchSys.slnx` completes with **0 errors, 0 warnings**
2. All 6 projects appear in the solution
3. Project reference graph:
   - `App` → `SharedKernel`, `Purchasing`, `Inventory`, `POS`, `Accounting`
   - Each module → `SharedKernel` only
   - No module-to-module references
4. All NuGet packages are restored and resolved
5. Running `MerchSys.App` launches a blank WinForms window
6. There are exactly 0 `.xaml` files anywhere in the solution folder.

## Output Requirements

### Implementation Summary
After completing all code, create a progress report at:
```
Progress/VISTA_Modules/Infrastructure/INFRA-01-summary.md
```
Using the template structure from `Progress/_template.md`. Include:
- All files created with full paths
- NuGet packages installed with versions
- Build status confirmation
- Any deviations from this plan

### Documentation
- Add XML doc comments to `Application.xaml.vb` explaining the DI setup placeholder
- Create a `README.md` at `WPF_Applications/MerchSys/README.md` with:
  - Solution overview (one paragraph)
  - Project list with descriptions
  - How to build (`dotnet build`)
  - How to run (`dotnet run --project src/MerchSys.App`)
