# ?? RBAC — Roles & Permissions Management (Phase A + B)

> **Status:** ? Backend builds with **0 errors** · Frontend `tsc --noEmit` **clean** · Migration created
> Everything — create role, create permission, grant/revoke, assign to users — is now driven from the UI.

---

## 1. ?? What Was Delivered

| Capability | Where |
|---|---|
| Create / edit / delete **roles** | Users ? **Roles** tab |
| Create / edit / delete **permissions** | Users ? **Permissions** tab |
| **Grant / revoke** permissions per role | Roles ? Edit (category matrix) |
| **Assign roles to users** | Users ? Edit (live role catalogue) |
| **Effective access** (what can a user do, and why) | Users ? **Effective Access** tab |
| Permissions enforced server-side | `[HasPermission("…")]` |

---

## 2. ?? The Key Architectural Decision

The repo contained a legacy RBAC set (`Role`, `Permission`, `UserRole`, `RolePermission`) that was
**unusable**:

- `User.Id` is a **string GUID**, but `UserRole.UserId` is an **`int`** ? `int.Parse(guid)` throws
- Endpoints were guarded by roles that don't exist (`TECHNICAL_REVIEW_MANAGER`) ? **nobody** could call them
- Its permissions were never written into the JWT ? **zero effect** on authorization

### ? Chosen approach: a clean schema keyed by **role name**

New tables (`AppRoles`, `AppPermissions`, `AppRolePermissions`) all use **string GUID** ids,
consistent with `BaseEntity`. Crucially, `AppRole.Name` matches the strings already stored in
`User.Roles` — the same values the JWT role claims are built from.

> ?? **Why this matters:** we get data-driven roles/permissions **without touching authentication**.
> The existing `[Authorize(Roles = "...")]` checks keep working exactly as before, and permissions
> ride along as an extra claim.

The legacy tables were left untouched (no data loss) but are **not used**.

---

## 3. ??? Backend Changes (Phase A)

| # | Item | File |
|---|---|---|
| 1 | Entities `AppRole`, `AppPermission`, `AppRolePermission` | `Domain/Entities/Access/AccessControlEntities.cs` |
| 2 | EF configs + unique indexes | `Infrastructure/Data/Configurations/AccessControlConfigurations.cs` |
| 3 | DbSets + `ApplyAllConfigurations` registration | `ApplicationDbContext.cs`, `ModelBuilderExtensions.cs` |
| 4 | **28-permission catalogue** | `ApiService/Security/Permissions.cs` |
| 5 | Idempotent seeder (13 roles, 28 permissions, default grants) | `ApiService/Security/AccessControlSeeder.cs` |
| 6 | `IPermissionResolver` — roles ? permissions | `ApiService/Security/Services/PermissionResolver.cs` |
| 7 | `[HasPermission]` attribute + policy provider + handler | `ApiService/Security/Authorization/PermissionAuthorization.cs` |
| 8 | **`AccessControlController`** (`/api/access`) | `ApiService/Controllers/AccessControlController.cs` |
| 9 | Login now resolves permissions **from the DB** | `AuthenticationService.BuildContextAsync` |
| 10 | `/api/users/roles` reads the **live catalogue** | `UsersController.cs` |
| 11 | Migration `AddAccessControlRolesPermissions` | `Infrastructure/Migrations/` |

### ?? The critical fix — permissions now reach the token

`BuildContext` was synchronous and returned **hardcoded** permissions. It is now:

```csharp
private async Task<UserSecurityContext> BuildContextAsync(User user)
{
    …
    // Effective permissions come from the database (role -> permission grants),
    // so administrators can change access without a redeploy.
    context.Permissions = await _permissionResolver.GetPermissionsForRolesAsync(context.Roles);
    return context;
}
```

`JwtService` already emitted a permission claim, so permissions now flow end-to-end:

```
AppRolePermissions ?? PermissionResolver ?? UserSecurityContext ?? JWT claim ?? [HasPermission]
```

### API surface (`/api/access`)

| Method | Route | Permission |
|---|---|---|
| `GET` | `/roles` | `roles:read` |
| `POST` | `/roles` | `roles:manage` |
| `PUT` | `/roles/{id}` | `roles:manage` |
| `DELETE` | `/roles/{id}` | `roles:manage` |
| `PUT` | `/roles/{id}/permissions` | `roles:manage` |
| `GET` | `/permissions` | `roles:read` |
| `POST` `PUT` `DELETE` | `/permissions…` | `roles:manage` |
| `GET` | `/users/{id}/effective-permissions` | `users:read` |

### ??? Safety rails
- **System roles/permissions** cannot be renamed or deleted (permissions *can* still be re-granted)
- A role **in use by any active user** cannot be deleted
- **Admin implicitly passes every** `[HasPermission]` check (prevents lockout)
- Grants are seeded **only once**, so revoking a default doesn't get undone on restart

---

## 4. ??? Frontend Changes (Phase B)

```
frontend/src/
??? types/access.ts       # Role, Permission, EffectiveAccess
??? api/endpoints/access.ts       # /api/access wrappers
??? features/user-management/
    ??? UsersPage.tsx      # 4 tabs
    ??? RolesPage.tsx       # ? rewritten: live CRUD grid
    ??? PermissionsPage.tsx    # NEW
    ??? EffectiveAccessPage.tsx     # NEW
    ??? hooks/useAccessControl.ts       # queries + mutations
    ??? components/RoleFormDrawer.tsx   # role + permission matrix
```

**Permission matrix UX:** permissions are grouped by **category** in a collapse, each with a
tri-state “select all” checkbox and an `n/total` badge — so granting an entire area is one click.

**User ? Roles** now uses the live catalogue, showing `Display Name (n permissions)`, so
**custom roles appear automatically**.

---

## 5. ?? Seeded Defaults

**13 roles** (mirroring `SecurityConstants`) and **28 permissions** across 9 categories:
`Claims`, `PreAuthorization`, `Eligibility`, `Patients`, `MasterData`, `Payments`, `Reports`,
`AccessControl`, `Audit`.

Default grants follow least-privilege, e.g.:

| Role | Notable grants |
|---|---|
| `Admin` | **everything** |
| `ClaimsProcessor` | `claims:submit` ? |
| `TechnicalReviewer` | `claims:technical-review` |
| `MedicalReviewer` | `claims:medical-review` |
| `Auditor` | `audit:read` (read-only) |

---

## 6. ?? How to Verify

```bash
# Backend — applies the migration and seeds on startup
dotnet run --project NPhies_FHIR_Integration.ApiService

# Frontend
cd frontend && npm run dev
```

1. Sign in as **`admin`** ? **User Management**.
2. **Permissions** tab ? 28 seeded permissions (system ones show a ??).
3. **Roles** tab ? 13 roles with permission counts.
4. Click **New role** ? e.g. `BillingSpecialist`, tick the *Payments* category ? Create.
5. **Users** tab ? edit a user ? the new role appears in the dropdown ?
6. **Effective Access** tab ? pick that user ? see each permission and **which role granted it**.

> ?? **Token refresh required:** permissions are stamped into the JWT at sign-in.
> After changing a role's permissions, affected users must **sign out and back in**.

---

## 7. ? Follow-ups — Now Completed

All four items from the original follow-up list have been done:

### 7.1 Login contract bug (critical fix)
`LoginResponse.token` is an **object** (`JwtTokenModel`), not a string. The frontend was
treating it as a string, which would have sent `Authorization: Bearer [object Object]`.

```ts
// frontend/src/store/authStore.ts
const accessToken = res.token?.accessToken ?? null;   // ? unwrapped
```

`types/auth.ts` now mirrors the real contract (`JwtToken`, `AuthUser` with `userId` +
`permissions`), and `AppRole` became a plain `string` because roles are data-driven.

### 7.2 Endpoints migrated to `[HasPermission]`

| Endpoint | Permission |
|---|---|
| `POST /api/claims/{id}/submit` | `claims:submit` |
| `POST /api/claims/{id}/technical-review` | `claims:technical-review` |
| `POST /api/claims/{id}/medical-review` | `claims:medical-review` |
| `GET /api/claims/awaiting-review/{stage}` | `claims:read` |
| **All master-data reads** (13 controllers) | `masterdata:read` |
| **All master-data writes** | `masterdata:manage` |

### 7.3 Master data no longer Admin-only ?
Every master-data controller moved from `[Authorize(Roles = "Admin")]` to
`[Authorize] + [HasPermission(masterdata:read)]`, with `masterdata:manage` on
create/update/delete. This matches the Role?Feature matrix — `RCMViewer`,
`ClaimsProcessor`, `Auditor` etc. can now read master data.

### 7.4 Frontend permission gating
- **`usePermission()`** ? `can()`, `canAny()`, `canAll()`, plus a `PERMISSIONS` constant map
  mirroring `Permissions.cs`.
- **`<Can>`** component for declarative gating:

```tsx
<Can permission={PERMISSIONS.ClaimsSubmit}>
  <Button>Submit to NPHIES</Button>
</Can>
```

- `authStore.can()` mirrors the backend handler — **Admin implicitly passes everything**.

---

## 8. ?? Re-login & Test Plan

> ?? **You must sign out and sign back in.** Permissions are stamped into the JWT at
> sign-in, and your current token predates this work.

```bash
dotnet run --project NPhies_FHIR_Integration.ApiService   # migrates + seeds
cd frontend && npm run dev
```

**On startup, look for these log lines:**
```
? Seeded 28 permission(s).
? Seeded 13 role(s).
? Seeded N role-permission grant(s).
```

| # | Test | Expected |
|---|---|---|
| 1 | Log out, log in as `admin` | Succeeds (token now carries permissions) |
| 2 | User Management ? **Permissions** | 28 permissions, system ones show ?? |
| 3 | User Management ? **Roles** | 13 roles with permission counts |
| 4 | Create role `BillingSpecialist`, tick **Payments** + **MasterData** | Created |
| 5 | Create user `billing1` with that role | Created |
| 6 | **Effective Access** ? select `billing1` | Shows payments/masterdata perms + granting role |
| 7 | Log in as `billing1` ? **Master Data** | ? Grids load (previously 403 — this proves 7.3) |
| 8 | As `billing1`, `POST /api/claims/{id}/submit` | ? 403 (no `claims:submit`) |
| 9 | Grant `claims:submit` to the role, re-login as `billing1` | ? Now allowed |

Step 9 is the key proof: **access changed with no redeploy.**

---

## 9. ?? Remaining Notes

1. **Legacy RBAC tables** (`Roles`, `Permissions`, `UserRoles`, `RolePermissions`) remain but are
   unused — safe to drop in a later migration.
2. **Other controllers** still use `[Authorize(Roles = "...")]`. They keep working; migrate
   opportunistically as you touch them.
3. **VS TypeScript warnings** about `@/…` imports are a Visual Studio quirk. `npx tsc --noEmit`
   in `/frontend` is authoritative and **passes clean**.

---

## 10. ??? Next Step

Foundation is complete. Recommended next: **Claims + dual-approval UI** — the backend
(`technical-review`, `medical-review`, gated `submit`) is already permission-guarded and ready.

---

*Access control is now fully data-driven and manageable end-to-end from the UI.*
