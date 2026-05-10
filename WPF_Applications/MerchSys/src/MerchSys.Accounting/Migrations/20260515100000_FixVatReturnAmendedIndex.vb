Imports Microsoft.EntityFrameworkCore.Migrations

' ACC-11: Replaces the full unique index on Acc_VatReturns (Year, Period, PeriodType, FormType)
' with a partial unique index that excludes Amended rows (FilingStatus = 3).
' This allows AmendReturnAsync to create a second row for the same period without
' violating the constraint, while still preventing accidental duplicate
' Generated/Filed rows for the same filing period.

Namespace Migrations

    <MigrationAttribute("20260515100000_FixVatReturnAmendedIndex")>
    Public Class FixVatReturnAmendedIndex
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS ""IX_Acc_VatReturns_Year_Period_PeriodType_FormType""")

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ""IX_Acc_VatReturns_Year_Period_PeriodType_FormType_Active"" " &
                "ON ""Acc_VatReturns"" (""Year"", ""Period"", ""PeriodType"", ""FormType"") " &
                "WHERE ""FilingStatus"" != 3")
        End Sub

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS ""IX_Acc_VatReturns_Year_Period_PeriodType_FormType_Active""")

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ""IX_Acc_VatReturns_Year_Period_PeriodType_FormType"" " &
                "ON ""Acc_VatReturns"" (""Year"", ""Period"", ""PeriodType"", ""FormType"")")
        End Sub

    End Class

End Namespace
