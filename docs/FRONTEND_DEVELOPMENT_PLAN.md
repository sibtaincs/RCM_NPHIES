# 🏥 NPHIES RCM — Frontend Development Plan

> **Project:** Revenue Cycle Management (RCM) Operator Console
> **Backend:** NPHIES FHIR Integration API (.NET 9, Clean Architecture)
> **Frontend:** React + TypeScript + Ant Design
> **Status:** Planning → Development
> **Last Updated:** 2025

---

## 1. 🎯 Vision & Goal

Build the **RCM Operator Console** — a bilingual (English/Arabic) web application that lets
healthcare revenue-cycle staff run the **entire NPHIES workflow end-to-end** without ever
touching the database.

### Definition of Success
> A claims specialist can complete the full cycle —
> **Patient → Eligibility → Pre-Authorization → Claim → Response → Payment → Analytics** —
> entirely through the UI, backed by the existing .NET 9 API.

### Primary User Roles
| Role | Key Activities |
|------|----------------|
| **Claims Specialist** | Create/submit claims, track responses, manage denials |
| **Eligibility Officer** | Verify patient coverage before service |
| **RCM Manager** | View dashboards, KPIs, denial analytics |
| **Admin** | Manage master data (payers, services, doctors, codes) |

---

## 2. 🧱 Tech Stack

| Concern | Choice | Reason |
|---------|--------|--------|
| **Build Tool** | **Vite** | Fast HMR, modern (CRA is deprecated) |
| **Language** | **TypeScript** | Matches strongly-typed backend DTOs |
| **UI Library** | **Ant Design (AntD 5)** | Free enterprise grids, admin components, first-class RTL/Arabic |
| **Routing** | **React Router v6** | Standard SPA routing + guards |
| **Server State** | **TanStack Query (React Query)** | Caching + polling (fits NPHIES async polling) |
| **Client State** | **Zustand** | Lightweight auth/session & UI state |
| **Forms** | **React Hook Form + Zod** | Performant forms; validation mirrors backend `ValidationRules` |
| **HTTP Client** | **Axios** | Interceptors for JWT + refresh tokens |
| **Charts** | **Ant Design Charts / Recharts** | KPI dashboards |
| **Auth** | **JWT + Refresh Token** | Reuses existing backend `JwtSettings` + `RefreshToken` |
| **API Types** | **openapi-typescript** | Auto-generate TS types from Swagger/OpenAPI |
| **Linting** | **ESLint + Prettier** | Consistent code style |
| **Testing** | **Vitest + React Testing Library** | Unit/component tests |

---

## 3. 📁 Project Structure (Feature-Based)

```
frontend/
├── public/
├── src/
│   ├── api/
│   │   ├── client.ts     # Axios instance + JWT interceptor
│   │   ├── queryClient.ts     # TanStack Query config
│   │   └── endpoints/      # Typed API functions
│   │       ├── auth.ts
│   │       ├── patients.ts
│   │       ├── eligibility.ts
│   │       ├── preauth.ts
│ │       ├── claims.ts
│   │       ├── rcm.ts
│   │ └── masters.ts
│   ├── features/    # One folder per domain
│   │   ├── auth/      # Login, guards
│   │   ├── patients/
│   │   ├── eligibility/
│   │   ├── preauth/
│   │   ├── claims/
│   │   ├── rcm-dashboard/
│   │   └── master-data/
│   ├── components/      # Shared UI
│   │   ├── DataTable/
│   │   ├── StatusBadge/
│   │   ├── PageHeader/
│   │   └── WorkflowSteps/
│   ├── hooks/      # useAuth, usePolling, useDebounce
│   ├── layouts/        # AppLayout (sidebar, header)
│   ├── routes/        # Route config + ProtectedRoute
│   ├── store/    # Zustand stores
│   ├── types/   # TS interfaces (generated + manual)
│   ├── utils/    # formatters, constants
│   ├── locales/ # en.json, ar.json (i18n)
│   ├── App.tsx
│   └── main.tsx
├── .env        # VITE_API_BASE_URL
├── index.html
├── package.json
├── tsconfig.json
└── vite.config.ts
```

---

## 4. 🔌 Backend Integration Checklist

Confirm these on the **.NET API** before/while building the frontend:

- [ ] **CORS** — allow React dev origin `http://localhost:5173` in `Program.cs`
 *(This is the #1 blocker for local development.)*
- [ ] **OpenAPI/Swagger** — already enabled via `AddOpenApi()`; use it to generate TS types
- [ ] **Auth endpoints** — confirm login + refresh-token routes and request/response contract
- [ ] **Base URL** — the `ApiService` URL (via Aspire / launch settings)
- [ ] **Error format** — standardized `ProblemDetails` (already using `AddProblemDetails()`)

### API Surface (existing controllers)
| Controller | Frontend Feature |
|------------|------------------|
| `EligibilityController` | Eligibility |
| `PreAuthorizationController` | Pre-Auth |
| `ClaimController` | Claims |
| `RCMController` | Dashboard / Adjudication |
| Master Data Controllers | Master Data admin |
| `CodeableConceptController` | Code lookups / validation |

---

## 5. 🗺️ Development Roadmap (Milestones)

| Phase | Milestone | Deliverable |
|-------|-----------|-------------|
| **0** | **Scaffold** | Vite + TS + AntD + Router + React Query running with blank layout |
| **1** | **Auth** | Login page, JWT storage, protected routes, Axios interceptor + refresh |
| **2** | **App Shell** | Sidebar nav, header, breadcrumbs, EN/AR toggle |
| **3** | **Master Data (read)** | Grids for seeded data (Payers, Services, Doctors, Codes) — *fastest visible win* |
| **4** | **Master Data (CRUD)** | Create/Edit/Delete forms for master tables |
| **5** | **Patients + Eligibility** | Register patient, run eligibility check |
| **6** | **Pre-Authorization** | Submit & track pre-auth (with polling) |
| **7** | **Claims** | Create → submit → poll → view ClaimResponse |
| **8** | **RCM Dashboard** | KPIs, charts, denial analytics |
| **9** | **Polish** | i18n completeness, error handling, loading states, tests |

> 💡 **Why start with Master Data (Phase 3)?**
> The database is already seeded with payers, services, doctors, etc.
> Building these read grids first validates the entire **API → UI pipeline** early
> and gives immediate real-data feedback.

---

## 6. 🎨 UI / UX Conventions

- **Layout:** Fixed left sidebar (nav) + top header (user, language toggle) + content area
- **Tables:** AntD `Table` with server-side pagination, sorting, filtering
- **Forms:** AntD `Form` + React Hook Form + Zod schema validation
- **Status:** Color-coded `Tag`/`Badge` (Approved=green, Denied=red, Pending=orange)
- **Workflow:** AntD `Steps` component for the RCM lifecycle
- **Feedback:** `message`/`notification` for API results; `Result` page after submissions
- **Loading:** Skeletons + spinners via React Query `isLoading`
- **Bilingual:** `ConfigProvider` with `direction="rtl"` + Arabic locale toggle

---

## 7. 🔐 Authentication Flow

```
1. User submits credentials → POST /api/auth/login
2. Receive { accessToken, refreshToken, expiresIn }
3. Store tokens (memory + secure storage)
4. Axios request interceptor attaches: Authorization: Bearer <accessToken>
5. On 401 → Axios response interceptor calls refresh endpoint
6. Retry original request with new token
7. On refresh failure → redirect to /login
```

---

## 8. 🌍 Internationalization (EN / AR)

- Library: **react-i18next** + AntD locale packs (`en_US`, `ar_EG`)
- RTL handled by AntD `ConfigProvider direction`
- Backend already provides Arabic fields (`PayerNameArabic`, `MessageTypeArabic`, etc.)
- Translation files: `src/locales/en.json`, `src/locales/ar.json`

---

## 9. ⚙️ Environment & Configuration

`.env`
```
VITE_API_BASE_URL=https://localhost:7xxx/api
VITE_DEFAULT_LOCALE=en
```

---

## 10. 📋 Coding Standards

- **TypeScript strict mode** on
- **ESLint + Prettier** enforced (format on save)
- **Feature-based** folder structure (no giant shared folders)
- **React Query** for all server data (no manual `useEffect` fetching)
- **Zod schemas** co-located with forms
- **Named exports** for components; PascalCase files
- **No `any`** — generate types from OpenAPI

---

## 11. 👥 User Management & Access Control Module

A first-class module for managing application users, roles, and security — backed by the
existing backend auth infrastructure (`User`, `RefreshToken`, `LoginAttempt`, `AuditLog`,
`ApiRateLimitLog` entities + `AuthController`).

### 11.1 Scope
| Capability | Description |
|-----------|-------------|
| **Authentication** | Login, logout, token refresh, "current user" |
| **User CRUD** | Create, list, edit, deactivate/soft-delete users |
| **Role Assignment** | Assign one or more roles per user (RBAC) |
| **Account Security** | Lock/unlock, reset password, force password change |
| **MFA** | Enable/disable multi-factor auth (`IsMfaEnabled`, `MfaSecret`) |
| **Profile** | User views/edits own profile; change password |
| **Login Audit** | View login attempts (success/failure, IP, agent) |
| **Activity Audit** | View `AuditLog` (who did what, when) |
| **Rate-Limit Monitor** | View `ApiRateLimitLog` (admin/security) |

### 11.2 Existing Backend Contract (verified)

**Endpoints — `AuthController` (`/api/auth`):**
| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| `POST` | `/api/auth/login` | Login → `LoginResponse` (JWT + user) | Anonymous |
| `POST` | `/api/auth/refresh` | Refresh access token | Anonymous (refresh token) |
| `POST` | `/api/auth/logout` | Revoke session | Authenticated |
| `GET`  | `/api/auth/me` | Current user context | Authenticated |
| `GET`  | `/api/auth/health` | Auth service health | Anonymous |

> ⚠️ **Backend gap to add:** User-management CRUD endpoints
> (`GET/POST/PUT/DELETE /api/users`, role assignment, lock/unlock, reset password)
> are **not yet exposed** — only auth endpoints exist today. These must be added on the
> API before Phase U2 (see roadmap). Frontend will be built against this agreed contract.

**Roles (from `SecurityConstants`):**
| Role | Intended Use |
|------|--------------|
| `Admin` | Full system + user management |
| `RCMProcessor` | Process RCM workflows |
| `RCMViewer` | Read-only RCM/dashboards |
| `ClaimsProcessor` | Create/submit claims |
| `ClaimsReviewer` | Review/approve claims |
| `TechnicalReviewer` | Technical review of claims (coding, completeness, submission rules) |
| `MedicalReviewer` | Clinical/medical review of claims (diagnoses, medical necessity) |
| `PayerUser` | Payer-side access |
| `ProviderUser` | Provider-side access |
| `ComplianceOfficer` | Compliance oversight |
| `Auditor` | Read audit logs |
| `Accountant` | Payments, reconciliation & financial reporting |
| `User` | Base authenticated user |

**User entity fields (from `User.cs`):**
`Id, Username, Email, FirstName, LastName, Roles[], IsActive, IsEmailVerified,
IsMfaEnabled, IsLocked, FailedLoginAttempts, LastLoginAt, LockedUntilAt,
OrganizationId, DepartmentId, CreatedAt/By, UpdatedAt/By`

### 11.2a Role → Feature / Permission Matrix

Legend: **F** = Full (CRUD + actions) · **W** = Write (create/submit) · **R** = Read-only · **A** = Approve/Reject · **—** = No access

> 🔒 **Submit to NPHIES:** Only **`Admin`** and **`ClaimsProcessor`** may submit claims
> (and pre-authorizations) to the NPHIES gateway. Other write-capable roles can draft/prepare
> but the final **`$submit`** action is restricted to these two roles (enforced server-side).

| Feature / Controller | Admin | RCMProcessor | RCMViewer | ClaimsProcessor | ClaimsReviewer | Accountant | PayerUser | ProviderUser | ComplianceOfficer | Auditor | User |
|----------------------|:-----:|:------------:|:---------:|:---------------:|:--------------:|:----------:|:---------:|:------------:|:-----------------:|:-------:|:----:|
| **User Management** (`/api/users`) | F | — | — | — | — | — | — | — | — | — | — |
| **Roles / RBAC** | F | — | — | — | — | — | — | — | — | — | — |
| **Master Data** (Payers, Services, Doctors, Codes) | F | R | R | R | R | R | R | R | R | R | R |
| **Patients** | F | W | R | W | R | R | — | W | R | R | — |
| **Eligibility** (`EligibilityController`) | F | W | R | W | R | R | R | W | R | R | — |
| **Pre-Authorization** (`PreAuthorizationController`) | F+S | W | R | W+S | A | R | R | W | R | R | — |
| **Claims** (`ClaimController`) | F+S | W | R | W+S | A | R | R | W | R | R | — |
| **Claim Response / Adjudication** (`RCMController`) | F | W | R | R | A | R | R | R | R | R | — |
| **Denials & Appeals** | F | W | R | W | A | R | R | W | R | R | — |
| **Payments & Reconciliation** | F | R | R | — | — | F | R | R | R | R | — |
| **RCM Dashboard / Analytics** | F | R | R | R | R | R | R | R | R | R | — |
| **CodeableConcept / Validation** (`CodeableConceptController`) | F | R | R | R | R | R | R | R | R | R | R |
| **Login Audit** (`LoginAttempt`) | F | — | — | — | — | — | — | — | R | R | — |
| **Activity Audit** (`AuditLog`) | F | — | — | — | — | — | — | — | R | R | — |
| **Rate-Limit Monitor** (`ApiRateLimitLog`) | F | — | — | — | — | — | — | — | R | R | — |

> **Notes**
> - This matrix is the **agreed contract** for both frontend menu/route gating and backend
>   `[Authorize(Roles = ...)]` policies. Backend endpoints must enforce it server-side;
> the frontend only mirrors it for UX (hiding menus/buttons).
> - **`+S` = Submit to NPHIES.** Only **Admin** and **ClaimsProcessor** can submit claims &
>   pre-authorizations to the NPHIES gateway; all other write roles can draft/prepare only.
> - **ClaimsReviewer** gets **Approve/Reject (A)** on claims & pre-auth but not raw creation.
> - **Reviewers are split into two specializations** (both inherit the `ClaimsReviewer` column's
>   access — `A` on Claims & Pre-Auth, `R` elsewhere):
>   - **TechnicalReviewer** → reviews coding accuracy, completeness, modifiers, and
>  claim-submission-rule compliance (technical/billing correctness).
>   - **MedicalReviewer** → reviews diagnoses, procedures, and **medical necessity**
>     (clinical correctness). A claim may require **both** approvals before submission.
> - **Accountant** owns **Payments & Reconciliation (F)** and reads financial dashboards.
> - **ComplianceOfficer** and **Auditor** get **read** access to all audit trails.
> - **RCMViewer** is intentionally read-only across the RCM lifecycle.
> - **User** is the base role — access only to shared reference lookups (no PHI/workflow).

### 11.3 Frontend Feature Structure

```
src/features/user-management/
├── pages/
│   ├── UsersListPage.tsx     # AntD Table + filters
│   ├── UserFormPage.tsx      # Create / Edit (RHF + Zod)
│   ├── UserDetailPage.tsx    # Profile + roles + security
│   ├── RolesPage.tsx     # Role reference / assignment
│   ├── LoginAuditPage.tsx    # LoginAttempt table
│   └── ActivityAuditPage.tsx # AuditLog table
├── components/
│   ├── RoleTags.tsx  # Colored role badges
│   ├── UserStatusBadge.tsx   # Active/Locked/Inactive
│   └── PasswordResetModal.tsx
├── hooks/
│   ├── useUsers.ts        # React Query: list/get/create/update
│   └── useUserSecurity.ts    # lock/unlock, reset, MFA
├── schemas/
│   └── userSchemas.ts        # Zod: create/edit/password
└── api/usersApi.ts
```

### 11.4 RBAC in the Frontend
- **Route guards** read roles from JWT claims (via Zustand auth store).
- **`<RequireRole roles={['Admin']}>`** wrapper hides/blocks unauthorized routes.
- **Component-level gating** hides action buttons (e.g., "Delete User" only for `Admin`).
- Menu items filtered by role so users only see permitted sections.

### 11.5 Security UX Rules
- Passwords never shown; strength meter on create/change (mirror backend policy:
  min 12 chars, history, expiry — see `SecurityConstants`).
- Lock indicator + "unlock" action (Admin only).
- Session timeout handling (auto-logout on refresh failure).
- Sensitive actions (delete, role change, unlock) require confirmation modals.
- All admin actions surface in the **Activity Audit** view.

### 11.6 User-Management Roadmap (sub-phases)
| Phase | Milestone | Deliverable |
|-------|-----------|-------------|
| **U0** | Auth core | Login, refresh, logout, `me`, protected routes (overlaps main Phase 1) |
| **U1** | Profile | View/edit own profile, change password |
| **U2** | User list (read) | Admin users grid (needs backend `GET /api/users`) |
| **U3** | User CRUD | Create/edit/deactivate users + role assignment |
| **U4** | Account security | Lock/unlock, reset password, MFA toggle |
| **U5** | Audit views | Login attempts + activity audit + rate-limit monitor |

---

## 12. ✅ Pre-Development Decisions (Confirmed)

| Decision | Choice |
|----------|--------|
| UI Library | **Ant Design 5** |
| Build Tool | **Vite** |
| Language | **TypeScript** |
| Location | `frontend/` folder inside this repo |
| First Feature | **Master Data grids** |
| Bilingual | **Yes (EN/AR)** |

### Open Questions (to confirm before Phase 1)
1. ✅ **Login endpoint** — `POST /api/auth/login` → `LoginResponse { Success, Message, Token, User }`;
   refresh via `POST /api/auth/refresh`; current user via `GET /api/auth/me`. *(Verified in `AuthController`.)*
2. **API base URL** — confirm the Aspire-assigned HTTPS port for `ApiService` (from `launchSettings.json`).
3. **Aspire integration** — a Blazor `NPhies_FHIR_Integration.Web` project already exists in the AppHost.
   Decide: replace/retire it with the new React app, run React standalone, or add React to AppHost orchestration.
4. **User CRUD API** — add `UsersController` (`/api/users`) on the backend before Phase U2
   (currently only auth endpoints exist).

dotnet ef migrations add AddClaimDualApprovalFields --project NPhies_FHIR_Integration.Infrastructure --startup-project NPhies_FHIR_Integration.ApiService
dotnet ef database update  --project NPhies_FHIR_Integration.Infrastructure --startup-project NPhies_FHIR_Integration.ApiService
