# ?? Frontend Phase 0 — Scaffolding Report

> **What this document covers:** exactly what was created during **Phase 0 (Scaffold)** of the
> NPHIES RCM React frontend, why each piece exists, how to run it, and what comes next.
> Read this alongside `docs/FRONTEND_DEVELOPMENT_PLAN.md` (the overall plan).

---

## 1. ? Summary — What Phase 0 Delivered

A runnable **Vite + React + TypeScript + Ant Design** application located in `/frontend`, wired with:

- ?? **Authentication** (login, JWT storage, auto-refresh on 401)
- ??? **Protected routes** + **role-based** guards (RBAC)
- ?? **App shell** (sidebar nav + header + language toggle)
- ?? **Bilingual (EN/AR)** with RTL support
- ?? **Axios API client** with token interceptors
- ? **TanStack Query** for server state
- ??? **Zustand** auth store (persisted)
- ?? Placeholder pages for Dashboard, Master Data, Claims, Users
- ?? **Backend CORS** policy added for the dev server

Everything **compiles** and the .NET backend **build passes** with the new CORS policy.

---

## 2. ?? Files Created

```
frontend/
??? package.json         # Dependencies + scripts
??? vite.config.ts        # Vite config + /api proxy to .NET
??? tsconfig.json / tsconfig.node.json   # TypeScript (strict) config
??? index.html      # SPA entry HTML
??? .env         # VITE_API_BASE_URL, locale
??? .gitignore
??? .prettierrc          # Formatting rules
??? .eslintrc.cjs  # Linting rules (no `any`)
??? src/
    ??? main.tsx     # App bootstrap: AntD + React Query + i18n
    ??? App.tsx      # Routes (public + protected + admin)
    ??? vite-env.d.ts     # Typed env variables
    ??? api/
    ?   ??? client.ts       # Axios instance + JWT + refresh interceptor
    ?   ??? queryClient.ts     # TanStack Query defaults
    ?   ??? endpoints/
    ?       ??? auth.ts   # /api/auth wrappers (login/refresh/logout/me)
    ??? store/
    ?   ??? authStore.ts      # Zustand auth/session (persisted)
    ??? types/
    ?   ??? auth.ts         # AppRole, AuthUser, Login DTOs
    ??? routes/
    ?   ??? ProtectedRoute.tsx # Auth + role route guard
    ??? layouts/
    ?   ??? AppLayout.tsx   # Sidebar + header shell
    ??? locales/
    ?   ??? i18n.ts  # EN/AR translations + init
    ??? features/
        ??? auth/LoginPage.tsx
        ??? rcm-dashboard/DashboardPage.tsx
        ??? master-data/MasterDataPage.tsx
        ??? claims/ClaimsPage.tsx
   ??? user-management/UsersPage.tsx
```

---

## 3. ?? Tech Stack Installed (package.json)

| Package | Purpose |
|---------|---------|
| `react`, `react-dom` | UI runtime |
| `vite`, `@vitejs/plugin-react` | Build tool + fast HMR |
| `typescript` | Type safety (strict mode) |
| `antd`, `@ant-design/icons` | Enterprise UI components |
| `react-router-dom` | SPA routing + guards |
| `@tanstack/react-query` | Server state / caching / polling |
| `zustand` | Client state (auth/session) |
| `axios` | HTTP client with interceptors |
| `react-hook-form`, `zod` | Forms + schema validation (used in later phases) |
| `i18next`, `react-i18next` | Internationalization (EN/AR) |
| `dayjs` | Dates (AntD peer) |
| `eslint`, `prettier` | Linting + formatting |

> ?? These are declared but **not yet installed** — run `npm install` inside `/frontend`
> (see §7). No packages were fetched during scaffolding.

---

## 4. ?? How the API Layer Works

### 4.1 Axios client (`src/api/client.ts`)
- **Base URL** comes from `VITE_API_BASE_URL` (defaults to `/api`).
- **Request interceptor** attaches `Authorization: Bearer <token>` from the Zustand store.
- **Response interceptor** handles **401**:
  1. Calls the auth store's `refresh()` once.
  2. Queues concurrent requests while refreshing (prevents refresh stampede).
  3. Retries the original request with the new token.
  4. On failure ? clears session ? redirects to `/login`.

### 4.2 Auth endpoints (`src/api/endpoints/auth.ts`)
Typed wrappers around the **verified** backend contract:

| Function | Backend endpoint |
|----------|------------------|
| `authApi.login` | `POST /api/auth/login` |
| `authApi.refresh` | `POST /api/auth/refresh` |
| `authApi.logout` | `POST /api/auth/logout` |
| `authApi.me` | `GET /api/auth/me` (unwraps `ApiResponse<T>.data`) |

### 4.3 Auth store (`src/store/authStore.ts`)
Zustand store **persisted to localStorage** (`nphies-auth`) exposing:
- `login()`, `logout()`, `refresh()`
- `hasAnyRole(roles)` — used by route guards & (later) menu gating
- Tokens + `user` + `isAuthenticated`

---

## 5. ??? Routing & RBAC

### 5.1 Route map (`src/App.tsx`)
| Path | Access | Component |
|------|--------|-----------|
| `/login` | Public | `LoginPage` |
| `/` | Authenticated | `DashboardPage` |
| `/master-data` | Authenticated | `MasterDataPage` |
| `/claims` | Authenticated | `ClaimsPage` |
| `/users` | **Admin only** | `UsersPage` |
| `*` | — | redirect ? `/` |

### 5.2 Guard (`src/routes/ProtectedRoute.tsx`)
- Not logged in ? `Navigate` to `/login` (remembers intended path).
- Logged in but missing role ? AntD **403** `Result` page.
- The `/users` route demonstrates role gating with `roles={['Admin']}`, matching the
  **Role ? Feature matrix** in the development plan.

---

## 6. ?? Internationalization & Layout

- `src/locales/i18n.ts` seeds **English + Arabic** dictionaries.
- The header's language button toggles locale **and** sets `document.dir` to `rtl`/`ltr`.
- `AppLayout` provides the fixed sidebar, header (user avatar, logout, language), and a
  content `<Outlet />`.

> Full RTL theming via AntD `ConfigProvider direction="rtl"` + Arabic locale pack will be
> completed in the polish phase; the toggle + dictionaries are in place now.

---

## 7. ?? How to Run It

```bash
# 1. Install dependencies (first time only)
cd frontend
npm install

# 2. Start the Vite dev server (http://localhost:5173)
npm run dev
```

The dev server proxies **`/api` ? `https://localhost:7001`** (see `vite.config.ts`).
?? **Update that target port** to match your `ApiService` launch profile if different.

Other scripts:
```bash
npm run build     # Type-check + production build
npm run lint      # ESLint
npm run format    # Prettier
```

---

## 8. ?? Backend Change (CORS)

To let the React dev server call the API, a CORS policy was added to
`NPhies_FHIR_Integration.ApiService/Program.cs`:

```csharp
const string ReactDevCors = "ReactDevCors";
builder.Services.AddCors(options =>
{
 options.AddPolicy(ReactDevCors, policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
.AllowAnyHeader()
   .AllowAnyMethod()
       .AllowCredentials();
    });
});
// ...
app.UseCors(ReactDevCors);   // placed before MapControllers
```

? The backend **build passes** with this change.

> Two ways to reach the API in dev:
> 1. **Vite proxy** (default here) — `/api` is same-origin, so CORS isn't strictly needed.
> 2. **Direct call** — set `VITE_API_BASE_URL` to the full API URL; the CORS policy covers this.

---

## 9. ?? Known Follow-ups / Notes

1. **`npm install` required** — packages are declared but not installed.
2. **API port** — confirm the `ApiService` HTTPS port and update `vite.config.ts` proxy target.
3. **Login response shape** — the store expects `{ success, token, refreshToken, user }`.
   Verify the backend `LoginResponse` casing (JSON is usually camelCase) and adjust if needed.
4. **`/auth/me` unwrap** — assumes `ApiResponse<T>.data`. Adjust `authApi.me` if the shape differs.
5. **User Management** — the `/users` page is a placeholder; it needs the backend
   `UsersController` (`/api/users`) before real CRUD (tracked as Phase U2 in the plan).

---

## 10. ??? What's Next (per the plan)

| Phase | Next deliverable |
|-------|------------------|
| **1 — Auth** | ? Mostly done here (login, guards, refresh). Verify against live API. |
| **2 — App Shell** | ? Base shell done. Add breadcrumbs + role-filtered menu. |
| **3 — Master Data (read)** | First real screens — grids for seeded Payers/Services/Doctors. |
| **7 — Claims** | Claims list + **dual-approval** review UI (maps to the new backend endpoints). |
| **U2 — Users** | Admin user grid (after backend `UsersController`). |

**Recommended next step:** run `npm install && npm run dev`, log in against the API,
then start **Phase 3 (Master Data grids)** for the fastest visible win against real seeded data.

---

*Generated during Phase 0 scaffolding. Companion to `docs/FRONTEND_DEVELOPMENT_PLAN.md`.*
