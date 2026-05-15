Imports System
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Migrations

Namespace Migrations

    ''' <summary>
    ''' Creates <c>Pos_ReceiptIntegrityArchive</c> as an INSERT-only mirror of
    ''' <c>Pos_ReceiptIntegrity</c>, adds INSERT-only triggers to
    ''' <c>Pos_OfficialReceiptArchive</c>, and amends the POS-13 delete trigger on
    ''' <c>Pos_OfficialReceipts</c> to permit DELETE when the archival session flag
    ''' (<c>temp.archival_session.archival_in_progress = 1</c>) is set by
    ''' <c>ReceiptArchivalService</c>.
    ''' <para>
    ''' <b>Trigger amendment (Option 1 from POS-16 plan):</b>
    ''' The POS-13 trigger <c>pos_receipts_no_delete</c> previously raised ABORT
    ''' unconditionally. The amended trigger adds a WHEN clause that checks a
    ''' connection-scoped temp table.  Absent the archival flag the DELETE is still
    ''' blocked — the BIR tamper-proof guarantee is preserved.
    ''' </para>
    ''' <para>
    ''' MariaDB equivalent triggers are scoped to INFRA-08 and are out of scope here.
    ''' </para>
    ''' </summary>
    <MigrationAttribute("20260515140000_AddReceiptIntegrityArchive")>
    Public Class AddReceiptIntegrityArchive
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)

            ' ── Pos_ReceiptIntegrityArchive ───────────────────────────────────
            migrationBuilder.Sql(
                "CREATE TABLE IF NOT EXISTS ""Pos_ReceiptIntegrityArchive"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pos_ReceiptIntegrityArchive"" PRIMARY KEY AUTOINCREMENT, " &
                """OriginalIntegrityId"" INTEGER NOT NULL, " &
                """ReceiptId"" INTEGER NOT NULL, " &
                """IntegrityHash"" TEXT NOT NULL, " &
                """PreviousHash"" TEXT NOT NULL, " &
                """RetentionExpiresAt"" TEXT NOT NULL, " &
                """IsImmutable"" INTEGER NOT NULL, " &
                """HashAlgorithm"" TEXT NOT NULL, " &
                """CanonicalPayload"" TEXT NOT NULL, " &
                """ArchivedAt"" TEXT NOT NULL, " &
                """ArchivedByService"" TEXT NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ""IX_Pos_ReceiptIntegrityArchive_ReceiptId"" " &
                "ON ""Pos_ReceiptIntegrityArchive"" (""ReceiptId"")")

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ""IX_Pos_ReceiptIntegrityArchive_OriginalIntegrityId"" " &
                "ON ""Pos_ReceiptIntegrityArchive"" (""OriginalIntegrityId"")")

            ' INSERT-only triggers — NIRC §235 tamper-proof archive integrity
            migrationBuilder.Sql(
                "CREATE TRIGGER IF NOT EXISTS pos_integrity_archive_no_update " &
                "BEFORE UPDATE ON ""Pos_ReceiptIntegrityArchive"" " &
                "BEGIN " &
                "SELECT RAISE(ABORT, 'BIR-archive-immutable'); " &
                "END")

            migrationBuilder.Sql(
                "CREATE TRIGGER IF NOT EXISTS pos_integrity_archive_no_delete " &
                "BEFORE DELETE ON ""Pos_ReceiptIntegrityArchive"" " &
                "BEGIN " &
                "SELECT RAISE(ABORT, 'BIR-archive-immutable'); " &
                "END")

            ' ── INSERT-only triggers on Pos_OfficialReceiptArchive ────────────
            ' These were absent in migration 20260510120000_AddBirRetentionConstraints.
            migrationBuilder.Sql(
                "CREATE TRIGGER IF NOT EXISTS pos_receipt_archive_no_update " &
                "BEFORE UPDATE ON ""Pos_OfficialReceiptArchive"" " &
                "BEGIN " &
                "SELECT RAISE(ABORT, 'BIR-archive-immutable'); " &
                "END")

            migrationBuilder.Sql(
                "CREATE TRIGGER IF NOT EXISTS pos_receipt_archive_no_delete " &
                "BEFORE DELETE ON ""Pos_OfficialReceiptArchive"" " &
                "BEGIN " &
                "SELECT RAISE(ABORT, 'BIR-archive-immutable'); " &
                "END")

            ' ── Amend pos_receipts_no_delete (POS-13 trigger) ────────────────
            '
            ' BEFORE (POS-13, 20260510120000):
            '   CREATE TRIGGER pos_receipts_no_delete
            '   BEFORE DELETE ON "Pos_OfficialReceipts"
            '   BEGIN SELECT RAISE(ABORT, 'BIR-immutable'); END
            '
            ' AFTER (this migration):
            '   CREATE TRIGGER pos_receipts_no_delete
            '   BEFORE DELETE ON "Pos_OfficialReceipts"
            '   WHEN (SELECT COALESCE((SELECT value FROM temp.archival_session
            '                          WHERE key = 'archival_in_progress'), 0) = 0)
            '   BEGIN SELECT RAISE(ABORT, 'BIR-immutable'); END
            '
            ' The WHEN clause is FALSE (trigger body skipped) only when the archival
            ' service has set archival_in_progress=1 in the connection-scoped temp table.
            ' Any normal DELETE from any other session leaves the temp table absent,
            ' COALESCE returns 0, the condition evaluates to TRUE, and the RAISE fires.
            '
            ' Option 1 was chosen (session-variable path) per the POS-16 plan.
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS pos_receipts_no_delete")

            migrationBuilder.Sql(
                "CREATE TRIGGER pos_receipts_no_delete " &
                "BEFORE DELETE ON ""Pos_OfficialReceipts"" " &
                "WHEN (SELECT COALESCE((SELECT value FROM temp.archival_session " &
                "WHERE key = 'archival_in_progress'), 0) = 0) " &
                "BEGIN " &
                "SELECT RAISE(ABORT, 'BIR-immutable'); " &
                "END")

        End Sub

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)
            ' Restore the unconditional delete-blocking trigger
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS pos_receipts_no_delete")
            migrationBuilder.Sql(
                "CREATE TRIGGER pos_receipts_no_delete " &
                "BEFORE DELETE ON ""Pos_OfficialReceipts"" " &
                "BEGIN " &
                "SELECT RAISE(ABORT, 'BIR-immutable'); " &
                "END")

            migrationBuilder.Sql("DROP TRIGGER IF EXISTS pos_receipt_archive_no_delete")
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS pos_receipt_archive_no_update")
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS pos_integrity_archive_no_delete")
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS pos_integrity_archive_no_update")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pos_ReceiptIntegrityArchive""")
        End Sub

    End Class

End Namespace
