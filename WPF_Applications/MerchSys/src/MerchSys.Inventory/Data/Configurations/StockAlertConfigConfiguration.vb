Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Inventory.Entities

Namespace Data.Configurations

    Public Class StockAlertConfigConfiguration
        Implements IEntityTypeConfiguration(Of StockAlertConfig)

        Public Sub Configure(builder As EntityTypeBuilder(Of StockAlertConfig)) Implements IEntityTypeConfiguration(Of StockAlertConfig).Configure
            builder.ToTable("Inv_StockAlertConfigs")

            builder.HasKey(Function(a) a.Id)

            builder.HasOne(Function(a) a.Product).
                    WithMany().
                    HasForeignKey(Function(a) a.ProductId).
                    OnDelete(DeleteBehavior.Cascade)

            builder.Property(Function(e) e.RowVersion).
                IsRowVersion().
                HasColumnType("TIMESTAMP(6)").
                ValueGeneratedOnAddOrUpdate()
        End Sub

    End Class

End Namespace
