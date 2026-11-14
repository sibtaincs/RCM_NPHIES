# Project status

Last reviewed: November 2026 (Phase 1 clean-up).

**The system is not production ready.** It can build, validate and exchange NPHIES messages, and the core RCM workflows are covered by integration tests. Security hardening, deployment, multi-hospital certificates, HIS integration and NPHIES certification are still to do. This file replaces the earlier status reports, which overstated completion.

## What works and is tested

These are covered by `NPhies_FHIR_Integration.Tests` (199 tests; the workflow tests run against a real SQL Server):

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

| Area | Defect | Impact today |
|---|---|---|
| `AuditController` (`/api/audit/*`) | `IPermissionAuditService` has no registered implementation, and the old one read entities that EF doesn't map. | Every audit endpoint fails at runtime. The tests register a fake, which hid this. Rebuild the controller on the `AuditLogs` table. |

The legacy defects listed in the Phase 0 review (`PaymentCalculationEngine`, `OutOfPocketRule`, `DeductibleRule` and the legacy `EligibilityService`) went away with that code in Phase 1. The payer's adjudication in the NPHIES ClaimResponse is the only source of truth for amounts.

## Not production ready yet

1. **Phase 1 leftovers.**
   - The claim review gate (`ClaimsController` and `Application/Services/ClaimService.cs`) still lives outside `api/rcm/*`.
   - `PreAuthorization*` and about a dozen other tables are no longer used by any code. Dropping them needs a migration.
2. **Security:**
   - PBKDF2 uses 10,000 iterations.
   - Refresh tokens are stored in plain text.
   - Rate limiting is in memory.
   - The configured field encryption is not implemented.
   - The frontend keeps the JWT in `localStorage`.
   - The admin seeder has a default password fallback.
   - NuGet reports high-severity advisories for `AutoMapper` 12.0.1 (GHSA-rvv3-g6hj-g44x) and `Aspire.Hosting.AppHost` 9.5.0 (GHSA-f7vh-h5g3-7jrh). Check the licence of newer AutoMapper versions before upgrading, or replace its few remaining mappings with hand-written code.
3. **Deployment:**
   - Migrations and seeding run only in Development.
   - There is no container image or infrastructure-as-code.
   - `ProductionBaseUrl` still points at the sandbox host.
   - The cache is in memory.
   - The poll worker is not safe to run on several instances.
   - Documents are stored on the local filesystem.
4. **Multi-hospital:** there is one client certificate and one provider licence per deployment.
5. **HIS integration:** there is no inbound API or adapter, and no outbound notification to a hospital information system.
6. **NPHIES certification:** see [NPHIES_GAP_ANALYSIS.md](NPHIES_GAP_ANALYSIS.md). 9 of the 25 test cases are blocked, mainly by missing newborn support, the transfer flag on eligibility, references from claims to earlier responses, and advanced authorization cancellation.
   - The embedded IG package is `nphies-fs` 1.0.0, with IG V2.6 documents. Confirm the current version on the NPHIES portal.
   - Profile validation defaults to `Warn`.
   - The onboarding test suites have not been run against the NPHIES sandbox.
7. **Frontend:** the Arabic translation and right-to-left layout are incomplete, and there are no automated tests.
8. **Code quality:** about 180 compiler warnings, mostly nullable-reference warnings.

## Roadmap

| Phase | Goal |
|---|---|
| 0. Baseline | Build and all tests in CI, repository clean-up, honest status. **Done.** |
| 1. One stack | Make `Rcm/` + `Nphies/` the only path. Port or retire legacy controllers and services. Enforce permission checks on every endpoint. **Mostly done:** the legacy services and controllers are removed; the review gate still has to move and the unused tables still have to be dropped. |
| 2. Security and compliance | Identity hardening, encryption of patient identifiers, audit, vault-managed secrets. Map against PDPL, NCA controls and data residency. |
| 3. NPHIES conformance | Current IG, per-organization certificates, `Enforce` validation, sandbox runs of every onboarding test case, certification evidence. |
| 4. HIS integration | Versioned inbound API with idempotency, HL7 v2 adapter path, code mapping, outbox and webhooks, one pilot hospital. |
| 5. Operations | Containers, migration bundle, Redis, object storage, observability, backups, load and penetration tests. |
| 6. Frontend | Arabic and RTL, remaining screens, end-to-end tests. |
| 7. Pilot and go-live | Sandbox, then production certification, parallel run, then go-live. |
