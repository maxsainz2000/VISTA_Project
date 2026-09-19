---
type: layer-manifest
module: MerchSys.SharedKernel
layer: Paging
last-updated: 2026-06-10
---

# MerchSys.SharedKernel — Paging Contracts

Standardized keyset (seek) pagination contracts for date-ordered history grids (INFRA-34). Keyset paging is used to maintain O(page) performance on unbounded tables by seeking via ordered indexes rather than counting past skipped rows.

## DTOs

| Class (`Paging/`) | Purpose | Key Properties |
|---|---|---|
| `PageRequest.vb` | Request for a specific slice of data using a composite cursor. | `PageSize` (Default 100), `CursorDate`, `CursorId` (Tie-breaker), `FromUtc`, `ToUtc`, `IsFirstPage` |
| `PagedResult(Of T).vb` | Result containing one page of items and the cursor for the next page. | `Items`, `HasMore` (Derived via Fetch-N+1), `NextCursorDate`, `NextCursorId` |

## Usage Pattern
1. **Producer:** Fetches `PageSize + 1` rows where `(Date < @cursorDate OR (Date = @cursorDate AND Id < @cursorId))`.
2. **HasMore:** If the `+1` row exists, `HasMore = True` and the extra row is dropped.
3. **Cursor:** `NextCursorDate`/`NextCursorId` are extracted from the last row in the page.
4. **Consumer:** Appends items to the collection and updates the `LoadMoreCommand` visibility based on `HasMore`.
