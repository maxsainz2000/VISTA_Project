Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Accounting.Entities

Namespace Data.Configurations

    Public Class TamperAuditEntryConfiguration
        Implements IEntityTypeConfiguration(Of TamperAuditEntry)

        Public Sub Configure(builder As EntityTypeBuilder(Of TamperAuditEntry)) Implements IEntityTypeConfiguration(Of TamperAuditEntry).Configure

            builder.ToTable("Acc_TamperAuditLog")

            builder.HasKey(Function(e) e.Id)
            builder.Property(Function(e) e.Id).ValueGeneratedOnAdd()

            builder.Property(Function(e) e.DetectedAt).IsRequired()
            builder.Property(Function(e) e.ReceiptId).IsRequired()
            builder.Property(Function(e) e.ReceiptNumber).IsRequired().HasMaxLength(50)
            builder.Property(Function(e) e.TamperKind).IsRequired().HasMaxLength(50)
            builder.Property(Function(e) e.DetectedByService).IsRequired().HasMaxLength(200)
            builder.Property(Function(e) e.ExpectedValue).HasMaxLength(512)
            builder.Property(Function(e) e.ActualValue).HasMaxLength(512)
            builder.Property(Function(e) e.AdditionalContextJson)
            builder.Property(Function(e) e.MachineName).IsRequired().HasMaxLength(256)
            builder.Property(Function(e) e.OperatingUser).IsRequired().HasMaxLength(256)
            builder.Property(Function(e) e.CreatedAt).IsRequired()
            builder.Property(Function(e) e.CreatedBy).IsRequired().HasMaxLength(256)

            ' Supports time-windowed queries filtered by incident type (common investigator access pattern)
            builder.HasIndex(Function(e) New With {e.DetectedAt, e.TamperKind}).HasDatabaseName("IX_Acc_TamperAuditLog_DetectedAt_TamperKind")

        End Sub

    End Class

End Namespace
