Imports System
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Migrations

' Backfill strategy (non-destructive):
'   All pre-existing revenue and expense rows are classified as Vatable (VatTreatment = 0).
'   VatableAmount is set to the row's primary monetary field (NetAmount for revenue,
'   Amount for expenses).  VatExemptAmount, ZeroRatedAmount, OutputVat, and InputVat
'   default to 0.  ACC-11 recalculates precise VAT figures when generating returns.
'
' Tables affected by new columns:
'   Acc_RevenueRecords  — 6 columns added (VatableAmount, VatExemptAmount, ZeroRatedAmount,
'                         OutputVat, InputVat, VatTreatment)
'   Acc_ExpenseRecords  — same 6 columns added
'
' New tables created:
'   Acc_VatReturns      — one row per BIR filing period/form
'   Acc_VatReturnLines  — one row per source document line (FK to Acc_VatReturns, CASCADE)

Namespace Migrations

    <MigrationAttribute("20260510100000_AddVatLedgerColumns")>
    Public Class AddVatLedgerColumns
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)

            ' ── Acc_RevenueRecords: add VAT columns ──────────────────────────────
            migrationBuilder.Sql("ALTER TABLE ""Acc_RevenueRecords"" ADD COLUMN ""VatableAmount"" TEXT NOT NULL DEFAULT '0'")
            migrationBuilder.Sql("ALTER TABLE ""Acc_RevenueRecords"" ADD COLUMN ""VatExemptAmount"" TEXT NOT NULL DEFAULT '0'")
            migrationBuilder.Sql("ALTER TABLE ""Acc_RevenueRecords"" ADD COLUMN ""ZeroRatedAmount"" TEXT NOT NULL DEFAULT '0'")
            migrationBuilder.Sql("ALTER TABLE ""Acc_RevenueRecords"" ADD COLUMN ""OutputVat"" TEXT NOT NULL DEFAULT '0'")
            migrationBuilder.Sql("ALTER TABLE ""Acc_RevenueRecords"" ADD COLUMN ""InputVat"" TEXT NOT NULL DEFAULT '0'")
            migrationBuilder.Sql("ALTER TABLE ""Acc_RevenueRecords"" ADD COLUMN ""VatTreatment"" INTEGER NOT NULL DEFAULT 0")

            ' Backfill: classify existing revenue rows as Vatable; set VatableAmount = NetAmount.
            migrationBuilder.Sql("UPDATE ""Acc_RevenueRecords"" SET ""VatableAmount"" = ""NetAmount"" WHERE ""VatableAmount"" = '0'")

            ' ── Acc_ExpenseRecords: add VAT columns ───────────────────────────────
            migrationBuilder.Sql("ALTER TABLE ""Acc_ExpenseRecords"" ADD COLUMN ""VatableAmount"" TEXT NOT NULL DEFAULT '0'")
            migrationBuilder.Sql("ALTER TABLE ""Acc_ExpenseRecords"" ADD COLUMN ""VatExemptAmount"" TEXT NOT NULL DEFAULT '0'")
            migrationBuilder.Sql("ALTER TABLE ""Acc_ExpenseRecords"" ADD COLUMN ""ZeroRatedAmount"" TEXT NOT NULL DEFAULT '0'")
            migrationBuilder.Sql("ALTER TABLE ""Acc_ExpenseRecords"" ADD COLUMN ""OutputVat"" TEXT NOT NULL DEFAULT '0'")
            migrationBuilder.Sql("ALTER TABLE ""Acc_ExpenseRecords"" ADD COLUMN ""InputVat"" TEXT NOT NULL DEFAULT '0'")
            migrationBuilder.Sql("ALTER TABLE ""Acc_ExpenseRecords"" ADD COLUMN ""VatTreatment"" INTEGER NOT NULL DEFAULT 0")

            ' Backfill: classify existing expense rows as Vatable; set VatableAmount = Amount.
            migrationBuilder.Sql("UPDATE ""Acc_ExpenseRecords"" SET ""VatableAmount"" = ""Amount"" WHERE ""VatableAmount"" = '0'")

            ' ── Acc_VatReturns ───────────────────────────────────────────────────
            migrationBuilder.Sql(
                "CREATE TABLE ""Acc_VatReturns"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Acc_VatReturns"" PRIMARY KEY AUTOINCREMENT, " &
                """Year"" INTEGER NOT NULL, " &
                """Period"" INTEGER NOT NULL, " &
                """PeriodType"" INTEGER NOT NULL, " &
                """FormType"" INTEGER NOT NULL, " &
                """TotalVatableSales"" TEXT NOT NULL, " &
                """TotalVatExemptSales"" TEXT NOT NULL, " &
                """TotalZeroRatedSales"" TEXT NOT NULL, " &
                """TotalOutputVat"" TEXT NOT NULL, " &
                """TotalVatablePurchases"" TEXT NOT NULL, " &
                """TotalInputVat"" TEXT NOT NULL, " &
                """VatPayable"" TEXT NOT NULL, " &
                """FilingStatus"" INTEGER NOT NULL, " &
                """FiledAt"" TEXT NULL, " &
                """FiledBy"" TEXT NULL, " &
                """GeneratedAt"" TEXT NOT NULL, " &
                """IsVatRegisteredSnapshot"" INTEGER NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ""IX_Acc_VatReturns_Year_Period_PeriodType_FormType"" " &
                "ON ""Acc_VatReturns"" (""Year"", ""Period"", ""PeriodType"", ""FormType"")")

            ' ── Acc_VatReturnLines ────────────────────────────────────────────────
            migrationBuilder.Sql(
                "CREATE TABLE ""Acc_VatReturnLines"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Acc_VatReturnLines"" PRIMARY KEY AUTOINCREMENT, " &
                """VatReturnId"" INTEGER NOT NULL, " &
                """SourceModule"" TEXT NOT NULL, " &
                """SourceTable"" TEXT NOT NULL, " &
                """SourceRowId"" INTEGER NOT NULL, " &
                """TransactionDate"" TEXT NOT NULL, " &
                """VatableAmount"" TEXT NOT NULL, " &
                """VatExemptAmount"" TEXT NOT NULL, " &
                """ZeroRatedAmount"" TEXT NOT NULL, " &
                """OutputVat"" TEXT NOT NULL, " &
                """InputVat"" TEXT NOT NULL, " &
                """Treatment"" INTEGER NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL, " &
                "CONSTRAINT ""FK_Acc_VatReturnLines_Acc_VatReturns_VatReturnId"" " &
                "FOREIGN KEY (""VatReturnId"") REFERENCES ""Acc_VatReturns"" (""Id"") ON DELETE CASCADE" &
                ")")

            migrationBuilder.Sql(
                "CREATE INDEX ""IX_Acc_VatReturnLines_VatReturnId"" " &
                "ON ""Acc_VatReturnLines"" (""VatReturnId"")")

            migrationBuilder.Sql(
                "CREATE INDEX ""IX_Acc_VatReturnLines_SourceModule_SourceTable_SourceRowId"" " &
                "ON ""Acc_VatReturnLines"" (""SourceModule"", ""SourceTable"", ""SourceRowId"")")

        End Sub

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)

            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Acc_VatReturnLines""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Acc_VatReturns""")

            migrationBuilder.Sql("ALTER TABLE ""Acc_RevenueRecords"" DROP COLUMN ""VatTreatment""")
            migrationBuilder.Sql("ALTER TABLE ""Acc_RevenueRecords"" DROP COLUMN ""InputVat""")
            migrationBuilder.Sql("ALTER TABLE ""Acc_RevenueRecords"" DROP COLUMN ""OutputVat""")
            migrationBuilder.Sql("ALTER TABLE ""Acc_RevenueRecords"" DROP COLUMN ""ZeroRatedAmount""")
            migrationBuilder.Sql("ALTER TABLE ""Acc_RevenueRecords"" DROP COLUMN ""VatExemptAmount""")
            migrationBuilder.Sql("ALTER TABLE ""Acc_RevenueRecords"" DROP COLUMN ""VatableAmount""")

            migrationBuilder.Sql("ALTER TABLE ""Acc_ExpenseRecords"" DROP COLUMN ""VatTreatment""")
            migrationBuilder.Sql("ALTER TABLE ""Acc_ExpenseRecords"" DROP COLUMN ""InputVat""")
            migrationBuilder.Sql("ALTER TABLE ""Acc_ExpenseRecords"" DROP COLUMN ""OutputVat""")
            migrationBuilder.Sql("ALTER TABLE ""Acc_ExpenseRecords"" DROP COLUMN ""ZeroRatedAmount""")
            migrationBuilder.Sql("ALTER TABLE ""Acc_ExpenseRecords"" DROP COLUMN ""VatExemptAmount""")
            migrationBuilder.Sql("ALTER TABLE ""Acc_ExpenseRecords"" DROP COLUMN ""VatableAmount""")

        End Sub

    End Class

End Namespace
