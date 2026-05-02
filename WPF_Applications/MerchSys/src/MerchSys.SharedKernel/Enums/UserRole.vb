Namespace Enums

    ''' <summary>
    ''' Defines the two user roles in the VISTA system.
    ''' Role enforcement is applied at the data layer, not just the UI.
    ''' </summary>
    Public Enum UserRole

        ''' <summary>Full CRUD access to all operational features across all modules.</summary>
        Manager = 1

        ''' <summary>Read-only access to dashboards, KPIs, and financial reports.</summary>
        Owner = 2

    End Enum

End Namespace
