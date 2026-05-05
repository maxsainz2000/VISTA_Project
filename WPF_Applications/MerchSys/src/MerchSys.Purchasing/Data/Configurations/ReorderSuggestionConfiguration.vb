Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports MerchSys.Purchasing.Entities

Namespace Data.Configurations

    Public Class ReorderSuggestionConfiguration
        Implements IEntityTypeConfiguration(Of ReorderSuggestion)

        Public Sub Configure(builder As EntityTypeBuilder(Of ReorderSuggestion)) Implements IEntityTypeConfiguration(Of ReorderSuggestion).Configure
            builder.ToTable("Pur_ReorderSuggestions")

            builder.HasKey(Function(s) s.Id)

            builder.Property(Function(s) s.ProductId).IsRequired()
            builder.Property(Function(s) s.ProductName).IsRequired().HasMaxLength(200)
            builder.Property(Function(s) s.Status).IsRequired().HasMaxLength(20)
            builder.Property(Function(s) s.PreferredVendorName).HasMaxLength(200)

            builder.HasIndex(Function(s) s.Status)
            builder.HasIndex(Function(s) New With {s.ProductId, s.Status})
        End Sub

    End Class

End Namespace
