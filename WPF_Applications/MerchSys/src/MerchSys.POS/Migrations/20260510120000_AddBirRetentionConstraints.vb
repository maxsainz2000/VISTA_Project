Imports System
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Migrations

Namespace Migrations

    ''' <summary>
    ''' Adds BIR tamper-proof receipt retention tables and SQLite-level immutability triggers.
    ''' Implements NIRC §113 (issuance sequence controls) and §235 (10-year tamper-proof preservation).
    ''' See: LLM_Wiki/wiki/concepts/bir-compliance.md
    ''' </summary>
    <MigrationAttribute("20260510120000_AddBirRetentionConstraints")>
    Public Class AddBirRetentionConstraints
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)

            ' Per-year sequence table for gap-free OR number generation (NIRC §113)
            migrationBuilder.Sql(
                "CREATE TABLE ""Pos_ReceiptSequence"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pos_ReceiptSequence"" PRIMARY KEY AUTOINCREMENT, " &
                """Year"" INTEGER NOT NULL, " &
                """NextValue"" INTEGER NOT NULL, " &
                """RowVersion"" BLOB NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")

            migrationBuilder.Sql("CREATE UNIQUE INDEX ""IX_Pos_ReceiptSequence_Year"" ON ""Pos_ReceiptSequence"" (""Year"")")

            ' SHA-256 hash chain sidecar table (NIRC §113, §235)
            migrationBuilder.Sql(
                "CREATE TABLE ""Pos_ReceiptIntegrity"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pos_ReceiptIntegrity"" PRIMARY KEY AUTOINCREMENT, " &
                """ReceiptId"" INTEGER NOT NULL, " &
                """IntegrityHash"" TEXT NOT NULL, " &
                """PreviousHash"" TEXT NOT NULL, " &
                """RetentionExpiresAt"" TEXT NOT NULL, " &
                """IsImmutable"" INTEGER NOT NULL, " &
                """HashAlgorithm"" TEXT NOT NULL, " &
                """CanonicalPayload"" TEXT NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL, " &
                "CONSTRAINT ""FK_Pos_ReceiptIntegrity_Pos_OfficialReceipts_ReceiptId"" " &
                "FOREIGN KEY (""ReceiptId"") REFERENCES ""Pos_OfficialReceipts"" (""Id"")" &
                ")")

            migrationBuilder.Sql("CREATE UNIQUE INDEX ""IX_Pos_ReceiptIntegrity_ReceiptId"" ON ""Pos_ReceiptIntegrity"" (""ReceiptId"")")
            migrationBuilder.Sql("CREATE INDEX ""IX_Pos_ReceiptIntegrity_RetentionExpiresAt"" ON ""Pos_ReceiptIntegrity"" (""RetentionExpiresAt"")")

            ' Cold-storage archive — copy destination after GraceDays expires (NIRC §235)
            migrationBuilder.Sql(
                "CREATE TABLE ""Pos_OfficialReceiptArchive"" (" &
                """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pos_OfficialReceiptArchive"" PRIMARY KEY AUTOINCREMENT, " &
                """OriginalReceiptId"" INTEGER NOT NULL, " &
                """TransactionId"" INTEGER NOT NULL, " &
                """ReceiptNumber"" TEXT NOT NULL, " &
                """BusinessName"" TEXT NOT NULL, " &
                """BusinessAddress"" TEXT NULL, " &
                """BusinessTIN"" TEXT NULL, " &
                """IssueDate"" TEXT NOT NULL, " &
                """Items"" TEXT NULL, " &
                """TotalAmount"" TEXT NOT NULL, " &
                """VatAmount"" TEXT NOT NULL, " &
                """IsVatRegistered"" INTEGER NOT NULL, " &
                """ArchivedAt"" TEXT NOT NULL, " &
                """ArchivedHash"" TEXT NOT NULL, " &
                """CreatedBy"" TEXT NULL, " &
                """CreatedAt"" TEXT NOT NULL, " &
                """ModifiedBy"" TEXT NULL, " &
                """ModifiedAt"" TEXT NULL" &
                ")")

            migrationBuilder.Sql("CREATE INDEX ""IX_Pos_OfficialReceiptArchive_OriginalReceiptId"" ON ""Pos_OfficialReceiptArchive"" (""OriginalReceiptId"")")

            ' SQLite-level immutability triggers — defense-in-depth alongside ImmutableReceiptInterceptor
            ' For MariaDB, equivalent triggers are defined in INFRA-06 mariadb-init.sql
            migrationBuilder.Sql(
                "CREATE TRIGGER pos_receipts_no_update " &
                "BEFORE UPDATE ON ""Pos_OfficialReceipts"" " &
                "BEGIN " &
                "SELECT RAISE(ABORT, 'BIR-immutable'); " &
                "END")

            migrationBuilder.Sql(
                "CREATE TRIGGER pos_receipts_no_delete " &
                "BEFORE DELETE ON ""Pos_OfficialReceipts"" " &
                "BEGIN " &
                "SELECT RAISE(ABORT, 'BIR-immutable'); " &
                "END")

        End Sub

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS pos_receipts_no_delete")
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS pos_receipts_no_update")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pos_OfficialReceiptArchive""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pos_ReceiptIntegrity""")
            migrationBuilder.Sql("DROP TABLE IF EXISTS ""Pos_ReceiptSequence""")
        End Sub

    End Class

End Namespace
