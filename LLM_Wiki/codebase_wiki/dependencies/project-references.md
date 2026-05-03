---
type: dependency-map
last-updated: 2026-05-03
---

# Project References

Visual map of dependencies across the `.vbproj` files.

- `MerchSys.SharedKernel` -> (No project dependencies)
- `MerchSys.POS` -> `MerchSys.SharedKernel`
- `MerchSys.Purchasing` -> `MerchSys.SharedKernel`
- `MerchSys.Inventory` -> `MerchSys.SharedKernel`
- `MerchSys.Accounting` -> `MerchSys.SharedKernel`
- `MerchSys.App` -> `MerchSys.SharedKernel`, `MerchSys.POS`, `MerchSys.Purchasing`, `MerchSys.Inventory`, `MerchSys.Accounting`
