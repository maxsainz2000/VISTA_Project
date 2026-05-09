Imports System
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports MerchSys.POS.Data

Namespace Migrations

    <DbContext(GetType(POSDbContext))>
    Partial Class POSDbContextModelSnapshot
        Inherits ModelSnapshot

        Protected Overrides Sub BuildModel(modelBuilder As ModelBuilder)
            modelBuilder.HasAnnotation("ProductVersion", "10.0.7")

            modelBuilder.Entity("MerchSys.POS.Entities.CreditAccount", Sub(b)
                b.Property(Of Integer)("Id").
                    ValueGeneratedOnAdd().
                    HasColumnType("INTEGER")
                b.Property(Of String)("Address").HasMaxLength(500).HasColumnType("TEXT")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Decimal)("CurrentBalance").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of DateTime?)("DeletedAt").HasColumnType("TEXT")
                b.Property(Of String)("DeletedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Boolean)("IsBlocked").HasColumnType("INTEGER")
                b.Property(Of Boolean)("IsDeleted").HasColumnType("INTEGER")
                b.Property(Of String)("CustomerName").
                    IsRequired().
                    HasMaxLength(200).
                    HasColumnType("TEXT")
                b.Property(Of DateTime?)("LastTransactionDate").HasColumnType("TEXT")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of String)("Notes").HasMaxLength(1000).HasColumnType("TEXT")
                b.Property(Of String)("Phone").HasMaxLength(50).HasColumnType("TEXT")
                b.Property(Of Decimal)("TotalCreditExtended").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of Decimal)("TotalPaymentsReceived").HasPrecision(18, 2).HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("IsBlocked")
                b.ToTable("Pos_CreditAccounts")
            End Sub)

            modelBuilder.Entity("MerchSys.POS.Entities.SalesTransaction", Sub(b)
                b.Property(Of Integer)("Id").
                    ValueGeneratedOnAdd().
                    HasColumnType("INTEGER")
                b.Property(Of Decimal)("AmountTendered").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of Decimal)("ChangeAmount").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of Integer?)("CreditAccountId").HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer?)("CustomerId").HasColumnType("INTEGER")
                b.Property(Of String)("CustomerName").HasMaxLength(200).HasColumnType("TEXT")
                b.Property(Of DateTime?)("DeletedAt").HasColumnType("TEXT")
                b.Property(Of String)("DeletedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Decimal)("DiscountAmount").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of Boolean)("IsDeleted").HasColumnType("INTEGER")
                b.Property(Of Boolean)("IsVoided").HasColumnType("INTEGER")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer)("PaymentMethod").HasColumnType("INTEGER")
                b.Property(Of Decimal)("SubTotal").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of Decimal)("TotalAmount").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of DateTime)("TransactionDate").HasColumnType("TEXT")
                b.Property(Of String)("TransactionNumber").
                    IsRequired().
                    HasMaxLength(20).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("VatAmount").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of String)("VoidReason").HasMaxLength(500).HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("TransactionNumber").IsUnique()
                b.HasIndex("TransactionDate")
                b.HasIndex("CreditAccountId")
                b.ToTable("Pos_SalesTransactions")
            End Sub)

            modelBuilder.Entity("MerchSys.POS.Entities.SalesTransactionLine", Sub(b)
                b.Property(Of Integer)("Id").
                    ValueGeneratedOnAdd().
                    HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Decimal)("DiscountAmount").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of Decimal)("LineTotal").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer)("ProductId").HasColumnType("INTEGER")
                b.Property(Of String)("ProductName").
                    IsRequired().
                    HasMaxLength(200).
                    HasColumnType("TEXT")
                b.Property(Of Integer)("Quantity").HasColumnType("INTEGER")
                b.Property(Of Integer)("TransactionId").HasColumnType("INTEGER")
                b.Property(Of Decimal)("UnitPrice").HasPrecision(18, 2).HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("TransactionId")
                b.ToTable("Pos_SalesTransactionLines")
            End Sub)

            modelBuilder.Entity("MerchSys.POS.Entities.OfficialReceipt", Sub(b)
                b.Property(Of Integer)("Id").
                    ValueGeneratedOnAdd().
                    HasColumnType("INTEGER")
                b.Property(Of String)("BusinessAddress").HasMaxLength(500).HasColumnType("TEXT")
                b.Property(Of String)("BusinessName").
                    IsRequired().
                    HasMaxLength(200).
                    HasColumnType("TEXT")
                b.Property(Of String)("BusinessTIN").HasMaxLength(50).HasColumnType("TEXT")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of DateTime)("IssueDate").HasColumnType("TEXT")
                b.Property(Of String)("Items").HasMaxLength(4000).HasColumnType("TEXT")
                b.Property(Of Boolean)("IsVatRegistered").HasColumnType("INTEGER")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of String)("ReceiptNumber").
                    IsRequired().
                    HasMaxLength(20).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("TotalAmount").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of Integer)("TransactionId").HasColumnType("INTEGER")
                b.Property(Of Decimal)("VatAmount").HasPrecision(18, 2).HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("ReceiptNumber").IsUnique()
                b.HasIndex("TransactionId").IsUnique()
                b.ToTable("Pos_OfficialReceipts")
            End Sub)

            modelBuilder.Entity("MerchSys.POS.Entities.CreditPayment", Sub(b)
                b.Property(Of Integer)("Id").
                    ValueGeneratedOnAdd().
                    HasColumnType("INTEGER")
                b.Property(Of Integer)("CreditAccountId").HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of String)("Notes").HasMaxLength(500).HasColumnType("TEXT")
                b.Property(Of Decimal)("PaymentAmount").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of DateTime)("PaymentDate").HasColumnType("TEXT")
                b.Property(Of Integer)("PaymentMethod").HasColumnType("INTEGER")
                b.Property(Of String)("ReceivedBy").
                    IsRequired().
                    HasMaxLength(100).
                    HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("CreditAccountId")
                b.ToTable("Pos_CreditPayments")
            End Sub)

            modelBuilder.Entity("MerchSys.POS.Entities.SalesReturn", Sub(b)
                b.Property(Of Integer)("Id").
                    ValueGeneratedOnAdd().
                    HasColumnType("INTEGER")
                b.Property(Of DateTime)("CreatedAt").HasColumnType("TEXT")
                b.Property(Of String)("CreatedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Boolean)("IsRestocked").HasColumnType("INTEGER")
                b.Property(Of DateTime?)("ModifiedAt").HasColumnType("TEXT")
                b.Property(Of String)("ModifiedBy").HasMaxLength(256).HasColumnType("TEXT")
                b.Property(Of Integer)("OriginalTransactionId").HasColumnType("INTEGER")
                b.Property(Of Integer)("ProductId").HasColumnType("INTEGER")
                b.Property(Of String)("ProductName").
                    IsRequired().
                    HasMaxLength(200).
                    HasColumnType("TEXT")
                b.Property(Of Integer)("QuantityReturned").HasColumnType("INTEGER")
                b.Property(Of String)("Reason").
                    IsRequired().
                    HasMaxLength(500).
                    HasColumnType("TEXT")
                b.Property(Of Decimal)("RefundAmount").HasPrecision(18, 2).HasColumnType("TEXT")
                b.Property(Of DateTime)("ReturnDate").HasColumnType("TEXT")
                b.Property(Of Decimal)("UnitPrice").HasPrecision(18, 2).HasColumnType("TEXT")
                b.HasKey("Id")
                b.HasIndex("OriginalTransactionId")
                b.ToTable("Pos_SalesReturns")
            End Sub)

            modelBuilder.Entity("MerchSys.POS.Entities.SalesTransaction", Sub(b)
                b.HasOne("MerchSys.POS.Entities.CreditAccount", "CreditAccount").
                    WithMany("Transactions").
                    HasForeignKey("CreditAccountId")
                b.HasMany("MerchSys.POS.Entities.SalesTransactionLine", "Lines").
                    WithOne("Transaction").
                    HasForeignKey("TransactionId").
                    OnDelete(DeleteBehavior.Cascade).
                    IsRequired()
            End Sub)

            modelBuilder.Entity("MerchSys.POS.Entities.OfficialReceipt", Sub(b)
                b.HasOne("MerchSys.POS.Entities.SalesTransaction", "Transaction").
                    WithOne("Receipt").
                    HasForeignKey("MerchSys.POS.Entities.OfficialReceipt", "TransactionId").
                    IsRequired()
            End Sub)

            modelBuilder.Entity("MerchSys.POS.Entities.CreditPayment", Sub(b)
                b.HasOne("MerchSys.POS.Entities.CreditAccount", "CreditAccount").
                    WithMany("Payments").
                    HasForeignKey("CreditAccountId").
                    IsRequired()
            End Sub)

            modelBuilder.Entity("MerchSys.POS.Entities.SalesReturn", Sub(b)
                b.HasOne("MerchSys.POS.Entities.SalesTransaction", "OriginalTransaction").
                    WithMany().
                    HasForeignKey("OriginalTransactionId").
                    IsRequired()
            End Sub)
        End Sub

    End Class

End Namespace
