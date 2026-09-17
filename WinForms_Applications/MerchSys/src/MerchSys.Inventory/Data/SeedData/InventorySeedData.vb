Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Inventory.Entities

Namespace Data.SeedData

    Friend Module InventorySeedData

        Private ReadOnly SeedDate As DateTime = New DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        Private Const SeedUser As String = "Manager"

        Public Sub Seed(modelBuilder As ModelBuilder)
            SeedCategories(modelBuilder)
            SeedFertilizers(modelBuilder)
            SeedPesticides(modelBuilder)
            SeedSeeds(modelBuilder)
            SeedAnimalFeeds(modelBuilder)
        End Sub

        Private Sub SeedCategories(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of ProductCategory)().HasData(
                New ProductCategory With {
                    .Id = 1, .Name = "Fertilizers",
                    .Description = "Soil nutrients and plant growth supplements.",
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New ProductCategory With {
                    .Id = 2, .Name = "Pesticides/Chemicals",
                    .Description = "Insecticides, herbicides, and fungicides.",
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New ProductCategory With {
                    .Id = 3, .Name = "Seeds",
                    .Description = "Planting seeds for various crops.",
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New ProductCategory With {
                    .Id = 4, .Name = "Animal Feeds",
                    .Description = "Feeds for hogs, poultry, and aquaculture.",
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                }
            )
        End Sub

        Private Sub SeedFertilizers(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Product)().HasData(
                New Product With {
                    .Id = 1, .CategoryId = 1, .Name = "Complete Fertilizer 14-14-14",
                    .Sku = "FERT-001", .Unit = "bag", .RetailPrice = 1250D,
                    .HasExpiry = False, .MinimumThreshold = 10, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Product With {
                    .Id = 2, .CategoryId = 1, .Name = "Urea 46-0-0",
                    .Sku = "FERT-002", .Unit = "bag", .RetailPrice = 1450D,
                    .HasExpiry = False, .MinimumThreshold = 10, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Product With {
                    .Id = 3, .CategoryId = 1, .Name = "Ammonium Sulfate",
                    .Sku = "FERT-003", .Unit = "bag", .RetailPrice = 1100D,
                    .HasExpiry = False, .MinimumThreshold = 10, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Product With {
                    .Id = 4, .CategoryId = 1, .Name = "Muriate of Potash 0-0-60",
                    .Sku = "FERT-004", .Unit = "bag", .RetailPrice = 1600D,
                    .HasExpiry = False, .MinimumThreshold = 5, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Product With {
                    .Id = 5, .CategoryId = 1, .Name = "Calcium Nitrate",
                    .Sku = "FERT-005", .Unit = "bag", .RetailPrice = 1800D,
                    .HasExpiry = False, .MinimumThreshold = 5, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                }
            )
        End Sub

        Private Sub SeedPesticides(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Product)().HasData(
                New Product With {
                    .Id = 6, .CategoryId = 2, .Name = "Malathion 57 EC",
                    .Sku = "PEST-001", .Unit = "bottle", .RetailPrice = 480D,
                    .HasExpiry = True, .MinimumThreshold = 5, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Product With {
                    .Id = 7, .CategoryId = 2, .Name = "Cypermethrin 10 EC",
                    .Sku = "PEST-002", .Unit = "bottle", .RetailPrice = 520D,
                    .HasExpiry = True, .MinimumThreshold = 5, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Product With {
                    .Id = 8, .CategoryId = 2, .Name = "Lambda-Cyhalothrin 2.5 EC",
                    .Sku = "PEST-003", .Unit = "bottle", .RetailPrice = 650D,
                    .HasExpiry = True, .MinimumThreshold = 5, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Product With {
                    .Id = 9, .CategoryId = 2, .Name = "Glyphosate 48 SL",
                    .Sku = "PEST-004", .Unit = "bottle", .RetailPrice = 390D,
                    .HasExpiry = True, .MinimumThreshold = 5, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Product With {
                    .Id = 10, .CategoryId = 2, .Name = "Mancozeb 80 WP",
                    .Sku = "PEST-005", .Unit = "pack", .RetailPrice = 280D,
                    .HasExpiry = True, .MinimumThreshold = 10, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                }
            )
        End Sub

        Private Sub SeedSeeds(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Product)().HasData(
                New Product With {
                    .Id = 11, .CategoryId = 3, .Name = "Hybrid Rice RC222",
                    .Sku = "SEED-001", .Unit = "kg", .RetailPrice = 1950D,
                    .HasExpiry = True, .MinimumThreshold = 5, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Product With {
                    .Id = 12, .CategoryId = 3, .Name = "Corn Yellow Hybrid",
                    .Sku = "SEED-002", .Unit = "kg", .RetailPrice = 1200D,
                    .HasExpiry = True, .MinimumThreshold = 5, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Product With {
                    .Id = 13, .CategoryId = 3, .Name = "Eggplant Seeds",
                    .Sku = "SEED-003", .Unit = "pack", .RetailPrice = 85D,
                    .HasExpiry = True, .MinimumThreshold = 10, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Product With {
                    .Id = 14, .CategoryId = 3, .Name = "Tomato Seeds",
                    .Sku = "SEED-004", .Unit = "pack", .RetailPrice = 95D,
                    .HasExpiry = True, .MinimumThreshold = 10, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Product With {
                    .Id = 15, .CategoryId = 3, .Name = "Ampalaya Seeds",
                    .Sku = "SEED-005", .Unit = "pack", .RetailPrice = 75D,
                    .HasExpiry = True, .MinimumThreshold = 10, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                }
            )
        End Sub

        Private Sub SeedAnimalFeeds(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Product)().HasData(
                New Product With {
                    .Id = 16, .CategoryId = 4, .Name = "Hog Grower Pellets",
                    .Sku = "FEED-001", .Unit = "bag", .RetailPrice = 1350D,
                    .HasExpiry = True, .MinimumThreshold = 10, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Product With {
                    .Id = 17, .CategoryId = 4, .Name = "Poultry Layer Mash",
                    .Sku = "FEED-002", .Unit = "bag", .RetailPrice = 1280D,
                    .HasExpiry = True, .MinimumThreshold = 10, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Product With {
                    .Id = 18, .CategoryId = 4, .Name = "Hog Starter Crumble",
                    .Sku = "FEED-003", .Unit = "bag", .RetailPrice = 1420D,
                    .HasExpiry = True, .MinimumThreshold = 5, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Product With {
                    .Id = 19, .CategoryId = 4, .Name = "Broiler Starter Mash",
                    .Sku = "FEED-004", .Unit = "bag", .RetailPrice = 1300D,
                    .HasExpiry = True, .MinimumThreshold = 5, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                },
                New Product With {
                    .Id = 20, .CategoryId = 4, .Name = "Tilapia Pellets",
                    .Sku = "FEED-005", .Unit = "bag", .RetailPrice = 980D,
                    .HasExpiry = True, .MinimumThreshold = 5, .IsActive = True,
                    .IsDeleted = False,
                    .CreatedBy = SeedUser, .CreatedAt = SeedDate,
                    .ModifiedBy = SeedUser, .ModifiedAt = SeedDate
                }
            )
        End Sub

    End Module

End Namespace
