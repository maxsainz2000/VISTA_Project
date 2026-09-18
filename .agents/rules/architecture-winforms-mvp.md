# VISTA Architecture: WinForms Model-View-Presenter (MVP)

This rule establishes the architectural standard for the VISTA Project (Windows Forms rewrite).

## The Core Rule

When implementing UI in the VISTA Project, you MUST strictly use the **Model-View-Presenter (MVP)** pattern. 
Never put business logic directly in the code-behind of the Forms.

### 1. Model
- Handles all database interactions (via Entity Framework Core and MariaDB) and business logic.
- Must not have any reference to `System.Windows.Forms` or any UI concerns.

### 2. View
- The WinForms Designer code (`.cs` or `.vb`) and the code-behind.
- The View MUST implement an `IView` interface (e.g. `IPurchasingDashboardView`) to abstract its controls.
- The View exposes its UI state via properties and exposes user actions via events (e.g. `SaveButtonClicked`).
- The View is completely passive. It only handles routing events to the Presenter and updating its controls when commanded by the Presenter.

### 3. Presenter
- Acts as the orchestrator.
- Takes the `IView` and any required `Services`/`Models` as dependencies in its constructor.
- Subscribes to the View's events.
- Updates the View via the `IView` interface methods.
- The Presenter contains all presentation logic but NO references to `System.Windows.Forms`. This makes the presentation logic unit-testable.

## Why MVP?
Because typical Windows Forms encourages heavy code-behind files ("Smart UI" anti-pattern). We use MVP to enforce separation of concerns, testability, and a clear migration path to future web or mobile implementations.
