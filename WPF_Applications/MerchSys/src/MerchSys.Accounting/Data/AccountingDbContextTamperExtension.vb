Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Accounting.Entities

Namespace Data

    ''' <summary>
    ''' Partial-class extension adding the tamper audit log DbSet to <see cref="AccountingDbContext"/>
    ''' without modifying the original ACC-02 source file.
    ''' Added by ACC-15.
    ''' </summary>
    Partial Public Class AccountingDbContext

        Public Property TamperAuditEntries As DbSet(Of TamperAuditEntry)

    End Class

End Namespace
