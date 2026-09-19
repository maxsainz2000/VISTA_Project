Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Accounting.Entities

Namespace Data

    ''' <summary>
    ''' Partial-class extension adding VAT-ledger DbSets to <see cref="AccountingDbContext"/>
    ''' without modifying the original ACC-02 source file.
    ''' </summary>
    Partial Public Class AccountingDbContext

        Public Property VatReturns As DbSet(Of VatReturn)
        Public Property VatReturnLines As DbSet(Of VatReturnLine)

    End Class

End Namespace
