# ?? User Management & Access Control — Implementation Guide

> **Status:** ? Complete and type-checking clean (`npx tsc --noEmit` passes)
> **Scope:** Full admin user management UI wired to the existing `.NET 9` `UsersController`.
> Companion to `FRONTEND_DEVELOPMENT_PLAN.md`, `FRONTEND_PHASE0_SCAFFOLDING.md`,
> `FRONTEND_PHASE3_MASTER_DATA.md`.

---

## 1. ? What You Can Do Now

From **User Management** in the sidebar (Admin only):

| Capability | How |
|---|---|
| **List users** | Paged grid with debounced search over username / email / name |
| **Create user** | "New user" ? drawer with username, email, temp password, name, **roles** |
| **Edit user** | Row menu ? Edit (username is immutable) |
| **Assign roles** | Multi-select in the drawer, populated from `GET /api/users/roles` |
| **Activate / Deactivate** | Row menu toggle |
| **Unlock account** | Row menu (appears only when the account is locked) |
| **Reset password** | Row menu ? modal with confirm field; revokes all sessions |
| **Delete user** | Row menu (soft delete, with confirmation) |
| **View roles reference** | "Roles & Permissions" tab — what each role is for |

---

## 2. ?? Important Finding — the Backend Was Already Done

Before writing any code I inspected the API. **`UsersController` already existed and is
excellent** — no backend changes were needed.

### Endpoints consumed (`/api/users`, all `[Authorize(Roles = "Admin")]`)

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/users?skip&take&search` | Paged list ? `{ items, total }` |
| `GET` | `/api/users/roles` | The 13 assignable role names |
| `GET` | `/api/users/{id}` | Single user |
| `POST` | `/api/users` | Create |
| `PUT` | `/api/users/{id}` | Update profile + roles |
| `POST` | `/api/users/{id}/set-active?isActive=` | Activate / deactivate |
| `POST` | `/api/users/{id}/unlock` | Clear lockout + failed counter |
| `POST` | `/api/users/{id}/reset-password` | Set password, revoke refresh tokens |
| `DELETE` | `/api/users/{id}` | Soft delete |

### Backend safety rails already in place (we rely on these)
- ? Cannot remove `Admin` from the **last administrator**
- ? Cannot deactivate or delete the **last administrator**
- ? Unknown role names are rejected
- ? Password minimum length enforced (`SecurityConstants.MINIMUM_PASSWORD_LENGTH` = 12)
- ? Password data is **never** returned by the API
- ? Reset password revokes all `RefreshTokens` (forces re-login everywhere)

> ?? The UI surfaces these errors rather than duplicating the logic — the server stays
> the single source of truth.

---

## 3. ?? Files Added (frontend)

```
frontend/src/
??? types/users.ts    # UserRecord, Create/Update payloads, UsersPage
??? api/endpoints/users.ts   # Typed wrappers for all 9 endpoints
??? utils/apiError.ts          # Extracts { message } from API errors
??? features/user-management/
    ??? UsersPage.tsx    # Tabbed hub (rewritten)
    ??? UsersListPage.tsx      # Grid + row actions
    ??? RolesPage.tsx      # Role reference table
    ??? hooks/
    ?   ??? useUsers.ts    # useUsers, useRoles, useUserMutations
    ??? schemas/
    ?   ??? userSchemas.ts       # Zod schemas + MIN_PASSWORD_LENGTH
    ??? components/
        ??? RoleTags.tsx      # Colour-coded role pills (+N overflow)
        ??? UserStatusBadge.tsx  # Locked / Inactive / Active
     ??? UserFormDrawer.tsx   # Create + Edit drawer
        ??? PasswordResetModal.tsx
```

**Modified:** `layouts/AppLayout.tsx` — the **Users menu item is now hidden** for non-Admins.

---

## 4. ?? How It's Wired

### 4.1 API layer — `usersApi`
One typed object per endpoint. Note `setActive` sends the flag as a **query string**
(matching `[FromQuery] bool isActive`), not a body:

```ts
usersApi.setActive(id, isActive)  // POST /users/{id}/set-active?isActive=true
```

### 4.2 Hooks — `useUsers`, `useRoles`, `useUserMutations`
- **`useUsers()`** — pagination + 400 ms debounced search, `keepPreviousData` (no flicker),
  auto-reset to page 1 on new search.
- **`useRoles()`** — cached 5 minutes (the catalogue is static).
- **`useUserMutations()`** — `create`, `update`, `setActive`, `unlock`, `resetPassword`,
  `remove`. Every mutation:
  - shows an AntD **success toast**,
  - **invalidates the users query** so the grid refreshes automatically,
  - on failure shows the **backend's own message** (e.g. *"Cannot delete the last administrator."*).

### 4.3 RBAC — three layers
1. **Menu** — Users entry hidden unless `hasAnyRole(['Admin'])`.
2. **Route** — `/users` wrapped in `<ProtectedRoute roles={['Admin']} />` ? 403 page.
3. **Server** — `[Authorize(Roles = "Admin")]` (the only one that actually matters).

> The first two are **UX**; the third is **security**. Never rely on the client.

### 4.4 Self-protection in the UI
The current user's row is marked **"(you)"**, and **Deactivate** / **Delete** are disabled for
yourself — preventing an admin from locking themselves out.

---

## 5. ?? Roles Available (13)

Sourced live from `GET /api/users/roles`, defined in `SecurityConstants`:

`Admin`, `RCMProcessor`, `RCMViewer`, `ClaimsProcessor`, `ClaimsReviewer`,
`TechnicalReviewer`, `MedicalReviewer`, `PayerUser`, `ProviderUser`,
`ComplianceOfficer`, `Auditor`, `Accountant`, `User`

Each is colour-coded in the grid for scannability. The **Roles & Permissions** tab documents
what each one is intended to do (mirrors the matrix in the development plan).

---

## 6. ?? How to Verify

```bash
cd frontend
npm run dev
```

1. Sign in as **`admin`**.
2. Open **User Management**.
3. **Create** a user — e.g. `treviewer` with role `TechnicalReviewer`, password ? 12 chars.
4. Sign out, sign in as the new user ? the **Users menu item disappears**; visiting `/users`
   directly shows the **403** page.
5. Back as admin: try **deleting the admin account** ? the backend blocks it and the UI shows
   *"Cannot delete the last administrator."*

---

## 7. ?? Notes & Decisions

1. **Two role systems exist.** There is also a richer `RolesController` / `UserRolesController`
   (DB-backed roles with granular permissions). This UI intentionally targets **`/api/users`**,
   because that is what populates the **JWT roles claim** that all `[Authorize]` checks and the
   frontend guards actually use. The other system is currently not wired into authentication.
   ?? Worth a decision later: consolidate on one model.

2. **Delete confirmation** uses a native `window.confirm` for simplicity. Easy to swap for an
   AntD `Modal.confirm` if you want consistent styling.

3. **Master data is still Admin-only** (flagged in the Phase 3 doc) — unrelated to this work,
   but still open for your decision.

4. **Not yet built** (available as follow-ups): self-service profile page & change-password,
   MFA toggle, login-attempt audit view, activity audit view.

---

## 8. ??? Suggested Next Steps

| Priority | Item |
|---|---|
| ?? | **Decide on the dual role systems** (§7.1) before building more business features |
| ?? | **Profile page** — let any user view their profile & change their own password |
| ?? | **Audit views** — `LoginAttempt` + `AuditLog` grids (Admin/Auditor/Compliance) |
| ? | Then proceed to business features (**Claims + dual-approval UI**) |

---

*User management is now production-usable for administering access to the RCM console.*
