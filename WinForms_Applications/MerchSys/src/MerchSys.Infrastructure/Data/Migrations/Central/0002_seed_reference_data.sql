-- VISTA Centralized MariaDB Seed Reference Data
-- Migration ID: 0002_seed_reference_data.sql
-- Reference Plans: INFRA-24

-- 1. Seed Product Categories
INSERT IGNORE INTO `Inv_ProductCategories` 
(`Id`, `Name`, `Description`, `IsDeleted`, `CreatedBy`, `CreatedAt`) VALUES 
(1, 'Fertilizers', 'Soil nutrients and plant growth supplements.', 0, 'System', UTC_TIMESTAMP(6)),
(2, 'Pesticides/Chemicals', 'Insecticides, herbicides, and fungicides.', 0, 'System', UTC_TIMESTAMP(6)),
(3, 'Seeds', 'Planting seeds for various crops.', 0, 'System', UTC_TIMESTAMP(6)),
(4, 'Animal Feeds', 'Feeds for hogs, poultry, and aquaculture.', 0, 'System', UTC_TIMESTAMP(6));

-- 2. Seed Vendors
INSERT IGNORE INTO `Pur_Vendors` 
(`Id`, `Name`, `ContactPerson`, `Phone`, `Email`, `Address`, `DefaultLeadTimeDays`, `Notes`, `IsDeleted`, `CreatedBy`, `CreatedAt`) VALUES 
(1, 'AgriChem Supplies', 'Juan dela Cruz', '09171234567', 'sales@agrichemsupplies.ph', '123 Magsaysay Ave, Cagayan de Oro City', 5, 'Pesticides and chemicals supplier.', 0, 'System', UTC_TIMESTAMP(6)),
(2, 'FarmFresh Seeds Corp.', 'Maria Santos', '09189876543', 'orders@farmfreshseeds.ph', '456 National Highway, Bukidnon', 7, 'Seeds and planting materials supplier.', 0, 'System', UTC_TIMESTAMP(6)),
(3, 'Golden Feeds Trading', 'Pedro Reyes', '09205551234', 'info@goldenfeedstrading.ph', '789 Rizal Street, Iligan City', 3, 'Animal feeds and livestock supplies.', 0, 'System', UTC_TIMESTAMP(6));

-- 3. Seed Products
INSERT IGNORE INTO `Inv_Products` 
(`Id`, `CategoryId`, `Name`, `Sku`, `Unit`, `RetailPrice`, `HasExpiry`, `MinimumThreshold`, `IsActive`, `IsDeleted`, `CreatedBy`, `CreatedAt`) VALUES 
(1, 1, 'Complete Fertilizer 14-14-14', 'FERT-001', 'bag', 1250.0000, 0, 10, 1, 0, 'System', UTC_TIMESTAMP(6)),
(2, 1, 'Urea 46-0-0', 'FERT-002', 'bag', 1450.0000, 0, 10, 1, 0, 'System', UTC_TIMESTAMP(6)),
(3, 1, 'Ammonium Sulfate', 'FERT-003', 'bag', 1100.0000, 0, 10, 1, 0, 'System', UTC_TIMESTAMP(6)),
(4, 1, 'Muriate of Potash 0-0-60', 'FERT-004', 'bag', 1600.0000, 0, 5, 1, 0, 'System', UTC_TIMESTAMP(6)),
(5, 1, 'Calcium Nitrate', 'FERT-005', 'bag', 1800.0000, 0, 5, 1, 0, 'System', UTC_TIMESTAMP(6)),
(6, 2, 'Malathion 57 EC', 'PEST-001', 'bottle', 480.0000, 1, 5, 1, 0, 'System', UTC_TIMESTAMP(6)),
(7, 2, 'Cypermethrin 10 EC', 'PEST-002', 'bottle', 520.0000, 1, 5, 1, 0, 'System', UTC_TIMESTAMP(6)),
(8, 2, 'Lambda-Cyhalothrin 2.5 EC', 'PEST-003', 'bottle', 650.0000, 1, 5, 1, 0, 'System', UTC_TIMESTAMP(6)),
(9, 2, 'Glyphosate 48 SL', 'PEST-004', 'bottle', 390.0000, 1, 5, 1, 0, 'System', UTC_TIMESTAMP(6)),
(10, 2, 'Mancozeb 80 WP', 'PEST-005', 'pack', 280.0000, 1, 10, 1, 0, 'System', UTC_TIMESTAMP(6)),
(11, 3, 'Hybrid Rice RC222', 'SEED-001', 'kg', 1950.0000, 1, 5, 1, 0, 'System', UTC_TIMESTAMP(6)),
(12, 3, 'Corn Yellow Hybrid', 'SEED-002', 'kg', 1200.0000, 1, 5, 1, 0, 'System', UTC_TIMESTAMP(6)),
(13, 3, 'Eggplant Seeds', 'SEED-003', 'pack', 85.0000, 1, 10, 1, 0, 'System', UTC_TIMESTAMP(6)),
(14, 3, 'Tomato Seeds', 'SEED-004', 'pack', 95.0000, 1, 10, 1, 0, 'System', UTC_TIMESTAMP(6)),
(15, 3, 'Ampalaya Seeds', 'SEED-005', 'pack', 75.0000, 1, 10, 1, 0, 'System', UTC_TIMESTAMP(6)),
(16, 4, 'Hog Grower Pellets', 'FEED-001', 'bag', 1350.0000, 1, 10, 1, 0, 'System', UTC_TIMESTAMP(6)),
(17, 4, 'Poultry Layer Mash', 'FEED-002', 'bag', 1280.0000, 1, 10, 1, 0, 'System', UTC_TIMESTAMP(6)),
(18, 4, 'Hog Starter Crumble', 'FEED-003', 'bag', 1420.0000, 1, 5, 1, 0, 'System', UTC_TIMESTAMP(6)),
(19, 4, 'Broiler Starter Mash', 'FEED-004', 'bag', 1300.0000, 1, 5, 1, 0, 'System', UTC_TIMESTAMP(6)),
(20, 4, 'Tilapia Pellets', 'FEED-005', 'bag', 980.0000, 1, 5, 1, 0, 'System', UTC_TIMESTAMP(6));

-- 4. Seed Credit Accounts
INSERT IGNORE INTO `Pos_CreditAccounts` 
(`Id`, `CustomerName`, `Phone`, `Address`, `CurrentBalance`, `TotalCreditExtended`, `TotalPaymentsReceived`, `IsBlocked`, `Notes`, `IsDeleted`, `CreatedBy`, `CreatedAt`) VALUES 
(1, 'Juan Dela Cruz', '', '', 0.0000, 0.0000, 0.0000, 0, 'Farmer', 0, 'System', UTC_TIMESTAMP(6)),
(2, 'Maria Santos', '', '', 500.0000, 500.0000, 0.0000, 1, 'Farmer', 0, 'System', UTC_TIMESTAMP(6)),
(3, 'Pedro Reyes', '', '', 0.0000, 0.0000, 0.0000, 0, 'Farmer', 0, 'System', UTC_TIMESTAMP(6));

-- 5. Seed VAT Configuration (Single Singleton Row)
INSERT IGNORE INTO `Pos_VatConfiguration` 
(`Id`, `IsVatRegistered`, `VatRate`, `NonVatPercentageTaxRate`, `EffectiveFrom`, `BusinessTIN`, `BusinessName`, `BusinessAddress`, `CreatedBy`, `CreatedAt`) VALUES 
(1, 0, 0.1200, 0.0300, '2026-01-01 00:00:00.000000', NULL, 'Villon Farm Supply', NULL, 'System', UTC_TIMESTAMP(6));

-- 6. Seed User Accounts
-- Pre-seeded default Argon2id password hashes for 'manager' and 'owner' with default password 'Vista2026!'
-- Salted Argon2id hashes:
-- 'manager' default: $argon2id$v=19$m=32768,t=4,p=1$ZGVmYXVsdHNhbHQ$6jHh/11V3kG4u4v271kZ8wX7t4T5Ld9p (example valid hash structure, but let's generate active hashed password)
-- Wait! Since PasswordHashHelper.Hash is in MerchSys.App, we can seed them here via hardcoded hashes, or generate them in MariaDbSchemaInitializer.vb using PasswordHashHelper.
-- Let's check how PasswordHashHelper works or how they are generated.
-- We can seed them in SQL using a known good hash, or let the VB.NET code seed the User Accounts.
-- Let's put standard SQL values here. Let's see: we can generate a valid Argon2id hash for 'Vista2026!'.
-- To be safe, we will seed them inside MariaDbSchemaInitializer.vb via code so it uses the actual PasswordHashHelper.Hash method, guaranteeing correctness under the actual helper implementation! 
-- This is incredibly robust because if PasswordHashHelper changes, the seed hash remains valid.
-- So we'll seed User Accounts in the MariaDbSchemaInitializer.vb code instead of raw SQL! This matches DatabaseInitializer.vb's logic precisely.
