# Jules AI Instructions for VISTA_Project

## CRITICAL RULE: Windows Forms ONLY
- **DO NOT USE WPF.** Per the professor's strict instructions, this project must use **Windows Forms** exclusively. Any existing WPF code or folders must be migrated to or replaced by Windows Forms equivalents. Do not generate XAML or WPF window classes.

## Architecture
- **Language**: Visual Basic .NET 10 (VB.NET). All code must use `.vb`.
- **Database**: Centralized MariaDB 11.4.x LTS. Do not use SQLite or sync infrastructure (as per the 2026-05-28 pivot).

## Build & Test
- Run `dotnet restore` to restore dependencies.
- Run `dotnet build` to compile the solution.
