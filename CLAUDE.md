# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Revenue cycle management (RCM) for Saudi healthcare providers, integrated with **NPHIES**, the national health insurance exchange. It covers eligibility, prior authorization, claims, polling, communications, payments, denials and appeals, and uses FHIR R4 message bundles posted to `$process-message` over **mutual TLS**, not OAuth. The backend is ASP.NET Core 9 with EF Core on SQL Server and the Firely SDK (`Hl7.Fhir.R4`). The frontend is React 18 + TypeScript + Vite + Ant Design in `frontend/`.

The system is **not production ready**. `docs/PROJECT_STATUS.md` lists the known defects, gaps and the phased roadmap; read it before larger changes.

## Commands

```bash
# Backend (from the repo root). The projects target net9.0.
dotnet build NPhies_FHIR_Integration.sln
dotnet run --project NPhies_FHIR_Integration.ApiService --launch-profile https   # https://localhost:7590

# Tests. The RCM workflow tests need SQL Server; point them at one:
export NPHIES_TEST_SQLSERVER="Server=localhost,1433;User Id=sa;Password=...;TrustServerCertificate=true"
dotnet test NPhies_FHIR_Integration.sln
dotnet test NPhies_FHIR_Integration.Tests --filter "FullyQualifiedName~RcmClaimWorkflowTests"          # one class (MSTest)
dotnet test NPhies_FHIR_Integration.Tests --filter "FullyQualifiedName~RcmClaimWorkflowTests.<Method>" # one test
dotnet test NPhies_FHIR_Integration.LegacyTests --filter "FullyQualifiedName~DeductibleRuleTests"      # xUnit

# Throw-away SQL Server for tests or a local run
docker run -d --name nphies-sql -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=Test_Passw0rd!" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest

# Frontend (from frontend/). The Vite dev server proxies /api to https://localhost:7590.
npm ci && npm run dev    # http://localhost:5173
npm run lint
npm run build            # tsc -b && vite build

# EF Core migrations (Infrastructure holds the DbContext; ApiService is the startup project)
dotnet ef migrations add <Name> --project NPhies_FHIR_Integration.Infrastructure --startup-project NPhies_FHIR_Integration.ApiService
dotnet ef migrations script --idempotent --project NPhies_FHIR_Integration.Infrastructure --startup-project NPhies_FHIR_Integration.ApiService -o migrations.sql
```

**Test behavior:**
- Without `NPHIES_TEST_SQLSERVER`, `RcmTestHost` uses `localhost` with Windows authentication, and marks the DB tests **Inconclusive** if it can't connect. `NPHIES_TEST_REQUIRE_SQLSERVER=true` turns that into a failure; CI sets it.
- Each run creates `NPhiesRcmTests_<guid>` and drops it at assembly cleanup.
- CI (`.github/workflows/ci.yml`) builds Release, runs both test projects against a SQL Server service container, then lints and builds the frontend.

**Local configuration:**
- Secrets come from user-secrets or environment variables, never `appsettings*.json`: `ConnectionStrings:DefaultConnection`, `JwtSettings:Secret` (32+ characters) and `Seed:AdminPassword`.
- Outside Development, startup fails if the first two are missing (`ApiService/Security/SecretConfiguration.cs`).
- `Nphies:Sandbox:Enabled=true` routes all NPHIES traffic to an in-process simulator (`Application/Nphies/Sandbox/NphiesSandboxTransport.cs`), so the app runs without a certificate.

**Development startup:**
- Only in Development does `Program.cs` apply migrations and run the seeders (sample hospitals, payers, patients and claims, the `admin` user, roles and permissions).
- Known bug: if the database does not exist yet, `CanConnectAsync()` returns false and migrations are skipped. Create the empty database first.

## Architecture

### Two stacks: build on the new one

- **Current, tested path:**
  - `Application/Nphies/`: the messaging core.
  - `Application/Rcm/`: the RCM workflows.
  - `Infrastructure/Nphies/`: the mTLS transport, certificate provider and message audit store.
  - `ApiService/Controllers/Rcm/`: the controllers, under `api/rcm/*`.
- **Legacy:**
  - `Application/Services/*` (about 40k lines) and most controllers directly under `ApiService/Controllers/`.
  - Several of these services are stubs or simulate their work: `PaymentService` and `ReportingService` contain `TODO`s; webhooks, API keys and notifications are held in memory.
  - Many folders are referenced by no API code: Analytics, MachineLearning, Reporting, Infrastructure, Search, Tenants, Enterprise.
  - Phase 1 of the roadmap consolidates onto the new path. Don't extend the legacy services.

### NPHIES messaging core (`docs/nphies-messaging-core.md`)

Workflow code must use these interfaces, never raw HTTP:

- **`INphiesBundleBuilder`** (`Nphies/Builders`) builds message bundles from the input models in `Nphies/Models/NphiesBuilderInputs.cs`. Profile, extension, code-system and identifier URLs are typed constants in `Nphies/Constants/NphiesConstants.cs`.
- **`INphiesBundleValidator`** runs before every send. It combines two layers:
  - business rules (`BV-*`/`IC-*`/`GE-*`/`NPHIES-*` rule ids);
  - IG profile conformance against the embedded `nphies-fs` package (`Nphies/Resources/nphies-fs-package.tgz`).
  - `Nphies:ProfileValidation:Mode` is `Off`, `Warn` (the default) or `Enforce`.
- **`INphiesMessagingClient`** validates, serializes, posts, audits (`NphiesMessageLogs`) and parses the reply with **`INphiesResponseParser`** into an `NphiesResponse`. Errors are enriched from the embedded error catalog.
- **Retries happen only for idempotent events** (poll, status-check); see `GetRetryPolicy` in `Application/Extensions/ServiceCollectionExtensions.cs`.
- **`NphiesTimeoutException` means the message may have been delivered:** recover with a status-check or poll, never a blind resend.

### RCM workflow layer (`docs/rcm-workflows.md`)

1. Services load EF entities and map them through `RcmInputMapper` to builder inputs.
2. `RcmMessageDispatcher` **persists an `NphiesTransaction` (Pending) before sending**, then records Completed, Queued, Error, ValidationFailed or Timeout.
3. Every reply goes through the single idempotent **`RcmResponseIngestor`**, whether it arrives synchronously, through polling (`RcmPollService` and `ApiService/BackgroundServices/NphiesPollWorker`, enabled by `Rcm:PollWorkerEnabled`) or through `POST api/rcm/messages/inbound`.
   - Duplicates are detected by the inbound MessageHeader id.
   - Correlation to the request is by transaction or by request identifier.
4. `Claim.NphiesStatus` moves through `RcmClaimStatus` in `Rcm/RcmOptions.cs`: Draft, then Submitted, Queued or Pended, then Approved, PartiallyApproved or Rejected, then Paid, PartiallyPaid or Recouped.
   - Side states include Timeout, Cancelled, Nullified and Superseded.
   - Prior authorizations use the same statuses.
5. `RcmWorkflowException` maps to 400, 404 or 409 through `RcmExceptionFilter` in `RcmControllerBase`.
   - 409 is returned for a wrong state, a review gate, or a timeout that needs a status-check.
   - Claims require approved technical and medical review before sending when `Rcm:RequireReviewApprovalForClaims` is set.

Amounts adjudicated by the payer (the ClaimResponse) are the source of truth. The provider-side engines (`PaymentCalculationEngine`, `Services/RCM/Rules/*`) only estimate, and have known defects; see the skipped tests in `LegacyTests`.

### Security and multi-hospital scoping

- JWT bearer authentication with a **fallback policy that requires an authenticated user on every endpoint**. Use `[AllowAnonymous]` explicitly.
- Fine-grained access uses `[HasPermission(Permissions.X)]`, resolved from database-driven roles (`ApiService/Security/`).
- `RcmControllerBase.Scope()` limits non-admin users to their organization (hospital) through the organization claim.
- Only the `api/rcm/*`, master-data and user/role controllers apply permissions and scoping. The legacy controllers don't yet.

### Data

- `ApplicationDbContext` and its entity configurations live in `Infrastructure/Data/Configurations/`; migrations are in `Infrastructure/Migrations/`.
- The Application project references Infrastructure and uses `ApplicationDbContext` directly.
- There are about 94 tables and string ids throughout.

### Tests

- **`NPhies_FHIR_Integration.Tests`** (MSTest) compiles only `Nphies/**`. It covers:
  - validation and parsing of every docs example;
  - golden round-trips;
  - full RCM workflows through the real services and EF Core, with `FakeNphiesTransport` replying with the docs examples (`RcmTestHost`);
  - endpoint authorization;
  - a regression test that every seeded claim conforms to the IG.
- **`NPhies_FHIR_Integration.LegacyTests`** (xUnit, Moq, EF InMemory) covers the legacy services. Delete each test together with the code it covers when consolidating.

## NPHIES reference material

`nphies-fhir-docs-main/` holds:
- the Implementation Guide PDF (V2.6);
- the onboarding test cases (Eligibility V4.0, Authorization V6.0, Claims V4.0, Payment V3.0);
- spreadsheets of profiles, CodeableConcepts and validation error codes;
- about 40 example request and response bundles. The tests copy these to `DocsExamples/` and use them as fixtures.

`docs/archive/` describes a superseded OAuth design; ignore it.
