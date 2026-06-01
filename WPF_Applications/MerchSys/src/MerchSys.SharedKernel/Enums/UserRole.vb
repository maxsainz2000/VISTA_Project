Namespace Enums

    ''' <summary>
    ''' Defines the user roles in the VISTA system.
    ''' Role enforcement is applied at the data layer, not just the UI.
    ''' </summary>
    Public Enum UserRole

        ''' <summary>Full CRUD access to all operational features across all modules.</summary>
        Manager = 1

        ''' <summary>Read-only access to dashboards, KPIs, and financial reports.</summary>
        Owner = 2

        ''' <summary>
        ''' Full operational access (superset of Manager) plus exclusive access to the
        ''' Developer Tools module. Reserved for the single internal developer account.
        ''' </summary>
        Developer = 3

    End Enum

End Namespace
