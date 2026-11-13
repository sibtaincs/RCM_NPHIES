# Project status

Last reviewed: November 2026 (Phase 0 baseline).

**The system is not production ready.** It can build, validate and exchange NPHIES messages, and the core RCM workflows are covered by integration tests. Security hardening, deployment, multi-hospital certificates, HIS integration and NPHIES certification are still to do. This file replaces the earlier status reports, which overstated completion.

## What works and is tested

These are covered by `NPhies_FHIR_Integration.Tests` (179 tests; the workflow tests run against a real SQL Server):

- **Messaging core** (`Application/Nphies`, `Infrastructure/Nphies`):
  - bundle builder for every message type in the NPHIES docs examples;
  - business-rule and IG profile validation;
  - response parser and error catalog;
  - mutual-TLS transport, with retries only for idempotent messages;
  - message audit.
- **RCM workflows** (`Application/Rcm`):
  - eligibility, prior authorization, claims and batches;
  - status-check, cancel and poll;
  - communications;
  - payment reconciliation, including recoupment;
  - denials and appeals;
  - the claim editor with review gates;
  - claim documents;
  - evidence export for NPHIES test cases.
  - Each outbound transaction is stored before it is sent, and inbound messages are ingested idempotently.
- **Access control:** permission-based authorization on the `api/rcm/*` controllers, with scoping by hospital.

## Known defects

These were found by tests during the Phase 0 review. Each one is a skipped test in `NPhies_FHIR_Integration.LegacyTests`, and the skip reason explains the defect.

| Area | Defect | Impact today |
|---|---|---|
| `PaymentCalculationEngine.CalculateBenefit` | Starts the patient's share at the not-allowed amount (submitted − allowed), so the patient is charged the contractual discount. | Low: only reached through `PaymentService`, whose methods are still `TODO` stubs. |
| `OutOfPocketRule` | Moves the insurer's remaining share to the patient, up to the out-of-pocket limit. | Used by the legacy `RCMService` estimate, not by NPHIES adjudication. |
| `DeductibleRule` | Reports "applied" with a zero amount when the deductible is already met. | Cosmetic. |
| `EligibilityService` (legacy) | Wraps validation and not-found errors in `InvalidOperationException`, so the API returns 500 instead of 400/404. | Legacy `api/v1/Eligibility` endpoints. |

The payer's adjudication in the NPHIES ClaimResponse is the source of truth for amounts. The provider-side engines above only produce estimates.

## Not production ready yet

1. **Two parallel stacks.** The older `Application/Services/*` code (about 40k lines) overlaps with `Rcm/`. Several services are stubs or simulate their work:
   - `PaymentService`, `ReportingService` and `PreAuthorizationService` contain `TODO` placeholders;
   - webhooks, API keys and notifications are held in memory.
   - Folders not referenced by the API: Analytics, MachineLearning, Reporting, Infrastructure, Search, Tenants, Enterprise.
2. **Security:**
   - The legacy controllers only require a login; they have no permission checks and no hospital scoping.
   - PBKDF2 uses 10,000 iterations.
   - Refresh tokens are stored in plain text.
   - Rate limiting is in memory.
   - The configured field encryption is not implemented.
   - The frontend keeps the JWT in `localStorage`.
   - The admin seeder has a default password fallback.
3. **Deployment:**
   - Migrations and seeding run only in Development.
   - There is no container image or infrastructure-as-code.
   - `ProductionBaseUrl` still points at the sandbox host.
   - The cache is in memory.
   - The poll worker is not safe to run on several instances.
   - Documents are stored on the local filesystem.
4. **Multi-hospital:** there is one client certificate and one provider licence per deployment.
5. **HIS integration:** there is no inbound API or adapter, and no outbound notification to a hospital information system.
6. **NPHIES certification:**
   - The embedded IG package is `nphies-fs` 1.0.0, with IG V2.6 documents. Confirm the current version on the NPHIES portal.
   - Profile validation defaults to `Warn`.
   - The onboarding test suites have not been run against the NPHIES sandbox.
7. **Frontend:** the Arabic translation and right-to-left layout are incomplete, and there are no automated tests.
8. **Code quality:** about 600 compiler warnings, mostly nullable-reference warnings.

## Roadmap

| Phase | Goal |
|---|---|
| 0. Baseline | Build and all tests in CI, repository clean-up, honest status. **Done.** |
| 1. One stack | Make `Rcm/` + `Nphies/` the only path. Port or retire legacy controllers and services. Enforce permission checks on every endpoint. |
| 2. Security and compliance | Identity hardening, encryption of patient identifiers, audit, vault-managed secrets. Map against PDPL, NCA controls and data residency. |
| 3. NPHIES conformance | Current IG, per-organization certificates, `Enforce` validation, sandbox runs of every onboarding test case, certification evidence. |
| 4. HIS integration | Versioned inbound API with idempotency, HL7 v2 adapter path, code mapping, outbox and webhooks, one pilot hospital. |
| 5. Operations | Containers, migration bundle, Redis, object storage, observability, backups, load and penetration tests. |
| 6. Frontend | Arabic and RTL, remaining screens, end-to-end tests. |
| 7. Pilot and go-live | Sandbox, then production certification, parallel run, then go-live. |
