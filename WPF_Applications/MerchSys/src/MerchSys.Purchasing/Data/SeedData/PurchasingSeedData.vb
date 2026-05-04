Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Purchasing.Entities

Namespace Data.SeedData

    Friend Module PurchasingSeedData

        Private ReadOnly SeedDate As DateTime = New DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        Private Const SeedUser As String = "Manager"

        Public Sub Seed(modelBuilder As ModelBuilder)
            SeedVendors(modelBuilder)
        End Sub

        Private Sub SeedVendors(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Vendor)().HasData(
                New Vendor With {
                    .Id = 1, .Name = "AgriChem Supplies",
                    .ContactPerson = "Juan dela Cruz", .Phone = "09171234567",
                    .Email = "sales@agrichemsupplies.ph", .Address = "123 Magsaysay Ave, Cagayan de Oro City",
                    .DefaultLeadTimeDays = 5, .Notes = "Pesticides and chemicals supplier.",
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Vendor With {
                    .Id = 2, .Name = "FarmFresh Seeds Corp.",
                    .ContactPerson = "Maria Santos", .Phone = "09189876543",
                    .Email = "orders@farmfreshseeds.ph", .Address = "456 National Highway, Bukidnon",
                    .DefaultLeadTimeDays = 7, .Notes = "Seeds and planting materials supplier.",
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Vendor With {
                    .Id = 3, .Name = "Golden Feeds Trading",
                    .ContactPerson = "Pedro Reyes", .Phone = "09205551234",
                    .Email = "info@goldenfeedstrading.ph", .Address = "789 Rizal Street, Iligan City",
                    .DefaultLeadTimeDays = 3, .Notes = "Animal feeds and livestock supplies.",
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                }
            )
        End Sub

    End Module

End Namespace
