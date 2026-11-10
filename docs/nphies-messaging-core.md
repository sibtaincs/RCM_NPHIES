# NPHIES messaging core

This layer builds, validates, sends, audits and parses NPHIES FHIR R4 message bundles. Workflow services (eligibility, prior auth, claims, polling, and so on) should use only the interfaces below, never raw HTTP.

All code is under `NPhies_FHIR_Integration.Application/Nphies` and `NPhies_FHIR_Integration.Infrastructure/Nphies`.

## Registration

```csharp
builder.Services.AddNphiesMessagingCore(builder.Configuration); // already called by AddNphiesIntegration
```

This registers:

| Service | Lifetime | Purpose |
|---|---|---|
| `INphiesBundleBuilder` | singleton | Builds message bundles that match the docs examples. |
| `INphiesBundleValidator` | singleton | Rule-based checks (IG + `ValidationErrorCodes` xlsx) run before sending. |
| `INphiesResponseParser` | singleton | Turns any NPHIES response or OperationOutcome into an `NphiesResponse`. |
| `INphiesErrorCatalog` | singleton | 3,638 NPHIES rule ids and 1,682 adjudication error codes, embedded from the ValidationErrorCodes xlsx. |
| `INphiesMessagingClient` | scoped | Validates, posts to `$process-message` over mTLS, audits, parses. |
| `INphiesTransport` | typed `HttpClient` | Mutual-TLS handler, 60 s timeout, Polly retry for idempotent events only. |
| `INphiesMessageAuditStore` | scoped | `EfNphiesMessageAuditStore` writes the `NphiesMessageLogs` table (raw request and response). |
| `NphiesCertificateHealthCheck` | health check | Reports the client certificate's presence and expiry. |

## Typical flow

```csharp
var bundle = builder.BuildClaimRequest(new ClaimRequestInput
{
    Type = NphiesClaimType.Professional,
    SubType = NphiesClaimSubType.Op,
    Provider = new OrganizationInput { License = "PR-FHIR", Name = "..." },
    Insurer  = new OrganizationInput { License = "INS-FHIR", Name = "..." },
    Patient = ..., Coverage = ..., Items = { ... }, SupportingInfo = { ... },
});

var result = await messagingClient.SendAsync(bundle, new NphiesSendOptions { CorrelationId = claimId });

if (result.Response.IsSuccess)
{
    var adjudication = result.Response.Adjudication; // outcome, preAuthRef, totals, per-item adjudication
}
else
{
    foreach (var error in result.Response.Errors) { /* error.Code, error.Message, error.Expression, error.Source */ }
}
```

* `MessageHeader.id` is the correlation key. Responses return it in `response.identifier`, exposed as `NphiesResponse.RequestMessageId`. Store `result.MessageId` and `result.BundleId` with the business entity.
* Leave `MessageId`, `BundleId` and `Timestamp` on any input unset to generate them. Set them explicitly when a resend must keep the same identity.
* `NphiesTimeoutException` means the request **may have been delivered**. Use `BuildStatusCheck` or `BuildPollRequest` to recover; never resend a claim blindly.
* `NphiesValidationException` means local validation failed and nothing was sent. `ValidationErrors` holds the rule ids.

## Builder methods (`INphiesBundleBuilder`)

| Method | Event | Docs examples |
|---|---|---|
| `BuildEligibilityRequest` | `eligibility-request` | 14a, 15a, `Eligibility Request_*.json` |
| `BuildPriorAuthRequest` | `priorauth-request` | 1, 2a, 2c, 4, 5a, 6 (vision prescription), `Preauth Professional Outpatient Request.json` |
| `BuildClaimRequest` | `claim-request` | 7a, 8, 9, 10, 11, 19 (shadow billing) |
| `BuildBatchRequest` | `batch-request` | 16 (batch-identifier, batch-number and batch-period are assigned automatically; one insurer per batch) |
| `BuildStatusCheck` / `BuildCancelRequest` | `status-check` / `cancel-request` | 9b, 17a |
| `BuildPollRequest` | `poll-request` | 12a (include/exclude message types, period, count ≤ 100) |
| `BuildCommunication` | `communication` | 18b (solicited, `BasedOnCommunicationRequestId`), 18c (unsolicited), attachments |
| `BuildPaymentNotice` | `payment-notice` | 13b: acknowledges a payment-reconciliation |

Claim type and subType, the supportingInfo categories (including vital signs, chief complaint, attachments, days-supply and so on), and all of the following come from the input models in `Nphies/Models/NphiesBuilderInputs.cs`:

* episode, account-period, transfer, eligibility/authorization offline, batch, and other claim extensions;
* item tax, patient-share, package, maternity and other item extensions;
* care team, diagnosis, encounter, and vision prescription.

Profile, extension, code-system and identifier URLs are typed constants in `Nphies/Constants/NphiesConstants.cs`: `NphiesProfiles`, `NphiesExtensions`, `NphiesCodeSystems`, `NphiesIdentifierSystems`, `NphiesMessageEvents` and `NphiesLimits`.

## Parsed responses (`NphiesResponse`)

| Incoming event | Populated property |
|---|---|
| `eligibility-response` | `Eligibility` (inforce, benefits, site-eligibility, errors) |
| `priorauth-response`, `claim-response` | `Adjudication` (outcome, preAuthRef, totals, items, adjudication reasons, notes) |
| `status-response`, `cancel-response` | `Task` (status, business status, output references) |
| `poll-response` | `Task` plus `InnerMessages`, with each polled message parsed recursively |
| `communication-request` | `CommunicationRequest` (about claim, payload text and attachments) |
| `payment-reconciliation` | `PaymentReconciliation` (payment identifier, amount, details) |
| `OperationOutcome` / errors | `Errors`, enriched from the catalog (`NphiesErrorSource.Transport/Validation/...`) |

## Configuration (`Nphies` section)

```json
"Nphies": {
  "Environment": "Sandbox",                    // or "Production"
  "SandboxBaseUrl": "https://hsb.nphies.sa/",
  "ProductionBaseUrl": "https://hsb.nphies.sa/",
  "ProcessMessagePath": "$process-message",
  "Timeout": 60, "RetryCount": 3, "RetryDelaySeconds": 2,
  "EnableDetailedLogging": false,              // payloads are never logged; this only adds headers and timings
  "EnableMessageAudit": true,
  "AuditStripStoredAttachments": true,          // replace stored-document base64 in the audit with nphies-doc:{id}:{sha256}
  "Sender": { "ProviderLicense": "", "ProviderBaseUrl": "http://provider.com", "SourceEndpoint": null },
  "Certificate": { "Source": "None|Store|Pfx", "Thumbprint": null, "StoreName": "My",
                   "StoreLocation": "CurrentUser", "PfxPath": null, "PfxPassword": null, "PfxBase64": null }
}
```

Keep secrets out of `appsettings.json`. Use user-secrets locally and environment variables or a vault in deployed environments:

```powershell
dotnet user-secrets --project NPhies_FHIR_Integration.ApiService set "Nphies:Certificate:Source" "Pfx"
dotnet user-secrets --project NPhies_FHIR_Integration.ApiService set "Nphies:Certificate:PfxPath" "C:\certs\provider.pfx"
dotnet user-secrets --project NPhies_FHIR_Integration.ApiService set "Nphies:Certificate:PfxPassword" "<password>"
dotnet user-secrets --project NPhies_FHIR_Integration.ApiService set "JwtSettings:Secret" "<at least 32 chars>"
dotnet user-secrets --project NPhies_FHIR_Integration.ApiService set "ConnectionStrings:DefaultConnection" "<sql connection>"
# environment variable form: Nphies__Certificate__PfxPassword, JwtSettings__Secret, ConnectionStrings__DefaultConnection
```

In Development, a missing JWT key or connection string falls back to an ephemeral key and LocalDB. In every other environment, startup fails until both are set (`SecretConfiguration.EnsureSecrets`).

## Validation

`INphiesBundleValidator` is a `CompositeNphiesBundleValidator` with two layers:

**1. Business rules (`NphiesBundleValidator`).** Rule ids reuse the xlsx codes where one exists (`BV-*`, `IC-*`, `GE-*`); the rest use `NPHIES-*` ids. It checks:

* the bundle and MessageHeader shape;
* fullUrl and reference resolution;
* the profiles per resource;
* claim type and subType consistency;
* item sequences and net totals;
* supportingInfo references;
* batch, poll and size limits.

All 23 outgoing docs examples pass it.

**2. IG profile conformance (`NphiesProfileValidator`).** The NPHIES IG package (`nphies-fs` 1.0.0, from `https://portal.nphies.sa/ig/package.tgz`) is embedded in the Application assembly (`Nphies/Resources/nphies-fs-package.tgz`) and loaded once by `NphiesConformancePackage`. The Firely validator package can't be restored behind the firewall, so the validator is implemented on the Firely SDK's `ITypedElement`/FhirPath. Each resource is validated against its `meta.profile` snapshot:

| Rule id | Check |
|---|---|
| `PRF-CARD` | min/max cardinality |
| `PRF-TYPE` | allowed types of choice elements |
| `PRF-FIXED` | fixed[x] / pattern[x] values |
| `PRF-SLICE` | slice cardinality and closed slicing (value/pattern, profile and type discriminators) |
| `PRF-CODE` | required bindings, when the value set can be enumerated from the package |
| `PRF-EXT` | NPHIES extensions against their packaged definitions |
| `PRF-PROFILE` | `meta.profile` not in the package (warning) |
| `BV-*` | NPHIES FHIRPath invariants from the profiles (core FHIR invariants are skipped) |

Configuration `Nphies:ProfileValidation`:

* `Mode`: `Off` (rules only), `Warn` (default; profile findings are reported as warnings and never block a send), `Enforce` (profile errors block sends like rule errors).
* `PackagePath`: optional path to a newer package `.tgz`; the embedded one is used when empty.

Findings are surfaced in:

* `GET api/rcm/claims/{id}/preview-bundle` (`warnings` and structured `issues`);
* `GET api/rcm/claims/{id}/health` (`conformance=true` by default; bundle findings have `field` = `bundle:<FHIR path>`);
* `POST api/rcm/conformance/validate` (raw bundle JSON body) and `GET api/rcm/conformance/package`.

The current IG is stricter than the docs examples. WS1.4 fixed the builder, mapper and seed data so generated bundles conform:

* **Fixed in the builder and mapper:**
  * per-code Task profiles (`status-request`, `cancel-request`, `poll-request`);
  * insurer `identifier.type`, the Organization `provider-type`, and `Coverage.policyHolder`;
  * Patient `occupation` (fallback `unknown`) and `maritalStatus` (fallback `UNK` using NullFlavor);
  * passport `identifier-country` extension (`Patient.NationalIdCountry`, ISO alpha-3; BV-00481);
  * an encounter is synthesised when one is missing;
  * timing on vital signs and other timed supportingInfo;
  * onset with a code; diagnosis `conditionOnset` (`ClaimDiagnosis.ConditionOnsetCode`);
  * item maternity extension for pharmacy.
* **Claim editor health rules** (Errors block submit):
  * required clinical info per claim type: chief complaint, HPI, patient history, physical examination, treatment plan, investigation result and vitals for professional/institutional claims; chief complaint for oral claims;
  * timing, onset code, and an attachment on IRA investigation results (BV-00846);
  * iqama must start with 2 and national ID with 1; a passport needs a country;
  * occupation must be a valid code; one sub-site per item;
  * a package item needs details.
* **Regression test:** `SampleDataSeederTests.Every_seeded_claim_conforms_to_the_nphies_ig_profiles` builds every seeded claim and requires zero profile errors in strict mode.
* **Known remaining items:**
  * **BV-00036 IG defect.** The published FHIRPath rejects packages that have details, which contradicts its own text and docs example 19. The validator overrides it with `CorrectedInvariants`.
  * **BV-00221.** The batch focus count conflicts with the docs batch example, and the current IG has no batch header profile. Confirm with NPHIES during certification.
  * **Docs examples.** Example 16 still uses `practitioner-role`, and the example iqama values (`0000000003`) fail BV-00798. These are test-data issues only.
  * **Existing databases.** They keep the old sample claims, because the seeder is idempotent by claim number. Reset or reseed them to get conformant sample data.

## Live-test readiness

* **Connectivity pre-flight.** `GET api/rcm/conformance/connectivity` reports Pass/Warn/Fail for configuration (https endpoint, provider license, simulator off, profile mode), the mTLS client certificate (loaded, private key, validity, days to expiry), DNS and a TLS handshake that offers the client certificate. `POST api/rcm/conformance/connectivity/poll` also sends a real poll-request; any messages returned are ingested normally.
* **Recommended live-test settings:** `Nphies:Sandbox:Enabled=false`, `Nphies:ProfileValidation:Mode=Enforce`, `Nphies:EnableMessageAudit=true`.
* **Test-case evidence.** `GET api/rcm/claims/{id}/evidence` (one claim or prior-auth) and `POST api/rcm/conformance/evidence` (`{ "claimIds": [...] }`, max 50) return a zip with every request/response bundle from the message audit, `timeline.json` and a `summary.md` per claim.
* **Advanced authorization.** Poll messages with event `advanced-authorization` are stored in `AdvancedAuthorizations` (reason, preAuthRef and period, service provider, diagnoses, addItem lines). `GET api/rcm/advanced-authorizations[/{id}]`; `POST api/rcm/advanced-authorizations/{id}/use` (`{ "claimId" }`) adds the preAuthRef to an editable claim for the same patient and insurer.
* **Recoupment / refund.** PaymentReconciliation details of type `adjustment` (or with a `predecessor`, or a negative amount) are classified `Recoupment` (negative) or `Adjustment`; `advance` lines are `Advance`. They match the claim through the request identifier or the predecessor line. The claim's paid amount is net of recoupments, and a fully recouped claim becomes `Recouped` (resubmit or appeal). `PaymentReconciliationDto.TotalRecouped` reports the total.

## Tests

`NPhies_FHIR_Integration.Tests/Nphies` (MSTest) contains:

* validation of every docs request;
* parsing of every docs response;
* golden round-trips: each docs request is reverse-mapped to builder input, rebuilt, and compared element by element;
* messaging-client tests with a fake transport.

Run them with `dotnet test NPhies_FHIR_Integration.Tests`.
