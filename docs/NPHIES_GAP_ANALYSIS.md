# NPHIES gap analysis

Compares what NPHIES requires with what the code does today. Prepared November 2026 by reading the reference material in `nphies-fhir-docs-main/` and the embedded IG package, then tracing each requirement through the code.

## Sources and versions

| Source | Version / date |
|---|---|
| Implementation Guide (PDF) | V2.6, created May 2024, 141 pages |
| Embedded IG package (`Nphies/Resources/nphies-fs-package.tgz`) | `nphies-fs` 1.0.0, "Healthcare Financial Services IG Edition 1", built 3 Dec 2025 |
| Test cases (against pseudo-payer `INS-FHIR`) | Eligibility V4.0, Authorization V6.0, Claims V4.0, Payment V3.0 |
| CodeableConcept spreadsheet | V5.3 |

These are the copies in the repository. Newer versions may exist on the NPHIES portal, which needs a provider login. **Confirm the current versions before certification.** The documents already disagree in places; see [Questions for NPHIES](#questions-for-nphies).

"Supported" below means the code path exists and matches the requirement on reading. None of this has been run against the NPHIES sandbox yet.

## Summary

Of the 25 certification test cases:
- **14 look supported** by the code;
- **2 are partly supported**;
- **9 are blocked** by missing features.

Four gaps cause all 9 blocks:

1. **No newborn support.** `extension-newborn` is not implemented anywhere.
2. **No transfer flag on eligibility requests.**
3. **No references from a claim to an online eligibility response or a prior-auth response.**
4. **Advanced authorization (APA) cancellation is not applied.**

Two smaller issues would also cause NPHIES rejections or wrong reports:
- the mandatory `episode` and `patientInvoice` extensions are only sent when someone fills them in;
- `BE` and `SE` rejection codes are put in the wrong denial categories.

## Certification test cases

✅ supported · ⚠️ partly supported · ❌ blocked

### Eligibility (V4.0)

| # | Test case | Status | Gap |
|---|---|---|---|
| 1 | Validation with any member ID | ✅ | |
| 2 | Discovery + Benefits without a coverage, with the **transfer** extension | ❌ | G2 |
| 3 | Validation + Benefits for a **newborn** (newborn extension; mother with Iqama, newborn with MRN) | ❌ | G1 |
| 4 | Not active (`siteEligibility = not-active`, inforce false) | ✅ | |
| 5 | Error 1658, no member found | ✅ | |

### Authorization (V6.0)

| # | Test case | Status | Gap |
|---|---|---|---|
| 1 | Institutional, deferred; vitals, chief complaint (text and ICD), encounter, package item; unsolicited communication | ⚠️ | Q1: item-sequence link in the communication payload |
| 2 | Professional with an **online eligibility response reference**; LOINC lab tests; solicited communication with attachment | ❌ | G3 |
| 3 | Dental, offline eligibility, `fdi-oral-region`, cancel | ✅ | |
| 4 | Optical with VisionPrescription | ✅ | |
| 5 | Pharmacy: days-supply linked by `informationSequence`; unlisted drug with SFDA shadow billing | ✅ | |
| 6 | Resubmission of a rejected authorization (`related = prior`) | ✅ | |
| 7 | Follow-up institutional authorization (`related = prior`, one service added) | ✅ | |
| 8 | **Newborn** authorization (birth-weight, Z38 diagnosis) | ❌ | G1 |
| 9 | Referral (**transfer**) authorization | ⚠️ | G9: the transfer details in the response are not read |
| 10 | Advanced authorization: poll filtered to APAs, show the APA, answer its communication request, payer cancels it | ❌ | G4 |

### Claims (V4.0)

| # | Test case | Status | Gap |
|---|---|---|---|
| 1 | Pharmacy claim with a **prior-auth response reference**; solicited communication | ❌ | G3 |
| 2 | Professional claim with an **online eligibility response reference**; vitals, onset, LOINC; unsolicited communication with item sequence | ❌ | G3, Q1 |
| 3 | Institutional, offline eligibility; vitals, attachment, icu-hours, encounter, package with details; status check | ✅ | |
| 4 | Dental, offline authorization, cancel or nullify | ✅ | |
| 5 | Claim without references, error response | ✅ | |
| 6 | **Newborn** claim (mother and newborn patients, birth-weight, Z38) | ❌ | G1 |
| 7 | Batch of at least 2 claims (optional) | ✅ | |
| 8 | Optical claim with **eligibility and authorization response references** | ❌ | G3 |
| 9 | Inpatient claim with 300 items (optional) | ✅ | Not tested; no item limit in the code, and the 10 MB message limit leaves room |

### Payment (V3.0)

| # | Test case | Status |
|---|---|---|
| 1 | Approved claim, poll the payment reconciliation, send a payment notice | ✅ |

## Gaps

### Priority 1: blocks certification

**G1. Newborn extension (`extension-newborn`) is not implemented.**
- The IG (section 6.5) requires the newborn extension on eligibility, prior authorization and claims for a newborn using the mother's coverage. Prior authorizations and claims also need `birth-weight` in supportingInfo, on the birth encounter.
- The IG package defines `StructureDefinition-extension-newborn`, but the code has no constant, builder input, mapper field or claim-editor field for it.
- Separate subscriber and patient resources are already supported (`Subscriber` on the inputs), and so are MRN identifiers and the `birth-weight` category.
- Blocks Eligibility #3, Authorization #8 and Claims #6.

**G2. Transfer extension on eligibility requests.**
- `Claim.extension.transfer` is supported (`NphiesBundleBuilder`, through `Claim.IsTransfer`). `CoverageEligibilityRequest.extension.transfer` is not: `EligibilityRequestInput` has no field for it.
- The IG (section 7.3, step 3) requires it when the receiving provider of a referral checks eligibility.
- Blocks Eligibility #2.

**G3. Claim references to earlier responses.**
- The IG claim profiles define `Claim.extension:eligibilityResponse` (`extension-eligibility-response`) and `Claim.extension:priorauthResponse` (`extension-priorauthresponse`). The builder supports only the offline references (`eligibilityOffLine`, `eligibilityOffLineDate`, `authorizationOffLineDate`).
- Add both references to the builder inputs and the mapper. Let the claim editor pick a stored eligibility response, and set the prior-auth response automatically when a claim is created from, or linked to, a prior authorization.
- Blocks Authorization #2 and Claims #1, #2 and #8.

**G4. Advanced authorization (APA) lifecycle.**
- The IG (section 7.9.2) says the payer cancels an APA by sending another response with the **same preAuthRef** and `reissue-reason = cancel`. `RcmResponseIngestor.IngestAdvancedAuthorizationAsync` does neither of the right things:
  - with the same response id or identifier, it returns the existing record **unchanged**;
  - otherwise it inserts a new record that is `Active` unless `ClaimResponse.status` is `cancelled`.
  - The reissue reason is never read, and the earlier APA is never marked as overridden.
- The provider can also cancel an APA with a `cancel-request` (Task focus = APA identifier). There is no API for that; `api/rcm/advanced-authorizations` only has list, get and `use`.
- The poll service only sends unfiltered polls. Test case #10 asks for a poll that includes only `advanced-authorization`. The builder already supports the filters (`PollRequestInput.IncludeMessageTypes`, `ExcludeMessageTypes`, `Period`, `Count`), but `RcmPollService` and `POST api/rcm/messages/poll` don't expose them.
- Blocks Authorization #10.

**G5. Mandatory `episode` and `patientInvoice`.** *Fixed:*
- When empty, `ep_<ClaimNumber>` and `inv_<ClaimNumber>` are sent and stored on the claim, and clones keep the original episode.
- The claim editor shows an Info notice before sending.
- Tests: `RcmClaimDefaultsTests`.

The original finding:
- The IG profiles make `Claim.extension:episode` 1..1 and `Claim.item.extension:patientInvoice` 1..1 for every claim type; see also IG section 7.4.2.
- `RcmInputMapper` only adds them when `EpisodeIdentifierValue` and `PatientInvoiceValue` are filled in. The claim editor's health rules don't require them.
- With the default `ProfileValidation:Mode=Warn`, such a claim is still sent, and NPHIES rejects it.
- Fix: generate defaults (for example the encounter or account number for the episode, and the HIS invoice number for each item), and add an editor rule.

**G6. Profile validation mode.** Certification runs should use `Nphies:ProfileValidation:Mode=Enforce`, so conformance errors block sending instead of being sent and rejected. This is a configuration change; the validator already supports it.

### Priority 2: wrong behavior or data

**G7. Denial categories.** *Fixed:*
- BE codes now map to `BillingError` and SE codes to `SupportingEvidence`.
- The migration `FixDenialCategoriesForNphiesRejectionCodes` corrects stored denials.
- Tests: `DenialCategoryTests`.

The original finding:
- The IG (section 6.3.10.2) defines five rejection categories: Administrative (AD), Coverage (CV), Medical Necessity (MN), Supporting Evidence (SE) and Billing Errors (BE).
- `RcmResponseIngestor.DenialCategory` maps `BE` to "Benefit" and `SE` to "Coding". Denial reports, analytics and appeal work queues therefore group these denials under the wrong headings.
- One-line fixes, plus a data fix for existing `ClaimDenials` rows.

**G8. Recovering after a timeout.**
- The IG (section 6.4) describes how to recover when the connection times out:
  - send **exactly the same** transaction again (same Bundle.id, MessageHeader.id, identifier and body); NPHIES's duplicate detection (section 6.7) returns the stored response;
  - or poll, but **not** for eligibility or status checks, whose responses are only real time;
  - for those two, send a new request.
- The code refuses any resend after a timeout and points to status-check or poll. That's safe for claims and prior authorizations, but:
  - a timed-out eligibility check is left with no recovery path;
  - the exact resend that the IG recommends isn't offered, even though the raw request is already stored in `NphiesMessageLogs`.
- Add a "resend identical message" action that replays the stored request body, and let eligibility and status checks simply be sent again.

**G9. Transfer (referral) response details.** For a referral prior authorization, the payer's response carries `transferAuthorizationProvider`, `transferAuthorizationPeriod` and `transferAuthorizationNumber` (IG section 7.3). The constants exist, but the parser and ingestor never read them, so the second provider's name, the period and the authorization number are lost.

**G10. Missing supporting-info categories.**
- CodeableConcept V5.3 lists 30 `claim-information-category` codes. These 4 are not supported anywhere in the backend or frontend: `hospitalized`, `morphology`, `admission-weight` and `estimated-Length-of-Stay`.
- Add them to the claim editor, with the value types and code systems the IG requires (section 6.3.11).

**G11. Queued-messages tag.**
- NPHIES can tag any response's MessageHeader with `meta.tag = queued-messages` (system `http://nphies.sa/terminology/CodeSystem/meta-tags`) when messages are waiting (IG section 6.3.7). The constant exists, but nothing reads the tag.
- Reading it and triggering a poll would get deferred responses sooner with fewer empty polls.

**G12. MRN identifier system.** `NphiesBundleBuilder` uses the fixed system `http://provider.com/mrn` for medical record numbers. It should be derived from the provider's own base URL, like the other provider identifier systems (`Rcm:IdentifierBaseUrl`).

### Priority 3: features in the IG that are not implemented

**G13. Prescription verification (predetermination), IG section 7.11.**
- The `prescriber-request` and `prescriber-response` event codes exist as constants, but there is no builder, workflow or screen.
- This use case covers prescriptions with dosage and refills, the payer's advisory response with approved refills, appeals (`related = appealing`), cancellation, communication and status checks.
- Not part of the current test cases. Needed if the product is to support e-prescription verification.

**G14. APA follow-up actions** (IG section 7.9.1):
- send a communication about an APA to ask for a modification or extension;
- send a new prior authorization that keeps the APA's preAuthRef, to modify or add items.

Not verified in the code; check during the G4 work.

**G15. Insurance discovery (HIDP API)** (IG section 6.6). Lists the insurers holding a policy for a patient. It's a separate API with its own guideline, which isn't in the repository, and nothing implements it.

**G16. Queue polling in production.** The IG (section 6.3.6) expects providers to poll the queue daily and periodically, with manual polling for urgent prior authorizations.
- The poll worker exists, but it is off by default (`Rcm:PollWorkerEnabled=false`).
- It is not safe to run on more than one API instance.

Already listed in `docs/PROJECT_STATUS.md`; it matters for certification because deferred responses are only received through polling.

## Questions for NPHIES

**Q1. Item-sequence link in Communication.payload.** Authorization #1 and Claims #2 require `Communication.payload.extension.ClaimItemSequence`. The embedded IG package (December 2025) defines no such extension on the Communication profile: payload slices are only string, attachment and reference. The communication examples in the repository don't use it either. Ask which extension URL to use, or whether the requirement has been dropped.

**Q2. Current versions.** The PDF guide (May 2024), the IG package (December 2025) and the test cases (2023) are different releases. Confirm the versions to certify against, and get any newer test-case documents.

**Q3. Event code naming.** The PDF's event table (section 6.2.7) says `authorization-request`/`authorization-response`, while the `ksa-message-events` CodeSystem and the examples use `priorauth-request`/`priorauth-response`. The code follows the CodeSystem; confirm that's correct.

**Q4. Known IG conflicts**, already noted in `docs/nphies-messaging-core.md`:
- BV-00036, about packages with details;
- BV-00221, about the batch focus count.

## What already matches the IG

These were checked and need no work for certification:
- **Messages:** all `ksa-message-events` codes, and a builder for every message type the provider sends except `prescriber-request`.
- **Duplicates:** the claim identifier system and value are unique per claim.
- **Polling:** count up to 100.
- **Errors:** OperationOutcome responses with HTTP 3xx and 4xx status codes are parsed. Errors are enriched from the validation error catalog (3,638 rules, 1,682 adjudication codes).
- **Adjudication:** both the amount and the value fields are read, for example `approved-quantity` as a value. Reissued adjudications replace denials nobody has acted on yet.
- **Claim content:** offline eligibility and authorization references, packages with details, the days-supply link through `informationSequence`, SFDA shadow billing, the oral region, VisionPrescription, `related = prior` resubmissions, and cancel and nullify.
- **Payments:** payment reconciliation with recoupments, and an automatic payment notice.

## Suggested order of work

1. **G5 and G7.** Small changes that remove certain rejections and wrong reports. *Done.*
2. **G1, G2 and G3:** newborn, transfer on eligibility, and the response references. Each one touches the builder inputs, the mapper, the claim editor and a docs-example test.
3. **G4:** the APA lifecycle (reissue and cancel, provider cancel, filtered poll).
4. **G8 and G9:** timeout recovery and the referral response details.
5. Ask NPHIES Q1–Q4 alongside, and set `Enforce` validation (G6) before the sandbox runs.
6. Run all 25 test cases against `INS-FHIR` in the NPHIES sandbox, and export the evidence with `POST api/rcm/conformance/evidence`.
7. **G10–G16**, in product priority order.
