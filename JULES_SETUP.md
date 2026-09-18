# 24/7 Agent & Jules AI Control Center

This workspace serves as the command-and-control center for orchestrating **Jules AI** via Antigravity to work on the **VISTA Project**. We have established a comprehensive setup spanning customizations, knowledge management, and strict project constraints.

## 1. Antigravity Configuration (`.agents/`)
We have configured Antigravity with custom rules and skills to effectively manage Jules and enforce architectural standards:
- **Rules (`.agents/rules/`)**:
  - `architecture-winforms-mvp.md` and `orchestration-workflow.md` enforce strict adherence to the WinForms MVP pattern and dictate how workflows should be orchestrated.
- **Skills (`.agents/skills/`)**:
  - `jules-manager`: Teaches Antigravity how to launch tasks, monitor progress, and pull results using the `@google/jules` CLI.
  - `jules-docs-vault`: Allows Antigravity to query the local Obsidian vault to answer complex questions about Jules.

## 2. Knowledge Vault (`Knowledge_Vault/`)
A centralized repository of documentation and domain logic, ensuring both you and the AI have the necessary context:
- **`Jules_AI/`**: A full local Obsidian knowledge base covering Jules' API, CLI, Integrations, and Release History.
- **`VISTA_Domain/`**: Deep context on the VISTA business domain, organized into `concepts`, `entities`, and `sources` (e.g., centralized DB architecture, FIFO costing).
- **`Rules/` & `Skills/`**: Shared documentation on the orchestration workflows and architecture patterns.

## 3. VISTA Project (`VISTA_Project/`)
The primary target repository for Jules AI, pre-configured with strict instructions and a detailed roadmap:
- **`AGENTS.md`**: Contains CRITICAL instructions for Jules. It mandates the use of **Windows Forms (MVP)** and **VB.NET 10**, explicitly banning WPF/XAML. It also details the database architecture (MariaDB 11.4.x LTS), MediatR event contracts, security requirements, and known VB.NET build traps.
- **`Roadmap.md`**: A comprehensive 7-phase execution plan covering everything from Infrastructure to Accounting and UX, broken down into hundreds of trackable tasks.
- **`Deferred_Tasks.md`**: A central ledger auto-populated by the 24/7 Agent to track tasks that Jules fails or defers due to spec issues, awaiting your manual verification.
- **`.github/workflows/jules-agent-trigger.yml`**: A GitHub Action that triggers the local 24/7 Antigravity listener whenever Jules completes a PR.
- **Directory Structure**: Includes `Specs/` (business logic requirements), `docs/` (architectural source of truth), and `WinForms_Applications/` (target for the migration).

## 4. 24/7 Autonomous Orchestration
We have configured a fully event-driven workflow where Jules and Antigravity work together seamlessly:
1. Jules runs its headless integration tests (via the installed MariaDB service in its Ubuntu VM).
2. When Jules finishes a task and opens a PR, the **GitHub Action** fires, sending a webhook payload (via a secure tunnel like ngrok or Cloudflare Tunnel) to your local machine.
3. A local **Python HTTP server** listener receives the webhook and automatically uses the Antigravity CLI (`agy`) to spawn an agent. The agent analyzes the output against the `Specs/`, logs issues in `Deferred_Tasks.md`, and **pauses for your manual verification** before continuing to the next session.

## Capabilities & Usage
By leveraging this setup, you can ask Antigravity to manage Jules entirely through natural language. 

**Example Prompts:**
- *"Ask Jules to start working on INFRA-01 from the Roadmap, keeping the VB.NET build traps in mind."*
- *"Check the Jules Docs Vault to see how we can trigger Jules via GitHub Actions."*
- *"Pull down Jules' latest PR for the Purchasing module so I can review it against the MVP architecture rules."*
- *"Write a lightweight Python FastAPI server to listen for GitHub Action webhooks on port 8080. When triggered, have it execute the `agy` CLI to spawn an agent that verifies Jules' PR against Specs/ and logs any errors in Deferred_Tasks.md for my review."*
