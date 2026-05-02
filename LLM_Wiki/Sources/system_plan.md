**SYSTEM PLAN**

**Villon Integrated Supply and Trade Application**

Villon Farm Supply

─────────────────────────────────────

**Document Type:** System Plan

**Phase:** Specification / Pre-Development

**Architecture:** Modular Monolith — Client-Server WPF

**Development Platform:** Visual Basic .NET 10 / Visual Studio 2026

**1. Executive Summary**

Villon Farm Supply is an agricultural supply and trade business serving local farmers with inputs such as fertilizers, pesticides, seeds, and animal feeds. The business currently operates without a purpose-built system, relying entirely on physical record books, manual shelf inspections, informal credit ledgers, and verbal supplier communication.

This system plan specifies the design for the Villon Integrated Supply and Trade Application — a WPF client-server desktop application built in Visual Basic .NET 10. The system directly addresses every operational gap identified during the manager interview and fulfills the professor-specified client-server architecture requirement.

The application is structured as a Modular Monolith with four independently bounded modules:

- Module 1 — Purchasing / Procurement

- Module 2 — Inventory Management

- Module 3 — Point of Sale / Sales

- Module 4 — Accounting & Financial Reporting

The system targets three analytical layers across all modules: Descriptive (readable information from raw data), Predictive (forecast reorder timing and quantity), and Prescriptive (automated alerts and recommended actions). All financial reports include plain-language interpretation, analytics, and visual graphs — a mandatory feature given the manager's partial financial literacy.

**2. Business Profile**

|                        |                                                                                 |
|------------------------|---------------------------------------------------------------------------------|
| **Business Name**      | Villon Farm Supply                                                              |
| **Business Type**      | Agricultural supply and trade (farm inputs)                                     |
| **Product Categories** | Fertilizers, Pesticides / Chemicals, Seeds, Animal Feeds                        |
| **SKU Count**          | Approximately 50 distinct products                                              |
| **BIR Status**         | Registered — issues Official Receipts (ORs); VAT status unconfirmed             |
| **Audit History**      | Has been audited by BIR; financial records retained long-term                   |
| **System Users**       | Manager (full control), Owner (read-only KPIs + notifications)                  |
| **Busiest Day**        | Sunday — local market day; highest transaction volume                           |
| **Peak Season**        | Palay planting and growing season (higher demand for insecticides, fertilizers) |

**3. Identified Problems and Gaps**

The following problems were directly identified from the manager interview. These take priority over all other feature considerations during implementation.

**3.1 Purchasing / Procurement**

| **#** | **Problem**                                                                                         | **Impact**                           |
|--------|-----------------------------------------------------------------------------------------------------|--------------------------------------|
| P1     | Reorder decisions based entirely on gut feel and visual shelf inspection — no data-driven threshold | Stockouts and overordering risk      |
| P2     | End-to-end purchasing is fully manual: shelf check → phone supplier → record in physical book       | Time-consuming; no audit trail       |
| P3     | Supplier prices change per order; each price change forces manual retail price updates              | Incorrect margins; wasted time       |
| P4     | No formal AP ledger — supplier credit tracked by supplier visit schedule only                       | Payment missed; financial blind spot |
| P5     | Price volatility creates retail pricing errors and incorrect COGS calculation                       | Profitability distortion             |
| P6     | Seasonal demand spikes not proactively planned — no historical demand model                         | Stockouts during peak season         |

**3.2 Inventory Management**

| **#** | **Problem**                                                                      | **Impact**                                   |
|--------|----------------------------------------------------------------------------------|----------------------------------------------|
| I1     | Stock on hand tracked only in a physical record book updated manually            | Data lag; transcription errors               |
| I2     | Weekly full physical count takes 1–3 hours each time                             | Operational downtime; error-prone            |
| I3     | No real-time dashboard showing levels, alerts, or movement on one screen         | Manager operates with delayed information    |
| I4     | Expiry dates not systematically tracked for pesticides, seeds, and feeds         | Expired goods sold or written off unrecorded |
| I5     | Total monetary value of stock on hand is not known at any point in time          | No basis for financial planning              |
| I6     | Stockouts have occurred due to demand spikes combined with delayed replenishment | Lost revenue; customer defection             |
| I7     | No visibility into fast-moving vs. slow-moving products by season                | Suboptimal ordering decisions                |

**3.3 Point of Sale / Sales**

| **#** | **Problem**                                                                          | **Impact**                                   |
|--------|--------------------------------------------------------------------------------------|----------------------------------------------|
| S1     | No formal sales return recording system — replacements handled case-by-case          | Returns untracked; inventory not updated     |
| S2     | Credit balances tracked in manual ledger book; no credit limits enforced             | Uncollected receivables; collection friction |
| S3     | Cashiering errors and price disputes resolved without verifiable transaction records | Trust issues; revenue loss                   |
| S4     | VAT registration status unconfirmed — system must handle both VAT and non-VAT        | BIR compliance risk                          |
| S5     | No automated daily sales summary — manager tracks mentally                           | Decision-making without data                 |

**3.4 Accounting & Financial Reporting**

| **#** | **Problem**                                                                             | **Impact**                                   |
|--------|-----------------------------------------------------------------------------------------|----------------------------------------------|
| A1     | Bookkeeping done manually by the owner — no automated financial reporting               | Owner time cost; delayed information         |
| A2     | Owner requests reports via text/written summaries — slow and unreliable                 | Delayed owner oversight                      |
| A3     | No product-level gross profit margin tracking                                           | Unprofitable products go undetected          |
| A4     | Cash flow shortfalls occur when credit sales delay cash while supplier bills come due   | Inability to restock or pay suppliers        |
| A5     | Manager has partial financial literacy — reports not interpretable without assistance   | Reports unused; decisions based on intuition |
| A6     | No automated early warning for slow sales, supplier bill due dates, or large AR overdue | Problems detected too late                   |
| A7     | Financial position only known after-the-fact; no real-time integrated view              | Reactive management only                     |

**4. System Goals**

**4.1 Primary Objectives**

- **Improve Efficiency:** Eliminate manual record books and automate repetitive transaction recording across all four modules.

- **Reduce Costs:** Prevent stockouts (lost sales), overordering (tied-up capital), and expired goods write-offs through data-driven alerts.

- **Support Decision-Making:** Provide the manager with descriptive, predictive, and prescriptive analytics; provide the owner with real-time KPI visibility.

**4.2 Three Analytics Layers**

| **Layer**    | **Description**                                                                                                                               | **Examples**                                                                                  |
|--------------|-----------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------|
| Descriptive  | Process raw transaction data into readable, meaningful information                                                                            | Daily sales summary; inventory valuation; P&L report with plain-language interpretation       |
| Predictive   | Forecast when to reorder, estimated delivery arrival, and quantity needed by a target date based on historical velocity and seasonal patterns | Days-until-stockout estimate; suggested reorder quantity; seasonal demand flags               |
| Prescriptive | Automated alerts with recommended actions delivered before problems occur                                                                     | Low-stock notification with reorder suggestion; overdue AP payment alert; flagged AR accounts |

**5. System Architecture**

**5.1 Architecture Pattern — Modular Monolith (Client-Server)**

The system is built as a client-server WPF application per the professor's requirement. The WPF client communicates with a centralized XAMPP MariaDB database via Entity Framework Core, enabling the Manager and Owner to read and write data simultaneously from separate sessions.

Internally, the application is structured as a Modular Monolith: a single deployable .exe composed of four distinct Class Libraries (MerchSys.Purchasing, MerchSys.Inventory, MerchSys.POS, MerchSys.Accounting). Modules communicate exclusively through well-defined public interfaces and an event-driven mediator (MediatR). No module accesses another module's internal data directly. This design allows any module to be extracted into an independent microservice post-prototype.

**5.2 Technology Stack**

|                          |                                                                              |
|--------------------------|------------------------------------------------------------------------------|
| **Language**             | Visual Basic (.NET 10)                                                       |
| **IDE**                  | Visual Studio 2026 (Windows)                                                 |
| **UI Framework**         | Windows Presentation Foundation (WPF)                                        |
| **MVVM Library**         | CommunityToolkit.Mvvm (latest stable — confirm version on NuGet before init) |
| **ORM**                  | Entity Framework Core 10                                                     |
| **Mediator**             | MediatR                                                                      |
| **Local Database**       | SQLite (offline-first, all transactions)                                     |
| **Central Database**     | MariaDB 11.4.x LTS via XAMPP on Windows                                      |
| **In-App Notifications** | ToastNotifications NuGet (rafallopatka/ToastNotifications)                   |
| **Source Control**       | Git (built-in to Visual Studio 2026)                                         |
| **Deployment Target**    | Single Windows desktop executable (.exe)                                     |

**5.3 Offline-First & Sync Strategy**

All transactions are committed to local SQLite first. A dedicated SyncWorker (BackgroundService implementing IHostedService) monitors connection status and synchronizes SQLite to MariaDB automatically when both conditions are simultaneously true:

- NetworkAvailabilityChanged fires with IsAvailable = true (at least one non-loopback interface is up)

- A TCP probe to the MariaDB host and port succeeds (confirms actual reachability of the sync target)

This dual-check is required because NetworkAvailabilityChanged alone is over-optimistic and does not confirm reachability of the sync target. The dual condition prevents false sync triggers that corrupt partial records.

**5.4 Class Library Names**

|                         |                                                          |
|-------------------------|----------------------------------------------------------|
| **MerchSys.Purchasing** | Vendor management, Purchase Orders, AP tracking          |
| **MerchSys.Inventory**  | Real-time stock tracking, expiry, FIFO valuation, alerts |
| **MerchSys.POS**        | Transaction processing, credit management, receipts      |
| **MerchSys.Accounting** | Financial reports, audit trail, owner dashboard          |

**6. Module Specifications**

**6.1 Module 1 — Purchasing / Procurement (MerchSys.Purchasing)**

This module manages the inflow of goods from vendors to the store. It replaces the manual phone-call and physical-book workflow with a structured digital process covering the entire order lifecycle.

**Core Features**

- **Vendor / Supplier Directory:** Contact details, pricing history per product, lead time, and payment terms stored per supplier. Supports multi-supplier products with selection criteria: availability, price, quality, lead time.

- **Purchase Order (PO) Management:** Create, send, receive, and verify POs. Status lifecycle: Draft → Submitted → Received → Verified. Each PO records quantities ordered vs. quantities received with discrepancy flagging.

- **Goods Receiving Verification:** Manager confirms received quantity and condition per item. Expiry date captured at receiving for pesticides, seeds, and feeds. Discrepancies trigger supplier contact flag.

- **Accounts Payable (AP) Tracking:** Records supplier credit terms, payment due dates, and outstanding balances. Replaces informal supplier-visit-based tracking.

- **Price Change Detection:** When a new purchase price differs from the last recorded cost, the system alerts the manager to review and update the retail selling price before the item is sold.

- **Reorder Suggestion Engine:** Calculates suggested order quantity based on current stock level, minimum threshold, average daily sales velocity, and supplier lead time. Flags seasonal demand periods (palay planting/growing season) to suggest pre-season stock-up.

**6.2 Module 2 — Inventory Management (MerchSys.Inventory)**

This module serves as the central hub. Every purchase received and every sale made automatically updates inventory in real time — eliminating the weekly physical count as the primary source of truth.

**Core Features**

- **Real-Time Stock Dashboard:** Single screen showing all products, current stock on hand, low-stock status, recent inflow/outflow, and total inventory value. Directly addresses the manager's stated need (Q19).

- **Automated Low-Stock Alerts:** When stock falls below the manager-defined minimum threshold, the system issues a prescriptive notification with a reorder suggestion.

- **Expiry Date Tracking:** Expiry dates recorded at batch/lot level for pesticides, chemicals, seeds, and animal feeds. Alerts generated when expiry is approaching.

- **FIFO Costing:** Oldest batch cost used first when valuing sold items, per client confirmation. Batch-level purchase records maintained for accurate COGS calculation.

- **Inventory Valuation:** Real-time total monetary value of stock on hand calculated using FIFO, eliminating the current gap where this figure is unknown.

- **Shrinkage Recording:** Manager records stock losses by category (damage, expiry write-off, admin discrepancy) with financial impact captured for the Accounting module.

- **Predictive Stockout Estimate:** Based on average daily sales velocity, the system calculates estimated days until each product runs out and displays this on the dashboard.

**6.3 Module 3 — Point of Sale / Sales (MerchSys.POS)**

This module handles all customer-facing transactions and replaces the manual receipt book and credit ledger with a fully auditable digital system.

**Core Features**

- **Transaction Processing:** Records product, quantity, unit price, discount applied, payment method, customer, and receipt number for every sale. Inventory updated automatically on each completed transaction.

- **Payment Mode Support:** Cash, GCash / e-wallet, bank transfer, and credit/charge account (utang). Each payment type recorded separately for accurate accounting.

- **Official Receipt (OR) Generation:** System issues and records ORs for every transaction, supporting BIR compliance.

- **Credit Customer Management:** Customer profile with credit balance, transaction history, and payment status. System blocks additional credit if an existing balance is unpaid, per the manager's current informal rule.

- **Discount Management:** Bulk buyer and regular customer discounts applied at transaction level with manager authorization. Discount amounts tracked for the Sales Reports.

- **Sales Return & Exchange:** Formal return recording: reason captured, inventory restocked, original transaction linked. Replaces the current informal case-by-case handling.

- **VAT-Ready Structure:** System accommodates both VAT-registered (12%) and non-VAT scenarios. VAT status confirmed with owner before go-live configuration.

- **Daily Sales Summary:** Automated end-of-day summary showing total sales, number of transactions, and current stock snapshot — directly per manager request (Q28).

- **AR Collection Flags:** Overdue credit accounts flagged for manager follow-up, replacing the current system of waiting for the customer to visit the store.

**6.4 Module 4 — Accounting & Financial Reporting (MerchSys.Accounting)**

This module pulls data from all three operational modules and generates financial reports for both the manager and the owner. Every report includes plain-language interpretation, analytics, and visual graphs — mandatory given the manager's partial financial literacy and the owner's need for transparent oversight without direct operational involvement.

**Core Financial Statements**

| **Report**             | **Description**                                                                                                                                                    |
|------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Income Statement (P&L) | Merchandising format: Net Sales (Gross minus Returns/Discounts), COGS (FIFO), Gross Profit, Operating Expenses, Net Income. Includes month-over-month trend chart. |
| Balance Sheet          | Assets (Merchandise Inventory at FIFO cost), Liabilities (Accounts Payable), Owner's Equity at a point in time.                                                    |
| Cash Flow Statement    | Tracks actual cash movements. Critical: client confirmed cash-profitable-but-cash-poor situations due to credit sales gap.                                         |

**POS / Sales Integration Reports**

| **Report**                 | **Description**                                                                                  |
|----------------------------|--------------------------------------------------------------------------------------------------|
| Sales Journal              | Chronological list of all sales transactions (cash and credit) with payment method breakdown.    |
| AR Aging Report            | Categorizes unpaid customer balances by 30 / 60 / 90 days outstanding. Flags high-risk accounts. |
| Sales Returns & Allowances | Financial impact of returned or exchanged merchandise, linked to original transactions.          |
| VAT / Sales Tax Report     | Summarizes taxes collected per period for BIR filing compliance.                                 |

**Purchasing Integration Reports**

| **Report**                | **Description**                                                                         |
|---------------------------|-----------------------------------------------------------------------------------------|
| Purchases Journal         | All merchandise purchases from suppliers, chronological with supplier and PO reference. |
| AP Aging Report           | Tracks outstanding supplier payables by due date. Alerts for approaching due dates.     |
| Purchase Discounts Report | Records savings from early payment terms offered by suppliers.                          |

**Inventory Integration Reports**

| **Report**                              | **Description**                                                                                  |
|-----------------------------------------|--------------------------------------------------------------------------------------------------|
| Inventory Valuation Report              | Total monetary value of stock on hand using FIFO costing. Real-time and periodic snapshot views. |
| Cost of Goods Sold (COGS) Report        | Detailed FIFO-based breakdown of direct costs of goods sold within a period.                     |
| Inventory Shrinkage / Adjustment Report | Financial summary of stock losses from damage, expiry, and admin discrepancies.                  |

**General Accounting & Audit Reports**

| **Report**         | **Description**                                                                                     |
|--------------------|-----------------------------------------------------------------------------------------------------|
| General Ledger     | Master record of every financial transaction across all modules with full detail.                   |
| Trial Balance      | Ensures total debits equal total credits before generating final financial statements.              |
| Audit Trail Report | Security report: who (user role), what (action), when (timestamp) for all financial record changes. |

**Owner KPI Dashboard (Read-Only)**

- **Purchasing KPIs:** Active vendor list, current PO status and pending deliveries.

- **Inventory KPIs:** Current stock on hand summary, low-stock item count.

- **Sales KPIs:** Daily/weekly revenue, transaction count, top-selling products.

- **Accounting KPIs:** Current period profit/loss, cash position, overdue AR total, upcoming AP due.

- **Notifications:** In-app WPF overlay notifications (ToastNotifications) categorized by module. Severity types: Info, Success, Warning, Error.

**7. User Roles and Access Control**

| **Feature Area**                           | **Manager** | **Owner** |
|--------------------------------------------|-------------|-----------|
| Purchasing — Create / Edit POs             | Full Access | No Access |
| Purchasing — View PO Status                | Full Access | Read-Only |
| Inventory — Record Adjustments / Shrinkage | Full Access | No Access |
| Inventory — View Stock Dashboard           | Full Access | Read-Only |
| POS — Process Transactions                 | Full Access | No Access |
| POS — View Transaction History             | Full Access | Read-Only |
| Accounting — View All Reports              | Full Access | Read-Only |
| System Settings / User Management          | Full Access | No Access |

The Owner role enforces read-only access at every data-access boundary in the codebase, not just at the UI layer. Principle of least privilege is applied per OWASP DA5.

**8. Security Requirements**

The system addresses the OWASP Desktop Application Security Top 10 (2021) in full. These requirements are non-negotiable and must be implemented before the system is deployed.

| **ID** | **Requirement**           | **Implementation**                                                                                                                                                                                                                                                                     |
|--------|---------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| DA1    | Injection Prevention      | EF Core parameterized queries exclusively. No raw SQL string concatenation or interpolation anywhere in the codebase.                                                                                                                                                                  |
| DA2    | Authentication & Session  | Username and password authentication. Passwords hashed with Argon2id. Session token held in-memory only (never written to SQLite or disk). Token invalidated on logout and after 15–30 minutes of inactivity. Lockout after 3–5 consecutive failed attempts. No hardcoded credentials. |
| DA3    | Sensitive Data Exposure   | Session token cleared from memory on logout. No PII, passwords, or tokens written to logs. No secrets in config files or compiled binaries.                                                                                                                                            |
| DA4    | Cryptography              | Argon2id: minimum 19 MiB memory, 2 iterations, 1 degree of parallelism. Do not use MD5, SHA-1, or plain SHA-256 for password storage. AES-256 in GCM mode for any symmetric encryption of config values.                                                                               |
| DA5    | Authorization             | Manager vs. Owner role enforced at every data-access boundary, not just the UI. Owner role is strictly read-only.                                                                                                                                                                      |
| DA6    | Security Misconfiguration | No debug or test code included in release builds. No default credentials. All configuration stored outside the compiled binary.                                                                                                                                                        |
| DA7    | Insecure Communication    | SQLite to MariaDB sync traffic encrypted over the local network. Obsolete cipher suites disabled.                                                                                                                                                                                      |
| DA8    | Code Quality              | No dead code in release builds. Code review gate required before any merge to the main branch.                                                                                                                                                                                         |
| DA9    | Vulnerable Components     | All NuGet dependencies pinned to specific versions. Audit with dotnet list package --vulnerable before every release build.                                                                                                                                                            |
| DA10   | Audit Logging             | Structured audit log for all financial record changes: user role, action type, and timestamp recorded on every write.                                                                                                                                                                  |

**9. Database Design Principles**

- **Third Normal Form (3NF):** Atomic column values (1NF); all non-key attributes fully dependent on the entire primary key (2NF); no transitive dependencies among non-key attributes (3NF). Foreign keys enforce referential integrity throughout.

- **FIFO Batch-Level Records:** Purchase price recorded at the batch/lot level per product to support accurate FIFO COGS calculation. Oldest unreserved batch cost consumed first on each sale.

- **Expiry Date at Batch Level:** Expiry dates stored per batch, not per product, to support batch-level expiry alerts and write-offs for pesticides, seeds, and animal feeds.

- **Audit Columns:** All financial and inventory tables include created_by, created_at, modified_by, and modified_at columns to support the audit trail requirement (DA10).

- **Soft Deletes:** Financial records are never hard-deleted. Deleted status stored as a flag with deletion timestamp and user, preserving audit integrity for BIR compliance.

**10. Risk Register**

The following risks are identified based on the client's current operating environment and the proposed system architecture. Each risk is assessed by likelihood and impact.

| **Risk**                         | **Description**                                                                                                                                 | **Likelihood** | **Impact**  |
|----------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------|----------------|-------------|
| Data Loss                        | SQLite database corrupted; transition from physical record books incomplete; unsynchronized records lost during system failure                  | Medium         | High        |
| System Downtime                  | XAMPP/MariaDB server offline during business hours; host PC hardware failure; power interruption during a transaction                           | Medium         | Medium–High |
| Security Breach                  | Unauthorized access to financial records or customer credit data; weak passwords; session hijacking on a shared machine                         | Low–Medium     | High        |
| Data Sync Conflict               | Offline SQLite transactions conflict with MariaDB records after reconnect, especially if multiple users enter data simultaneously while offline | Medium         | Medium      |
| Price Volatility & Costing Error | Retail price not updated after supplier cost change — confirmed ongoing problem — leading to incorrect COGS and distorted profit margins        | High           | High        |
| Stockout or Overstock            | Predictive model insufficient for unusual demand spikes or unexpected supply chain delays; seasonal pattern not captured correctly              | Medium         | Medium      |
| BIR Compliance Gap               | VAT registration status unconfirmed; OR generation or tax reporting misconfigured for the business's actual tax status                          | Low            | High        |

**11. Risk Mitigation Strategies**

| **Risk**                         | **Mitigation Strategy**                                                                                                                                                                                                                                                                                                                                                               |
|----------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Data Loss                        | Offline-first SQLite ensures all transactions are persisted locally before any network sync attempt. Automated nightly backup to a secondary location via Windows Task Scheduler. A structured data migration plan will encode existing record book data into the system before go-live to prevent information loss during transition.                                                |
| System Downtime                  | The offline-first architecture means the application runs fully and processes all transactions without requiring the central MariaDB server. The MariaDB instance should be hosted on a dedicated machine separate from the POS workstation. An uninterruptible power supply (UPS) is recommended for the server PC to prevent mid-sync corruption.                                   |
| Security Breach                  | Argon2id password hashing with OWASP-compliant parameters prevents credential theft from the database. In-memory-only session tokens are never exposed on disk. Role-based access control enforced at the data layer prevents privilege escalation. Failed-login lockout limits brute-force attempts. Audit trail logs all financial record changes with user identity and timestamp. |
| Data Sync Conflict               | The SyncWorker uses a dual-condition check (network interface up AND TCP probe to MariaDB succeeds) before initiating sync, preventing partial or failed sync attempts. A last-write-wins policy with a conflict log is applied on sync. All records carry timestamps to detect and resolve ordering conflicts.                                                                       |
| Price Volatility & Costing Error | The system detects when a new purchase price differs from the last recorded cost for a product and immediately prompts the manager to review and update the retail selling price before the item is made available for sale. FIFO batch-level costing ensures historical sales are not retroactively affected by new purchase prices.                                                 |
| Stockout or Overstock            | Predictive reorder alerts are triggered before stock reaches zero, based on real-time sales velocity and the manager-defined minimum stock threshold per product. Seasonal demand flags (palay planting/growing season) allow the system to recommend pre-season stock-up. The manager retains override authority on all suggested quantities.                                        |
| BIR Compliance Gap               | VAT registration status is confirmed with the owner before system go-live configuration. The system is architected to support both VAT and non-VAT modes. OR issuance, VAT report generation, and record retention comply with BIR requirements, including long-term record storage per the client's existing practice.                                                               |

**12. Key Constraints and Non-Negotiables**

- Client-server WPF application with centralized MariaDB via XAMPP — required by professor.

- Offline-first: all transactions committed to local SQLite; auto-sync to MariaDB on stable dual-condition connection.

- Modular Monolith: four Class Libraries with no cross-module internal data access; inter-module communication via MediatR only.

- FIFO inventory costing method confirmed by the client.

- Expiry date tracking required for pesticides/chemicals, seeds, and animal feeds.

- All Accounting module reports must include plain-language interpretation, analytics, and visual graphs — mandatory, not optional.

- Owner receives in-app WPF overlay notifications and has strictly read-only access to all four module KPIs.

- OWASP Desktop Application Security Top 10 (2021) fully addressed in implementation.

- Third Normal Form (3NF) database design with referential integrity enforced through foreign keys.

- Structured audit trail on all financial record changes for BIR compliance and internal oversight.

- Module interfaces designed from the start to allow extraction into independent microservices post-prototype.

- Simplicity over complexity — explicit design principle; no over-engineering.
