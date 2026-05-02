Imports MerchSys.POS.Entities

Namespace Data.SeedData

    Friend Module POSSeedData

        Private ReadOnly SeedDate As New DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)

        Public Function GetCreditAccountSeeds() As CreditAccount()
            Return {
                New CreditAccount() With {
                    .Id = 1,
                    .CustomerName = "Juan Dela Cruz",
                    .Phone = "",
                    .Address = "",
                    .Notes = "Farmer",
                    .CurrentBalance = 0D,
                    .TotalCreditExtended = 0D,
                    .TotalPaymentsReceived = 0D,
                    .IsBlocked = False,
                    .IsDeleted = False,
                    .CreatedBy = "System",
                    .CreatedAt = SeedDate,
                    .ModifiedBy = "System"
                },
                New CreditAccount() With {
                    .Id = 2,
                    .CustomerName = "Maria Santos",
                    .Phone = "",
                    .Address = "",
                    .Notes = "Farmer",
                    .CurrentBalance = 500D,
                    .TotalCreditExtended = 500D,
                    .TotalPaymentsReceived = 0D,
                    .IsBlocked = True,
                    .IsDeleted = False,
                    .CreatedBy = "System",
                    .CreatedAt = SeedDate,
                    .ModifiedBy = "System"
                },
                New CreditAccount() With {
                    .Id = 3,
                    .CustomerName = "Pedro Reyes",
                    .Phone = "",
                    .Address = "",
                    .Notes = "Farmer",
                    .CurrentBalance = 0D,
                    .TotalCreditExtended = 0D,
                    .TotalPaymentsReceived = 0D,
                    .IsBlocked = False,
                    .IsDeleted = False,
                    .CreatedBy = "System",
                    .CreatedAt = SeedDate,
                    .ModifiedBy = "System"
                }
            }
        End Function

    End Module

End Namespace
