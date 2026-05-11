Imports Microsoft.EntityFrameworkCore.Migrations

' ACC-15: Adds the Acc_TamperAuditLog table for receipt tamper incident recording.
' This is the local-side (SQLite) schema. MariaDB-equivalent immutability triggers
' are in scope for INFRA-08 (central-side tamper-table immutability). When INFRA-08
' is implemented, use this file's trigger SQL as the reference and adjust for MariaDB
' DDL syntax (SIGNAL SQLSTATE instead of RAISE(ABORT, ...)).

Namespace Migrations

    <MigrationAttribute("20260516100000_AddTamperAuditLog")>
    Public Class AddTamperAuditLog
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)

            migrationBuilder.Sql(
                "CREATE TABLE ""Acc_TamperAuditLog"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Acc_TamperAuditLog"" PRIMARY KEY AUTOINCREMENT, " &
                """DetectedAt"" TEXT NOT NULL, " &
                """ReceiptId"" INTEGER NOT NULL, " &
                """ReceiptNumber"" TEXT NOT NULL, " &
                """TamperKind"" TEXT NOT NULL, " &
                """DetectedByService"" TEXT NOT NULL, " &
                """ExpectedValue"" TEXT NULL, " &
                """ActualValue"" TEXT NULL, " &
                """AdditionalContextJson"" TEXT NULL, " &
                """MachineName"" TEXT NOT NULL, " &
                """OperatingUser"" TEXT NOT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """CreatedBy"" TEXT NOT NULL" &
                ")")

            migrationBuilder.Sql(
                "CREATE INDEX ""IX_Acc_TamperAuditLog_DetectedAt_TamperKind"" " &
                "ON ""Acc_TamperAuditLog"" (""DetectedAt"", ""TamperKind"")")

            ' INSERT-permissive, UPDATE-blocking trigger — mirrors POS-13 pattern on Pos_ReceiptIntegrity.
            ' INFRA-08 cross-reference: MariaDB-equivalent triggers for the central replica belong to INFRA-08 scope.
            migrationBuilder.Sql(
                "CREATE TRIGGER acc_tamper_audit_no_update " &
                "BEFORE UPDATE ON ""Acc_TamperAuditLog"" " &
                "BEGIN " &
                "SELECT RAISE(ABORT, 'tamper-audit-immutable'); " &
                "END")

            ' DELETE-blocking trigger — tamper records must be retained for BIR §235 (10-year retention).
            migrationBuilder.Sql(
                "CREATE TRIGGER acc_tamper_audit_no_delete " &
                "BEFORE DELETE ON ""Acc_TamperAuditLog"" " &
                "BEGIN " &
                "SELECT RAISE(ABORT, 'tamper-audit-immutable'); " &
                "END")

        End Sub

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS acc_tamper_audit_no_delete")
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS acc_tamper_audit_no_update")
            migrationBuilder.Sql("DROP INDEX IF EXISTS ""IX_Acc_TamperAuditLog_DetectedAt_TamperKind""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Acc_TamperAuditLog""")
        End Sub

    End Class

End Namespace
