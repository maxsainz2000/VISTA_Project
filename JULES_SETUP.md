# 24/7 Agent & Jules AI Control Center

This workspace serves as the command-and-control center for orchestrating **Jules AI** via Antigravity to work on the **VISTA Project**. We have established a comprehensive setup spanning customizations, knowledge management, and strict project constraints.

## 1. Antigravity Configuration (`.agents/`)
We have configured Antigravity with custom rules and skills to effectively manage Jules and enforce architectural standards:
- **Rules**:
  - `Knowledge_Vault/Rules/orchestration-workflow.md` dictates how workflows should be orchestrated.
  - `VISTA_Project/docs/rules/architecture-winforms-mvp.md` enforces strict adherence to the WinForms MVP pattern.
- **Skills (`.agents/skills/`)**:
  - `jules-manager`: Teaches Antigravity how to launch tasks, monitor progress, and pull results using the `@google/jules` CLI.
  - `jules-docs-vault`: Allows Antigravity to query the local Obsidian vault to answer complex questions about Jules.

## 2. Knowledge Vault (`Knowledge_Vault/`)
A centralized repository of documentation and domain logic, ensuring both you and the AI have the necessary context:
- **`Jules_AI/`**: A full local Obsidian knowledge base covering Jules' API, CLI, Integrations, and Release History.
- **`Rules/` & `Skills/`**: Shared documentation on the orchestration workflows and architecture patterns.

## 3. VISTA Project (`VISTA_Project/`)
The primary target repository for Jules AI, pre-configured with strict instructions and a detailed roadmap:
- **`docs/domain/`**: Deep context on the VISTA business domain, organized into `concepts`, `entities`, and `sources` (e.g., centralized DB architecture, FIFO costing).
- **`AGENTS.md`**: Contains CRITICAL instructions for Jules. It mandates the use of **Windows Forms (MVP)** and **VB.NET 10**, explicitly banning WPF/XAML. It also details the database architecture (MariaDB 11.4.x LTS), MediatR event contracts, security requirements, and known VB.NET build traps.
- **`Roadmap.md`**: A comprehensive 7-phase execution plan covering everything from Infrastructure to Accounting and UX, broken down into hundreds of trackable tasks.
- **`Deferred_Tasks.md`**: A central ledger auto-populated by the 24/7 Agent to track tasks that Jules fails or defers due to spec issues, awaiting your manual verification.
- **Directory Structure**: Includes `Specs/` (business logic requirements), `docs/` (architectural source of truth), and `WinForms_Applications/` (target for the migration).

## 4. 24/7 Autonomous Orchestration
We have configured a fully automated workflow where Jules and Antigravity work together seamlessly via the `jules-teamwork` skill:
1. Jules runs its headless integration tests (via the installed MariaDB service in its Ubuntu VM).
2. The `jules-teamwork` skill executes a supervised sequential burst of up to 100 sessions. Antigravity orchestrates, verifies, and manages docs, while Jules strictly codes.
3. The agent analyzes the output against the `Specs/`, logs issues in `Deferred_Tasks.md`, and loops synchronously.

## Capabilities & Usage
By leveraging this setup, you can ask Antigravity to manage Jules entirely through natural language. 

**Example Prompts:**
- *"Ask Jules to start working on INFRA-01 from the Roadmap, keeping the VB.NET build traps in mind."*
- *"Check the Jules Docs Vault to see how we can trigger Jules via GitHub Actions."*
- *"Pull down Jules' latest PR for the Purchasing module so I can review it against the MVP architecture rules."*
- *"Run a /jules-teamwork loop for the VISTA project to execute the roadmap."*
