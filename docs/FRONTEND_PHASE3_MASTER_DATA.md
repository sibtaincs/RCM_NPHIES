# ?? Frontend Phase 3 — Master Data Grids

> **What this covers:** the read-only Master Data screens built in Phase 3, how they talk to
> the .NET API, and the reusable pieces introduced for all future grids.
> Companion to `FRONTEND_DEVELOPMENT_PLAN.md` and `FRONTEND_PHASE0_SCAFFOLDING.md`.

---

## 1. ? What Was Delivered

The `Master Data` page is no longer a placeholder. It now shows **five live, server-paginated
grids** driven by the seeded database:

| Tab | API Resource | Key columns |
|-----|--------------|-------------|
| **Service Codes** | `ServiceCodeMasters` | Code, Name, Category, NPHIES code/mapping, Price, Pre-Auth |
| **Diagnosis Codes** | `DiagnosisCodeMasters` | ICD-10 Code, Name, Category, Type, Severity, NPHIES |
| **Medications** | `MedicationCodeMasters` | Code, Name, Ingredient, Strength, Form, Price, Controlled |
| **Payers** | `PayerMasters` | Payer ID, Name, Type, NPHIES member, Connection, Limits |
| **Doctors** | `DoctorMasters` | Doctor Code, Name, Specialization, Experience, Fees |

Every grid supports **search, pagination, page-size change, refresh, loading and error states**.

? `npx tsc --noEmit` passes with **no type errors**.

---

## 2. ?? Files Added

```
frontend/src/
??? types/masterData.ts    # PagedResult/PagedQuery + 5 DTO interfaces
??? api/endpoints/masterData.ts# createMasterDataApi() factory + resource map
??? hooks/
?   ??? useDebounce.ts  # Generic debounce
?   ??? useMasterData.ts     # Pagination + debounced search + React Query
??? components/
?   ??? DataTable/DataTable.tsx      # Reusable server-paginated grid
?   ??? StatusBadge/StatusBadge.tsx  # ActiveTag / MappedTag pills
??? utils/format.ts     # formatCurrency(), orDash()
??? features/master-data/
    ??? MasterDataPage.tsx     # Tabbed hub (rewritten)
    ??? grids/
 ??? ServiceCodesGrid.tsx
  ??? DiagnosisCodesGrid.tsx
        ??? MedicationsGrid.tsx
        ??? PayersGrid.tsx
        ??? DoctorsGrid.tsx
```

---

## 3. ?? The Reusable Pattern (important)

Phase 3 deliberately built **one generic pipeline** so future grids cost ~30 lines each.

### 3.1 API factory — `createMasterDataApi<T>(resource)`
Every master-data controller in the backend shares the same contract:

```
GET /api/{Resource}?skip=&take=&searchTerm=   ?   { items: T[], total: number }
```

So a single factory covers them all:

```ts
const api = createMasterDataApi<PayerMaster>('PayerMasters');
await api.getAll({ skip: 0, take: 10, searchTerm: 'bupa' });
```

Resource names live in one place (`masterDataResources`) and match the backend
`[Route("api/[controller]")]` names exactly.

### 3.2 Hook — `useMasterData<T>(resource)`
Owns all grid state and returns everything the table needs:

- `rows`, `total`, `isLoading`, `isFetching`, `error`, `refetch`
- `page`, `pageSize`, `search` + their setters
- **Debounced search** (400 ms) so typing doesn't spam the API
- **`keepPreviousData`** so the grid doesn't flicker while paging
- Search changes **auto-reset to page 1**

### 3.3 Component — `<DataTable resource columns />`
Renders search box + refresh + record count + AntD `Table` with server-side pagination,
horizontal scroll, and an error `Alert`. A whole screen becomes:

```tsx
<DataTable<PayerMaster>
resource={masterDataResources.payers}
  columns={columns}
  searchPlaceholder="Search payer ID or name…"
/>
```

> ? **To add a new master grid:** define a `ColumnsType<T>`, add the resource name, and drop in
> a `<DataTable>`. No new fetching/pagination code required.

---

## 4. ?? Backend Contract Used (verified)

Read from the actual controllers, not assumed:

- `MasterData/CodeMasterControllers.cs` ? `ServiceCodeMastersController`,
  `DiagnosisCodeMastersController`, `MedicationCodeMastersController`, …
- `MasterData/ProviderAndPolicyControllers.cs` ? `PayerMastersController`,
  `DoctorMastersController`, …

All of them:
```csharp
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]     // ?? see §5
public async Task<IActionResult> GetAll(int skip = 0, int take = 10, string? searchTerm = null)
    => Ok(new { items, total });
```

DTO field names were taken from `Application/DTOs/MasterDataDtos.cs` and mirrored in
`types/masterData.ts` (camelCase, as ASP.NET serialises them).

---

## 5. ?? Important: Admin-only endpoints

Every master-data controller is decorated with **`[Authorize(Roles = "Admin")]`**.

- Logged in as the seeded **`admin`** user ? all grids load normally. ?
- Logged in as any other role ? the API returns **403**, and the grid shows an error.

The page therefore renders a **warning banner** for non-Admin users explaining this.

> ?? **Design note:** the plan's Role?Feature matrix says most roles should have **read (R)**
> access to master data. The backend is currently stricter than the plan. Recommended fix
> (backend change): relax the read endpoints, e.g.
> `[Authorize]` on `GetAll`/`GetById` and keep `[Authorize(Roles="Admin")]` on
> create/update/delete. **Not changed in this phase** — flagging for your decision.

---

## 6. ?? How to Verify

```bash
cd frontend
npm run dev
```

1. Log in as **`admin`**.
2. Open **Master Data** from the sidebar.
3. Switch tabs — each loads independently (`destroyInactiveTabPane` keeps memory low).
4. Type in the search box ? watch it debounce and refetch.
5. Change page size ? confirm `skip`/`take` update in the Network tab.

---

## 7. ?? Design Decisions

| Decision | Why |
|---|---|
| **Tabs** instead of separate routes | One layout, faster to build, easy to compare datasets. Can be split into routes later without touching the grids. |
| **Read-only first** | Validates the full API?UI pipeline before adding write complexity (CRUD is Phase 4). |
| **Server-side pagination** | Master tables can grow to thousands of rows; never load them all. |
| **`destroyInactiveTabPane`** | Prevents 5 simultaneous queries on mount; each tab fetches only when opened. |
| **Defensive `?? []` in API layer** | Grid stays stable even if the API omits `items`/`total`. |

---

## 8. ??? What's Next

| Phase | Deliverable |
|-------|-------------|
| **4 — Master Data CRUD** | Create/Edit/Delete drawers using React Hook Form + Zod; wire `POST/PUT/DELETE` (already exist on the controllers). |
| **7 — Claims** | Claims grid + the **dual-approval** review UI (technical/medical) and the guarded Submit action. |
| **U2 — Users** | Admin user grid (a `UsersController` now exists — worth re-checking its contract). |

**Recommended next:** decide on the **Admin-only read restriction** (§5), then move to
**Phase 4 (CRUD)** to make Master Data fully manageable from the UI.

---

*Generated during Phase 3. Companion to `FRONTEND_DEVELOPMENT_PLAN.md`.*
