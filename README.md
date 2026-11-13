# NPHIES RCM

Revenue cycle management for healthcare providers in Saudi Arabia, integrated with **NPHIES** (the national health insurance exchange) using FHIR R4 messaging over mutual TLS.

- **Backend:** ASP.NET Core 9, EF Core (SQL Server), Firely FHIR SDK
- **Frontend:** React 18 + TypeScript, Vite, Ant Design

> **Status:** not production ready. See [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md) for what works, the known defects and the roadmap.

## Repository layout

| Path | Contents |
|---|---|
| `NPhies_FHIR_Integration.ApiService` | REST API host: controllers, JWT security, background poll worker |
| `NPhies_FHIR_Integration.Application` | `Nphies/`: messaging core (builder, validator, parser, client). `Rcm/`: RCM workflows. `Services/`: older services being consolidated |
| `NPhies_FHIR_Integration.Infrastructure` | EF Core `ApplicationDbContext`, migrations, seeders, mTLS transport, certificate provider |
| `NPhies_FHIR_Integration.Domain` | Entities |
| `NPhies_FHIR_Integration.Common` | Shared constants and models |
| `NPhies_FHIR_Integration.AppHost`, `.ServiceDefaults` | .NET Aspire host and defaults (OpenTelemetry, health checks) |
| `NPhies_FHIR_Integration.Tests` | MSTest: messaging core, RCM workflows (needs SQL Server), security |
| `NPhies_FHIR_Integration.LegacyTests` | xUnit tests for the older `Services/` code |
| `frontend` | React RCM console |
| `nphies-fhir-docs-main` | NPHIES reference material: IG, code systems, validation codes, test cases, example messages |
| `docs` | Design documentation |

## Prerequisites

- .NET 9 SDK
- Node.js 22
- SQL Server 2019 or later. On Windows, LocalDB is used automatically in Development; elsewhere, use Docker as shown below.

## Running locally

### 1. Configure secrets

Keep secrets out of `appsettings*.json`; use user-secrets:

```bash
dotnet user-secrets --project NPhies_FHIR_Integration.ApiService set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=NPhiesDb;User Id=sa;Password=<password>;TrustServerCertificate=true"
```

```bash
dotnet user-secrets --project NPhies_FHIR_Integration.ApiService set "JwtSettings:Secret" "<random string of at least 32 characters>"
```

```bash
dotnet user-secrets --project NPhies_FHIR_Integration.ApiService set "Seed:AdminPassword" "<initial admin password>"
```

NPHIES settings (provider licence, client certificate, environment) are described in [docs/nphies-messaging-core.md](docs/nphies-messaging-core.md#configuration-nphies-section). Without a certificate you can use the built-in simulator by setting `Nphies:Sandbox:Enabled=true`.

### 2. Start the API

```bash
dotnet run --project NPhies_FHIR_Integration.ApiService --launch-profile https
```

In Development the API applies migrations, seeds sample data and creates the `admin` user. It listens on `https://localhost:7590`.

### 3. Start the frontend

```bash
cd frontend
```

```bash
npm ci
```

```bash
npm run dev
```

Open `http://localhost:5173`. Vite proxies `/api` to the API. Copy `frontend/.env.example` to `frontend/.env` if you need to change the defaults.

## Tests

The RCM workflow tests need SQL Server. Start a throw-away instance with Docker:

```bash
docker run -d --name nphies-sql -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=Test_Passw0rd!" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
```

Point the tests at it. Without this variable they use `localhost` with Windows authentication, and are marked inconclusive if no server is reachable.

```bash
export NPHIES_TEST_SQLSERVER="Server=localhost,1433;User Id=sa;Password=Test_Passw0rd!;TrustServerCertificate=true"
```

```bash
dotnet test NPhies_FHIR_Integration.sln
```

Set `NPHIES_TEST_REQUIRE_SQLSERVER=true` to make the run fail, instead of skipping, when the database is unreachable. CI does this.

Each test database is created with a unique name and dropped at the end of the run.

## Continuous integration

`.github/workflows/ci.yml` runs on every pull request and on pushes to `main`:

- **Backend:** restore, Release build, both test projects against a SQL Server service container; test results are uploaded as an artifact.
- **Frontend:** `npm ci`, lint and production build.

## Documentation

| Document | Topic |
|---|---|
| [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md) | Current status, known defects, roadmap |
| [docs/nphies-messaging-core.md](docs/nphies-messaging-core.md) | Building, validating, sending and parsing NPHIES messages; configuration |
| [docs/rcm-workflows.md](docs/rcm-workflows.md) | RCM workflows, claim statuses, payments, denials, routes |
| [docs/RCM_SYSTEM_OVERVIEW.md](docs/RCM_SYSTEM_OVERVIEW.md) | System overview |
| [docs/RBAC_ROLES_PERMISSIONS.md](docs/RBAC_ROLES_PERMISSIONS.md) | Roles and permissions |
| [docs/master-data.md](docs/master-data.md) | Master data |
| [docs/CODEABLE_CONCEPTS_EXPLAINED.md](docs/CODEABLE_CONCEPTS_EXPLAINED.md) | NPHIES code systems |
| [docs/archive](docs/archive) | Superseded guides, kept for reference |
