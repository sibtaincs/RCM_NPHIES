# NPHIES RCM workflows

The RCM workflow layer (`NPhies_FHIR_Integration.Application/Rcm`) drives every NPHIES provider use case end to end on top of the messaging core (`docs/nphies-messaging-core.md`). Controllers live in `ApiService/Controllers/RcmControllers.cs`; DI is `services.AddRcmWorkflows(configuration)`.

## How a message flows

1. A service loads the EF entities and maps them through `RcmInputMapper` → `INphiesBundleBuilder`.
2. `RcmMessageDispatcher` writes an outbound `NphiesTransaction` row (status `Pending`), sends via `INphiesMessagingClient`, and records `Completed` / `Queued` / `Error` / `ValidationFailed` / `Timeout`.
3. A synchronous reply goes through `RcmResponseIngestor`, the same idempotent ingestor that the poll worker and `POST /api/rcm/messages/inbound` use.
   - Duplicates are detected by inbound MessageHeader id.
   - Payment reconciliations and communication requests are detected by resource id/identifier.
   - Replies are correlated to the request by transaction, or by the request identifier (`Claim.ClaimIdentifierValue`, Task focus, CommunicationRequest `about`, payment detail `request`).
   - A Task reply whose focus identifier does not match the claim is not applied; it is stored as `Unmatched`.
4. A timeout leaves the claim in `Timeout`. Resending is refused; run a status-check or poll instead.

## Claim statuses (`Claim.NphiesStatus`)

`Draft` → `Submitted` / `Queued` / `Pended` → `Approved` / `PartiallyApproved` / `Rejected` → `Paid` / `PartiallyPaid`

The side states are:
- `Error`
- `ValidationFailed`
- `Timeout`
- `Cancelled`
- `Nullified`
- `Superseded`: the claim was replaced by an update, extension or resubmission. This is set when the new version is sent.

Prior authorizations use the same statuses. The granted `preAuthRef` and `preAuthPeriod` are stored on the `ClaimResponse`, and `PreAuthPeriodEnd` is copied to the claim. A claim created from, or linked to, a prior authorization carries the ref in `Claim.PreAuthorizationRef`. Linking an expired authorization is refused.

## Payments

`PaymentReconciliationDetail` is matched to a claim by `request.identifier` (with `use = claim`).

- **Expected payable** is `benefit + tax` from the latest ClaimResponse. In example 7b → 13a, the benefit is 170 and the VAT is 25.5, so the expected payable is 195.5.
- **Paid** is the `component-payment` extension, which is the gross amount before the NPHIES and early fees. Detail `amount` is the net cash.
- **Statuses:** a detail is `Matched`, `Underpaid`, `Overpaid` or `Unmatched`. A reconciliation is `Matched`, `Discrepancy`, `PartiallyMatched` or `Unmatched`.
- **Acknowledgement:** a payment-notice is sent automatically (`Rcm:AutoAcknowledgePayments`) or through `POST /payments/{id}/acknowledge`.

## Denials and appeals

A `ClaimDenial` row is created for each adjudicated item that has a reason code or a reduced or zero benefit. Denials are categorised (Contractual, Technical, Duplicate, …) and given an appeal deadline (`Rcm:AppealWindowDays`).

For each open denial the user can do one of three things (the Denials & appeals screen has a button for each):

- **Appeal in the same claim** (`POST /appeals` with `resubmit: false`, method `Communication`). This follows the Claim Re-submission guide.
  - It sends a `communication` about the same claim with `reasonCode = re-adjudication`, and one text payload per denied service carrying `extension-ClaimItemSequence`.
  - The text is the service's own justification (`items`), else the first of `texts`, else the reason. With a single service, attachments are linked to it.
  - A denial of the whole claim (no item) names every service.
  - The communication goes first. If NPHIES refuses it (for example BV-00148, a claim that is not posted and active), the appeal is `Failed` and the denials stay as they were.
  - The service refuses a re-adjudication for a prior authorization (400), and for a claim that is cancelled, answered with errors, superseded or not yet adjudicated (409).
- **Resubmit** (`resubmit: true`, method `Resubmission`). The claim is cloned with `related = prior` and all its services, then sent. Texts and attachments go in a separate communication. When the new version is adjudicated, the appeal and the original denials are resolved.
- **Write off**: set the denial to `WrittenOff` (`PATCH /denials/{id}`).

The payer answers a re-adjudication by poll, and `RcmResponseIngestor` applies the answer:

- **A reissued ClaimResponse** (`reissue-reason = adjudication`, stored in `ClaimResponse.ReissueReason`). Each appealed denial is decided:
  - the service is no longer denied: `Overturned`;
  - still denied: `Upheld`, and the row stays the record of that denial instead of a new `Open` one;
  - denied less: split into an `Overturned` row for the recovered amount and an `Upheld` row for the rest.
- **A `notification` CommunicationRequest with `reasonCode = re-adjudication`.** The services it names (all of them if it names none) become `Upheld`, with the payer's text in the notes. A notification needs no reply, so it is not an open request.

The appeal is then `Decided`:
- the outcome is `Overturned`, `PartiallyOverturned` or `Upheld`;
- `recoveredAmount` is the sum of the overturned rows;
- the explanation is the payer's text.

An upheld denial from a same-claim appeal is still live. It can be appealed again (the level goes up), accepted or written off, and a later resubmission carries it. Appeal numbers count every appeal on the claim, so a withdrawn appeal keeps its number. Prior-authorization denials are appealed by sending an update (`POST /preauths/{id}/update`).

The item link is also available on any communication. `items` are texts about single services, `attachments[].itemSequence` links a document to one service, and each sequence must be a service of the claim. The extension URL `extension-ClaimItemSequence` comes from the ClaimItem guide V1.1. It is not in the IG package (Q1), so profile validation only warns about it, and inbound messages are matched without regard to case.

## Routes

| Area | Routes |
|---|---|
| Eligibility | `POST /api/rcm/eligibility`, `POST /api/rcm/eligibility/insurance-discovery` (NPHIES Check Insurance lookup by identity number), `POST /api/rcm/eligibility/insurance-discovery/coverage` (save a discovered policy as a coverage), `POST /api/rcm/eligibility/offline` (record a portal or phone confirmation), `POST /api/rcm/eligibility/{id}/apply-to-claim/{claimId}` (put the offline reference and date on an unsent claim), `GET /api/rcm/eligibility`, `GET /api/rcm/eligibility/{id}`, `GET /api/rcm/eligibility/patients/{patientId}/latest` |
| Claims | `GET /api/rcm/claims`, `POST /api/rcm/claims/{id}/submit\|resubmit\|link-preauth\|status-check\|cancel`, `GET /api/rcm/claims/{id}/timeline\|adjudication` |
| Prior auth | `GET /api/rcm/preauths`, `POST /api/rcm/preauths/{id}/submit\|update\|extend\|claim\|status-check\|cancel` |
| Batches | `POST /api/rcm/batches`, `GET /api/rcm/batches`, `GET /api/rcm/batches/{batchId}` |
| Messages | `POST /api/rcm/messages/poll` (optional filters: count, include or exclude types, period, `focusClaimId`), `POST /api/rcm/messages/{transactionId}/resend` (identical resend after a timeout), `GET /api/rcm/messages/stuck`, `POST /api/rcm/messages/stuck/scan`, `POST /api/rcm/messages/inbound` (raw NPHIES bundle JSON) |
| Communications | `GET /api/rcm/communication-requests`, `GET /api/rcm/communication-requests/{id}`, `POST /api/rcm/communication-requests/{id}/respond`, `GET /api/rcm/communications`, `POST /api/rcm/communications` |
| Payments | `GET /api/rcm/payments`, `GET /api/rcm/payments/discrepancies`, `GET /api/rcm/payments/{id}`, `POST /api/rcm/payments/{id}/acknowledge`, `POST /api/rcm/payments/details/{detailId}/match` |
| Denials | `GET /api/rcm/denials`, `GET /api/rcm/denials/summary`, `GET /api/rcm/denials/{id}`, `PATCH /api/rcm/denials/{id}` |
| Appeals | `POST /api/rcm/appeals`, `GET /api/rcm/appeals`, `GET /api/rcm/appeals/{id}`, `POST /api/rcm/appeals/{id}/withdraw` |
| Reports | `GET /api/rcm/reports/dashboard`, `GET /api/rcm/reports/aging`, `GET /api/rcm/reports/denial-rate?groupBy=month\|week\|payer\|reason`, `GET /api/rcm/reports/payers` |

Notes on routes outside `/api/rcm`:
- The legacy `api/RCM/*` endpoints and `POST /api/claims/{id}/submit` delegate to these services.
- `GET /api/bulk/batch/{id}` returns the real batch state.

List endpoints accept `providerId`, `insurerId`, `patientId`, `status`, `from`, `to`, `page` and `pageSize`. Workflow errors map to:
- 400 for an invalid request;
- 404 when an entity is not found;
- 409 for a conflict, such as the wrong state, a review gate, or a timeout requiring a status-check.

## Configuration (`Rcm` section)

| Key | Default | Purpose |
|---|---|---|
| `PollWorkerEnabled` | not set | Run `NphiesPollWorker`. When not set it runs everywhere except Development |
| `PollIntervalSeconds` | `300` | Delay between poll cycles in working hours |
| `OffHoursPollIntervalSeconds` | `3600` | Delay outside working hours, capped at one day |
| `WorkingHoursStart`, `WorkingHoursEnd` | `7`, `20` | Working hours in Saudi time (UTC+3) |
| `PollLeaseSeconds` | `900` | How long an instance keeps the poll lease without renewing; only the lease holder polls |
| `StuckAfterDays` | `3` | Flag a claim or prior authorization with no final response after this many days |
| `StuckEscalateAfterDays` | `30` | Escalate it: contact NPHIES support with the bundle ids |
| `StuckScanIntervalMinutes` | `60` | How often the poll worker runs the stuck-request scan |
| `MaxPollsPerCycle` | `10` | Polls per provider per cycle while messages keep arriving |
| `AutoAcknowledgePayments` | `true` | Send payment-notice after ingesting a payment-reconciliation |
| `AppealWindowDays` | `30` | Appeal deadline after a denial |
| `PaymentMatchTolerance` | `0.01` | SAR tolerance for payment matching |
| `IdentifierBaseUrl` | `Nphies:Sender:ProviderBaseUrl` | Base for provider identifier systems |
| `RequireReviewApprovalForClaims` | `true` | Claims (and batches) require approved technical and medical review before sending |

The batch size limit is `Nphies:BatchMaxClaims`, which falls back to `NphiesLimits`.

## Tests

`NPhies_FHIR_Integration.Tests/Nphies/Rcm` runs every workflow through the real services, the messaging core and EF Core. Two things are replaced:
- the NPHIES transport is a fake (`FakeNphiesTransport`) that replies with the docs examples (`nphies-fhir-docs-main`);
- the database is a throw-away SQL Server database.

The fixture coverage is:
- 2b: preauth partial, with denials
- 5b: professional preauth
- 7b: claim
- 9c: status-check
- 12b: poll
- 13a: payment
- 14b and 15c: eligibility
- 17b: cancel
- 18a: communication request

EF InMemory and Sqlite are not available offline, so the tests need a local SQL Server. The tests connect to `Server=localhost;Integrated Security=true;TrustServerCertificate=true`, create `NPhiesRcmTests_<guid>`, and drop it at assembly cleanup. If no server is reachable, the tests report `Inconclusive`.

```powershell
dotnet test NPhies_FHIR_Integration.Tests --filter "FullyQualifiedName~Rcm"
```

## Data explorer API

- `GET /api/data/tables`: the tables the user may read, with columns and child relations.
- `GET /api/data/{table}`: one page of rows. Query parameters:
  - `hospitalId`, plus `from` and `to` (inclusive) applied to `dateField`.
  - `search`, and `f.{column}=value` for equality filters.
  - `sortBy`, `sortDir`, `page`, and `pageSize` (max 500).
- `GET /api/data/{table}/{id}`: the full row plus child tables and their counts (master-detail).
- `GET /api/data/{table}/export`: CSV of the filtered rows, up to 50k.
- `GET /api/lookups/hospitals` and `GET /api/lookups/insurers`: lists for the filter dropdowns.

Users and refresh tokens are not exposed. Secret columns are dropped.

Hospital scoping:
- Tables with a direct `ProviderId`, `RequestorId` or `OrganizationId` column are filtered on it.
- Child tables inherit the scope from their parent.
- Non-admin users who belong to an organization are always restricted to that hospital.

## Frontend (`frontend/`)

The header holds a global **hospital + date range** filter, persisted in local storage. It applies to every page:
- the dashboard, the RCM pages and the data explorer;
- non-admin users are locked to their own hospital.

| Route | Page |
|---|---|
| `/` | Dashboard: KPIs, A/R aging, denial rate by month/week/payer/reason, payer breakdown, and a "Poll NPHIES now" button |
| `/rcm/eligibility` | Eligibility checks, a new check (benefits/validation/discovery), and a response drawer |
| `/rcm/preauths` | Prior authorizations: submit, update, extend, convert to claim, status check, cancel |
| `/rcm/claims` | Claims: submit, resubmit, link pre-auth, status check, cancel/nullify, and batch submit for selected claims |
| `/rcm/batches` | Batches, each expandable to its claims |
| `/rcm/communications` | Insurer CommunicationRequests (respond with attachments; notifications are marked informational) and unsolicited communications, optionally about one service (item sequence) and with a reason code |
| `/rcm/payments` | Payment reconciliations: detail lines, manual matching, payment notice, discrepancies |
| `/rcm/denials` | Denial KPIs and breakdowns, the denial work queue with three actions (appeal in the same claim with a justification per service, resubmit, write off), appeals with method, outcome and recovered amount, and withdraw |
| `/data/:table` | Generic explorer for every table: search, column filters, sort, column chooser, CSV export, and a master-detail drawer with child tabs |

Clicking a claim anywhere opens the **claim timeline** drawer. Its tabs show:
- the header, items, diagnoses, care team and supporting info;
- per-item adjudication;
- NPHIES messages, denials, communications, payments and related claims.

## Sample data (Development)

`SampleDataSeeder` (Infrastructure/Data/Seeders) runs at startup in Development after `DevelopmentClaimSeeder`. It is idempotent: it matches rows by natural key and inserts only what is missing. It seeds:
- 6 payers (`INS-FHIR-0001..0006`) with insurer organizations, 2-3 policies each, benefit coverage and submission rules.
- 3 hospitals (`N-F-10001..10003`), each with locations, clinics, practitioners, doctors and 6 patients with coverages.
- 12 claims/preauths per hospital (`SMP-*`). They cover every claim type, plus: preauth links, resubmission/related, shadow billing, vision prescriptions, oral details, accident and encounters.
- NPHIES code masters: services, ICD-10-AM, medications, modifiers, devices, benefits and error codes.

`SampleDataSeederTests` asserts that every seeded claim maps to a valid NPHIES bundle.
