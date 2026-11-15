# Development phases: NPHIES-compatible RCM

The plan for designing and building the rest of the RCM system. It has two parts:

- **Part A: what NPHIES requires.** The rules from the reference material in `RCM/`, grouped by use case, so a developer doesn't have to reread 18 PDFs and the IG site.
- **Part B: the phases.** What to build, in what order, and how we know each phase is done.

This plan builds on [PROJECT_STATUS.md](PROJECT_STATUS.md), the honest state of the code, and [NPHIES_GAP_ANALYSIS.md](NPHIES_GAP_ANALYSIS.md), gaps G1 to G16 and questions Q1 to Q4. Where an item here is already a gap there, it carries the same G number.

Prepared November 2026.

---

## Sources

Everything below comes from the `RCM/` folder:

| Path | What it is |
|---|---|
| `RCM/full-ig/site/*.md` | The FHIR IG "Healthcare Financial Services IG Edition 1", `nphies-fs` 1.0.0, built 3 Dec 2025. Start with `usecases.md`, `message-exchange.md`, `message-structure.md`, `error-handling.md` and the `usecase-*.md` pages. |
| `RCM/full-ig/site/*.pdf` | 18 NPHIES guidelines (listed in [Guideline index](#guideline-index)). |
| `RCM/definitions.json/` | 213 StructureDefinitions, 78 CodeSystems and 82 ValueSets as JSON. |
| `RCM/examples.json/` | Example bundles and resources. |
| `RCM/ValidationErrorCodes/` | `nphies-ValidationErrorCodes - 2.6_OBA.xlsx`. |
| `RCM/full-ig/site/package.tgz` | The IG package. **It is byte-identical to the one the app embeds** (`Application/Nphies/Resources/nphies-fs-package.tgz`), so profile validation already runs against this version. |

---

# Part A: what NPHIES requires

## A1. Transport and message rules (all use cases)

**Transport**
- Every message is a FHIR R4 `Bundle` (`type = message`) posted to `$process-message` over **mutual TLS** with an X.509 certificate issued by NPHIES. Renew it before it expires; report and replace it at once if it may be compromised.
- `MessageHeader` must be the **first** entry. The other resources can come in any order.
- Test environments must never carry real patient data.

**Limits**

| Limit | Value |
|---|---|
| Connection "up time" | 60 seconds |
| Message size | 10 MB |
| Claims per batch | 200 |
| Messages per poll | 100 (1 if `count` is not given) |
| Large-claim threshold | more than 300 items (NPHIES may change it) |

**Duplicate detection.** A message is a duplicate if it has the same `MessageHeader.id`, the same focal-resource identifier and the same body.

| Bundle.id | MessageHeader.id | Resource identifier | NPHIES result |
|---|---|---|---|
| new | new | new | OK, new message |
| new | new | existing | Error: duplicate identifier |
| new | existing | new | Error: duplicate MessageHeader.id |
| new | existing | existing | OK, duplicate: returns the latest stored response, or "queued" if there is none |
| existing | any | any | Error, unless MessageHeader.id and identifier also exist (then "duplicate message") |

**Recovering from a timeout or 5xx**
- **Prior authorization, claim, communication, cancel:** resend **exactly the same** message (same Bundle.id, MessageHeader.id and body). NPHIES returns the stored response. Or poll.
- **Eligibility and status-check:** these are real-time only and are never queued, so poll can't recover them. Send a **new** request.
- **Never resubmit a claim with new ids** after a "queued" response. That creates a business duplicate. The payer may then reject it with codes 1677 or 1678, or denial code N-DC-069.
- If no final response arrives within **30 days**, contact NPHIES support with the bundle ids.

**Meta tags NPHIES puts on responses** (`http://nphies.sa/terminology/CodeSystem/meta-tags`)

| Tag | Meaning | What we do |
|---|---|---|
| `queued-messages` | More messages are waiting in our queue. | Trigger a poll (G11). |
| `nphies-generated` | NPHIES answered for the payer, for example because the payer is offline or slow. | Show that the queue is NPHIES's, not the payer's. Wait and poll. |
| `large-claim` | The claim was accepted for later validation. | Mark it Queued. The result, including validation errors, arrives by poll. |

**Who queued it?** This comes from the Claim Submission Guideline. The sender is the payer licence and outcome is `queued`: the payer has the claim. The sender is the NPHIES licence and the tag is `nphies-generated`: NPHIES holds it, because the payer is offline, it was a batch, or it was a large claim.

**Three kinds of "error" a provider can receive** (Error received by HCP guideline). The UI must keep them apart:
1. **NPHIES validation error.** The sender is NPHIES. The codes are `BV-`, `DT-`, `IB-` or `IC-` followed by digits. Fix the message and send a new one.
2. **Payer business error.** `outcome = error` and `ClaimResponse.error.code` comes from `adjudication-error`. The request was **not** adjudicated. Send a new, corrected request with a **new identifier**.
3. **Rejection.** `outcome = complete`, the `adjudication-outcome` is `rejected`, and an adjudication reason is given. This is a denial; it goes to the denials and appeals path.

Unreadable messages and messages with a bad structure come back as an `OperationOutcome` with HTTP 3xx or higher. Errors in the content come back as HTTP 200 with a business response that contains the errors.

## A2. Two status fields on every PA and claim response

These come from the Transaction Statuses Guideline and `nphies Definitions.pdf`.

| Field | Values | Meaning |
|---|---|---|
| `ClaimResponse.outcome` (processing) | `queued`, `partial`, `complete`, `error` | How far the payer has got. `partial` is planned for prior authorizations only. |
| `extension-adjudication-outcome`, on the header and on each item (business) | `approved`, `partial`, `rejected`, `pended`, `not-required` | The decision. `not-required` applies to prior authorizations only. |

Rules NPHIES plans to enforce, which our ingestion should already follow:
- If any item is `pended`, the outcome is `partial`. If none is, it is `complete`. A `queued` outcome means the header is `pended`.
- If every item is approved, the header is approved. If every item is rejected, the header is rejected. Any mix gives `partial`.

The **rejection categories** are AD (administrative), BE (billing error), CV (coverage), MN (medical necessity) and SE (supporting evidence). The G7 fix maps them.

## A3. Use cases

| Use case | Event codes | Pattern | Real time? | Notes |
|---|---|---|---|---|
| Eligibility | `eligibility-request` / `-response` | Full cycle | Real time only | Purposes: `benefit`, `discovery`, `validation`. |
| Prior authorization | `priorauth-request` / `-response` | Full cycle | Real time and deferred | 5 types: institutional, professional, pharmacy, oral, vision. |
| Advanced authorization (APA) | `advanced-authorization` | Payer to NPHIES | Notice; we get it by poll | |
| Claim | `claim-request` / `-response` | Full cycle | Real time and deferred | Same 5 types. |
| Claim batch | `batch-request` / `-response` | Provider to NPHIES | Deferred | At most 200 claims, one payer. |
| Information request | `communication-request` | Payer to NPHIES | Notice; we get it by poll | |
| Information submission | `communication` | Full cycle | Real time and deferred | |
| Cancel / nullify | `cancel-request` / `-response` (and `nullify-*`) | Full cycle | Real time | |
| Status check | `status-check` / `status-response` | Full cycle | Real time only; never queued | |
| Poll | `poll-request` / `poll-response` | Provider to NPHIES | Real time | |
| Payment reconciliation | `payment-reconciliation` | Payer to NPHIES | Notice; we get it by poll | |
| Payment notice | `payment-notice` | Provider to NPHIES | Real time | |
| Prescriber (e-prescription) | `prescriber-request` / `-response` | Full cycle | Real time and deferred | Event codes exist, but the package has **no profiles** yet (G13). |
| Error notice | `error-notice` | NPHIES to payer | | Payer side only; we don't build it. |

### Eligibility
- Request contents: CoverageEligibilityRequest, Coverage, Patient, the provider and insurer Organizations, and optionally the policy holder and a Location.
- **Newborn:** add `extension-newborn` and use the **mother's** coverage. `servicedDate` (or the end of `servicedPeriod`) must be no more than **90 days** after the date of birth. Payer error 1665 means the baby is more than 30 days old and has its own policy.
- **Transfer:** the receiving provider sends `extension-transfer = true`. The payer must then answer `siteEligibility = eligible`, even when the provider is out of network.
- **Site eligibility:** required when the outcome is `complete` (BV-00503). It appears on the header and on each insurance entry.
  - Codes: `eligible`, `not-active`, `not-covered`, `not-direct-billing`, `out-network`, `limit-exhausted`, `coverage-suspended`, `provider-contract-suspended`.
  - The header is `eligible` only if at least one in-force policy is eligible (BV-00535, 00558 to 00560).
- **Payer error codes** worth explaining in the UI: 1658 no member found, 1659 provider not contracted, 1660 wrong TPA, 1665 newborn over 30 days, 1666 provider not licensed, 1680 bad document id format, 1681 bad membership number format, 1682 patient details don't match the member.
- **Offline eligibility** (checked on a portal or by phone): store the reference and the date. Later claims and prior authorizations send them as `eligibility-offline-reference` and `eligibility-offline-date`.

### Insurance discovery (HIDP)
This comes from the Beneficiary Discovery guide (G15). It is **not FHIR**:
- The call is `GET https://hsb.nphies.sa/check-insurance?PatientKey={id}&SystemType={1-5}`. In OBA the host is `http://hsb.oba.nphies.sa`.
- The SystemType values are:
  1. HIDP: nationals and residents, by NIN or Iqama (10 digits).
  2. VIDP: visitors.
  3. SCTH: tourists.
  4. UIDP: premium residency.
  5. HUIDP: Hajj and Umrah, by passport.
- It returns the policy number, the insurer's name and id, the class, the expiry date, the network, the deductible and the maximum limit.
- Error codes are 101 to 112, for example 105 "No record found" and 108 to 112 for authorization header problems.
- A CHI mandate says providers must look up the insurer by patient id this way, **not ask the patient**.

### Prior authorization
- Request contents: one of the 5 authorization profiles, Coverage, Patient, the provider and insurer, the Practitioner, and an Encounter auth profile that matches the encounter class (AMB, EMER, HH, IMP, SS or VR).
- **Eligibility link (required):** use `extension-eligibility-response` for an online check, or the offline reference and date (G3).
- `days-supply` supporting info is required for outpatient medication.
- The response returns `preAuthRef` and `preAuthPeriod`.
- **Changing an approved PA** (follow-up, extension, amendment or resubmission) works like this:
  - Send a **new** PA with `Claim.related.claim` pointing at the **first** PA in the chain (always A, never B), `related.relationship = prior`, and the same `preAuthRef`.
  - Include **all** items: the ones already approved and done, the ones approved and still planned, and the new ones.
  - Don't change the dates of items already done. Items already done must carry their `servicedDate`, and the payer must not reverse their approval.
  - To change an item: if nothing has been done yet, remove it and add a new item. If part of it has been done, reduce its quantity and add a new item for the rest.
  - To remove items: while the PA is pending, cancel it and resubmit with only the items needed. Once it is adjudicated, send a related `prior` PA without those items.
- **Cancel:** send `cancel-request` with `Task.focus` set to the PA identifier. **Don't cancel an expired PA**; once `preAuthPeriod` has passed it is effectively cancelled.
- **Transfer of care (referral):**
  1. Provider A updates its PA to remove the services it can't perform.
  2. Provider A sends a transfer PA with `extension-transfer = true`.
  3. The payer answers with `transferAuthorizationNumber`, `transferAuthorizationPeriod` and `transferAuthorizationProvider` (G9). Use the **approved quantity**, not the outcome.
  4. Provider B checks eligibility with the transfer flag.
  5. Provider B claims with `reason-for-visit = referral`, `insurance.preAuthRef` and `authorizationOffLineDate`.
  6. Provider B extends the authorization with a PA that has `referral.display` and the same preAuthRef.
- **APA (advanced authorization):** we receive it by poll.
  - To change it: send a PA with **all** items and the APA's `preAuthRef`. This overrides the APA.
  - To ask the payer to change it: send a `communication` about the APA.
  - The payer cancels it by reissuing it with the same preAuthRef and `reissue-reason = cancel`.
  - We cancel it with `cancel-request` (G4, G14).

### Claims
- Request contents: one of the 5 claim profiles plus the matching Encounter claim profile (AMB, EMER, HH, IMP, SS or VR).
- **A claim can't be changed after it is sent.** To change its content, cancel it (even if it is adjudicated), then send a new claim.
- **Required on every claim:**
  - `extension-episode` (a Claim-level Identifier). It is the same for every claim in an episode of care: admission, interim, discharge and follow-up.
  - `extension-patientInvoice` on **every item**. NPHIES checks only the structure, not duplicates (G5, done).
  - The eligibility link (online response, or offline reference and date).
  - The PA link: `insurance.preAuthRef`, plus `extension-priorauthresponse` or `authorization-offline-date` (G3).
- **Line amount:** `net = quantity × unitPrice × factor + tax`. `factor` defaults to 1 (0.9 means a 10% discount). Also send `patientShare` and `payerShare`. When `package = true`, `net` doesn't have to equal the sum of the details.
- **Accidents:**
  - A `work-related-diagnosis` code requires `accident.type = WPA`.
  - An `rta-diagnosis` code requires `MVA`.
  - Both rules apply to claims and prior authorizations.
- **Dual coding:** the first coding is the official code from the required ValueSet. The second coding can carry the internal code agreed with the payer.
- **Newborn** (Newborn v1.2):
  - add `extension-newborn`;
  - use a Z38.0 to Z38.8 diagnosis;
  - add `birth-weight` supporting info in kg using UCUM, required on institutional and professional birth encounters;
  - service dates must be no more than 90 days after birth.
- The rules are BV-00506, 00507, 00509 and 00553 to 00556 in Newborn v1.2. Check the exact id for each rule in the error-code sheet.
- **Chief complaint:** use an ICD-10-AM code, free text, or both.
- **Supporting info by discipline** (I = institutional, P = professional, O = oral, Ph = pharmacy, V = vision):
  - **Required (1..1) for I and P:** chief-complaint, history-of-present-illness, patient-history, physical-examination, treatment-plan, vital signs (weight, height, systolic, diastolic), temperature, pulse, respiratory-rate and oxygen-saturation.
  - **1..\* for I and P:** investigation-result.
  - If a vital sign wasn't measured, send the matching `*-absence-reason` code in `.reason` instead of a value.
  - Text for an `investigation-result` of `other` goes in `valueString`, never in `code.text`.
- **Batch:**
  - at most 200 claims, all to the same payer;
  - every claim carries `batch-identifier`, `batch-number` and `batch-period`;
  - NPHIES validates them in real time and queues them for the payer, and we poll for each claim's response.
- **Large claim** (more than 300 items): we get a queued response with the `large-claim` tag at once, and the result arrives by poll.
- **Resubmitting a rejected or partly approved claim:** send a new claim with `related.claim` set to the original identifier and `relationship = prior`. Include **all** the services, including the ones already approved.
- **Adding services after a claim:** send a **new** claim with the **same episode id**. That is not a resubmission.

### Communications (information request and submission)
- The payer's `CommunicationRequest.about` names our Claim identifier.
- Our `Communication.about` must name the CommunicationRequest when we are answering one, or the Claim when we send information unprompted.
- On a PA chain, refer to the **latest** PA.
- **Appeals inside the same claim** (Claim Re-submission Guide):
  - send a `communication` with `reasonCode = re-adjudication`, the item sequence and the justification or attachment;
  - the payer then reissues the ClaimResponse (`reissue-reason = adjudication` or `correction`), or sends a CommunicationRequest with `category = notification` explaining why not.
  - NPHIES returns an error if the claim was cancelled, had errors, or doesn't exist.
- **Item-level link:** use `payload.extension-ClaimItemSequence` (`valuePositiveInt`), from the CommunicationRequest-to-ClaimItem guide V1.1.
  - **The extension isn't in the IG package**, and the PDF spells the URL `ClaimItemSequence` in one place and `claimItemSequence` in another.
  - This is still open as Q1.

### Cancel and nullify
- Use `Task.code = cancel` with `Task.focus.identifier` set to the business identifier.
- To nullify, add `Task.input type = nullify` with value `true`. The record is then kept for audit only and never displayed.
- The answer is `Task.status = completed`, or `error` with `Task.output type = error`.
- You can't cancel a request that was already answered with errors.

### Status check and poll
- **Status check:** `Task.code = status` with `focus.identifier`. It is never queued, so send it again if it times out.
- **Poll:** `Task.code = poll`. The filters are:
  - `count`, up to 100;
  - `include-message-type` or `exclude-message-type` (not both);
  - `period`;
  - `focus.identifier`, which returns the response to one specific request.
- The IG expects providers to poll **at least daily, automatically**, and to allow a poll on demand for urgent prior authorizations.

### Payment reconciliation and payment notice
- **PaymentReconciliation** (Payment Reconciliation and Payment Notice guideline V2.0):
  - `paymentIssuer`, `requestor`, `paymentDate`, `paymentIdentifier` and `paymentAmount`;
  - one `detail` per claim, with `request` and `response` references, an `identifier` and an `amount`.
  - `detail.amount` = `component-payment` (benefit plus tax) + `component-early-fee` + `component-nphies-fee`.
  - `paymentAmount` is the sum of all the `detail.amount` values.
  - **Detail types:**
    - `advance`: no claim references;
    - `payment`;
    - `adjustment`: the advance already covered it.
  - **Reversals:** delta amounts, often negative, with `detail.predecessor` set to the previous `detail.identifier`.
- **PaymentNotice:** send it **only after the money is in the bank**. It goes to NPHIES, not the payer, and carries the paid amount and the date.

### Prescriber and e-prescription (EBP)
EBP Pharmacy Technical Summary V3, G13. This is future scope.
- `prescriber-request` with `Claim.use = predetermination`.
- Items use **scientific (generic) drug codes**, with a `diagnosisSequence` on every item.
- A `MedicationRequest` carries the dosage (`doseAndRate`, and `timing` with count, frequency, period and duration).
- Trade (GTIN) codes need an `absenceOfScientificCode` reason: `unresponsive`, `adverse-effect`, `irreplaceable`, `allergy` or `others`.
- The response adds the adjudication categories `approved-duration` and `approved-refill`.
- An appeal is a related request with `relationship = appealing`.
- The pharmacy's later PA or claim puts the payer's reference in `Claim.prescription`. Each item carries:
  - `pharmacist-selection-reason` (required);
  - `prescribed-medication`;
  - `pharmacist-substitute`.
- **The IG package has no prescriber profiles yet.** Ask NPHIES for them before building.

---

# Part B: development phases

The phase numbers follow the roadmap in [PROJECT_STATUS.md](PROJECT_STATUS.md). Phases 1 to 3 of that roadmap are split below into feature tracks, B1 to B8, which can run partly in parallel once Phase 1 is done.

**Ground rules for every phase**
- Build only on `Application/Nphies` + `Application/Rcm`. Don't extend `Application/Services/*`.
- Every outbound message goes through `INphiesBundleBuilder`, then `INphiesBundleValidator`, then `RcmMessageDispatcher` (stored as Pending before it is sent). Every inbound message goes through `RcmResponseIngestor`.
- Each feature ships with:
  - a docs-example or golden test in `NPhies_FHIR_Integration.Tests`;
  - a workflow test through `RcmTestHost`;
  - an update to the matching sandbox scenario (`Nphies:Sandbox`).
- Each feature that maps to an onboarding test case updates the table in NPHIES_GAP_ANALYSIS.md.

## Phase 0: baseline. **Done.**
CI builds and tests, the repository is cleaned up, and the status is honest. See PROJECT_STATUS.md.

## Phase 1: one stack
**Goal.** `Rcm/` + `Nphies/` is the only path, with permissions and hospital scoping on every endpoint.

- List each legacy controller under `ApiService/Controllers/`. For each one, decide whether to **port** it to `api/rcm/*` or **retire** it. Delete the legacy service and its `LegacyTests` together.
- Retire the folders no API code uses: Analytics, MachineLearning, Reporting, Infrastructure, Search, Tenants and Enterprise.
- Apply `[HasPermission]` and `Scope()` to every remaining controller, and add endpoint-authorization tests.

**Done when** no controller outside `api/rcm/*`, master data or user/role management remains, and the authorization tests cover every route.

## Phase 2: messaging core hardening (track B1)
**Goal.** The transport behaves exactly as A1 describes, under timeouts, duplicates and queueing.

| Item | Gap | Work |
|---|---|---|
| Exact resend | G8 | Add a "resend identical message" action that replays the stored Bundle from `NphiesMessageLogs` with the same ids. Allow it only for PA, claim, communication and cancel. |
| Eligibility and status-check timeouts | G8 | Let them be sent again as new requests. Block poll-based recovery for them. |
| Meta tags | G11, new | Parse `queued-messages` (trigger a poll), `nphies-generated` (record that NPHIES is holding it) and `large-claim` (Queued, wait for the poll). |
| Error source | new | Classify each response as an NPHIES validation error, a payer business error or a rejection (A1). Store it, and show it in the UI. |
| Poll filters | G4 (part) | Expose `count`, include/exclude types, `period` and `focus` on `POST api/rcm/messages/poll` and `RcmPollService`. |
| Poll worker | G16 | Make it safe to run on several instances (a lease or lock in the database). Enable it by default in non-dev environments; it runs at least daily and more often during working hours. |
| Limits | new | Check the 10 MB message size and the 200 claims per batch before sending. Keep the HTTP timeout within the 60-second connection limit. |
| Stuck requests | new | A job flags PAs and claims still Queued after N days, and escalates them at 30 days with the bundle ids for NPHIES support. |
| Status mapping | new | Store both `outcome` and `adjudication-outcome`, at header and item level, including `pended` and `not-required` (A2). |

**Done when** sandbox tests cover a timeout followed by an exact resend, a duplicate returning the stored response, the large-claim tag, and a queued response with an NPHIES sender.

**Status: implemented** on `phase-2/messaging-core`. The sandbox tests are in `NphiesSandboxTransportRulesTests`; the lease, stuck-request scan and limits are in `RcmWorkerLeaseAndStuckRequestTests`. The stuck-request list has a screen (`/rcm/stuck`). Still open: waking the lease holder when another instance receives a `queued-messages` tag.

## Phase 3: front office: discovery and eligibility (track B2)
| Item | Gap | Work |
|---|---|---|
| Insurance discovery | G15 | A REST client for `check-insurance`, with SystemType 1 to 5 and environment URLs. It pre-fills the patient's coverage and payer. Map error codes 101 to 112. |
| Eligibility purposes | | Support `benefit`, `discovery` and `validation`, alone or together. |
| Site eligibility | | Show `siteEligibility` per insurance with a plain-language reason. Warn before a PA or claim for a patient who is not eligible. |
| Newborn and transfer | G1, G2 done | Add a check for the 90-day window. |
| Offline eligibility | | A screen to record a reference and date obtained on a portal or by phone. Link them to later PAs and claims. |
| Eligibility error codes | | Explanations for 1658 to 1682 in the UI. |

**Done when** all Eligibility V4.0 test cases pass in the sandbox.

**Status: partly implemented** on `phase-3/eligibility` (tests in `RcmEligibilityFrontOfficeTests`). Done: the three purposes, the site-eligibility reason, the claim-health warning `ELIGIBILITY_NOT_ELIGIBLE`, the 90-day newborn check, offline eligibility records (with a "Use latest" button in the claim editor), and advice for payer error codes. Insurance discovery is also done (`Application/Nphies/Discovery`, tests in `RcmInsuranceDiscoveryTests`): the guide lists the response fields but not the JSON envelope or the authorization header format, so the parser accepts an object, an array or a wrapped array, and the header value is configurable (`Nphies:Discovery:AuthorizationHeader`); confirm both against the OBA environment. The five Eligibility V4.0 onboarding test cases pass against the sandbox (`EligibilityOnboardingCasesTests`); they have not been run against the real OBA environment.

## Phase 4: prior authorization lifecycle (track B3)
| Item | Gap | Work |
|---|---|---|
| Eligibility and PA response links | G3 | Pick a stored eligibility response, and send `extension-eligibility-response` and `extension-priorauthresponse`. |
| Follow-up and extension | new | An "amend PA" action that clones **all** items, keeps the dates of items already done, sets `related = prior` to the **first** PA in the chain, and keeps the `preAuthRef`. Partial-quantity split as in A3. |
| Remove items | new | Pending: cancel and resubmit. Adjudicated: a related `prior` PA without the items. |
| Cancel rules | | Block cancelling a PA whose `preAuthPeriod` has expired. |
| Transfer of care | G9 | Read and store `transferAuthorizationNumber`, `Period` and `Provider`. Use the approved quantity. Support the provider B claim and extension (`reason-for-visit = referral`, `referral.display`, `authorizationOffLineDate`). |
| APA lifecycle | G4, G14 | Handle a reissue with the same preAuthRef: `reissue-reason = cancel` cancels it, and other reasons replace it. Add a provider cancel API, an "amend from APA" action (a PA with all items and the APA's preAuthRef), and a communication about an APA. |
| PA expiry | | Track `preAuthPeriod`. Warn when a claim falls outside it. |

**Done when** all Authorization V6.0 test cases pass, including #2 (G3) and #10 (APA with a filtered poll).

## Phase 5: claims (track B4)
| Item | Gap | Work |
|---|---|---|
| Required extensions | G5 done | Episode reuse: the same episode for interim, discharge and follow-up claims, chosen from the patient's open episodes. |
| References | G3 | Eligibility and PA links, as in Phase 4. |
| Claim editor rules | G10 + new | Before sending, check the A3 rules: the required supporting info for I and P (or an absence reason), the accident type for WPA/MVA diagnoses, the newborn rules (Z38.x, birth-weight in kg, 90 days), the `net` formula, packages, days-supply for outpatient drugs, dual coding, and the missing categories `hospitalized`, `morphology`, `admission-weight` and `estimated-Length-of-Stay`. |
| Immutability | | Once a claim is sent its content is read-only. "Correct" means cancel, then clone into a new claim with a **new identifier**. |
| Resubmission | | "Resubmit" means a new claim with `related.claim` set to the original, `relationship = prior`, and **all** items. "Add services" means a new claim with the same episode. |
| Batches | | Group by payer, at most 200 claims, with `batch-identifier`, `batch-number` and `batch-period`. Match each deferred ClaimResponse back to its claim by the batch extensions. |
| Large claims | | Warn the user when a claim has more than 300 items; the result arrives by poll. |
| Duplicate guard | | Before sending, warn when another claim has the same patient, service codes and dates. Show payer codes 1677 and 1678 and N-DC-069 as duplicates. |

**Done when** all Claims V4.0 test cases pass, and every seeded claim passes `Enforce` validation.

## Phase 6: denials, communications and appeals (track B5)
| Item | Gap | Work |
|---|---|---|
| Information request worklist | | Incoming `communication-request`s go to a queue per hospital, with an SLA timer, linked to the PA or claim (and the item, through ClaimItemSequence). |
| Replies | | A `communication` whose `about` is the CommunicationRequest when replying, or the Claim when sending unprompted. On a PA chain, use the latest PA. Attachments go through claim documents. |
| Re-adjudication appeal | new | An "appeal items" action sends a `communication` with `reasonCode = re-adjudication`, the item sequences and the justification. Handle the reissued ClaimResponse (`reissue-reason`) or a `notification` CommunicationRequest. |
| Item sequence | Q1 | Settle the extension URL with NPHIES before shipping. |
| Denial management | G7 done | A work queue by AD/BE/CV/MN/SE category. For each denial, choose one of: appeal in the same claim (communication), resubmit (`related = prior`), or write off. |

**Done when** the communication test cases pass (Authorization #1, Claims #2), and an appeal round trip works in the sandbox.

## Phase 7: payments and reconciliation (track B6)
| Item | Work |
|---|---|
| Reconciliation ingest | Handle `advance`, `payment` and `adjustment` detail types, the three components, and `predecessor` chains for reversals and recoupments. Check that `paymentAmount` equals the sum of the details. |
| Posting | Post each detail to the claim's receivable. Allocate advances and net them against later payments. Flag claims approved but not paid after the payer's cycle. |
| Bank matching | Record bank receipts and match them by `paymentIdentifier` and amount. Only then send the **PaymentNotice**. Make the automatic notice depend on bank confirmation. |
| Reports | Aged receivables by payer, denial rate, payment turnaround, and NPHIES fees. |

**Done when** all Payment V3.0 test cases pass.

## Phase 8: pharmacy and e-prescription (track B7, optional)
Build only if the product is to support EBP. Prerequisite: NPHIES publishes the prescriber profiles.

The scope is a prescriber-request builder and workflow (scientific codes, MedicationRequest dosage, absence reasons, appealing), the pharmacy extensions, and `Claim.prescription` (G13). See A3.

## Phase 9: security and compliance (roadmap phase 2)
- Identity hardening:
  - higher PBKDF2 iterations;
  - hashed refresh tokens;
  - the JWT moved out of `localStorage`;
  - no default admin password;
  - a distributed rate limiter.
- Field encryption for national ids and Iqama numbers.
- An audit trail of who viewed or sent each message.
- Secrets in a vault.
- A map against the PDPL, NCA controls and data residency rules.
- A **separate client certificate and provider licence for each hospital**. A certificate must never be logged or exported.

## Phase 10: NPHIES certification (roadmap phase 3)
- Set `Nphies:ProfileValidation:Mode=Enforce` (G6).
- Ask NPHIES Q1 to Q4, and **confirm current document versions**. The PDFs here range from 2021 to 2025, and the error-code sheet is 2.6 OBA.
- Run all 25 onboarding test cases against `INS-FHIR` in OBA, and export the evidence (`POST api/rcm/conformance/evidence`).
- Run the HIDP check-insurance API against OBA.

## Phase 11 onwards: HIS integration, operations, frontend, pilot (roadmap phases 4 to 7)
These are unchanged from PROJECT_STATUS.md:
- **HIS integration:**
  - a versioned inbound API with idempotency;
  - HL7 v2;
  - code mapping to SBS, ICD-10-AM and SFDA;
  - an outbox and webhooks.
- **Operations:**
  - containers;
  - a migration bundle;
  - Redis;
  - object storage for documents;
  - observability;
  - backups;
  - load and penetration tests.
- **Frontend:** Arabic and right-to-left layout, the screens for the tracks above, and end-to-end tests.
- **Pilot:** sandbox, then production certification, a parallel run, then go-live.

---

## Order and dependencies

```
Phase 1 (one stack)
   └─► Phase 2 (messaging core) ─┬─► Phase 3 (eligibility) ─► Phase 4 (prior auth) ─► Phase 5 (claims) ─┬─► Phase 6 (denials)
                                 │                                                                    └─► Phase 7 (payments)
                                 └─► Phase 9 (security), in parallel
Phases 3–7 ─► Phase 10 (certification) ─► Phase 11+ (HIS, ops, frontend, pilot)
Phase 8 (EBP) only after NPHIES publishes the prescriber profiles.
```

Ask NPHIES the questions (Q1 to Q4, document versions, prescriber profiles) **now**, not in Phase 10. Their answers change the designs in Phases 4, 6 and 8.

## Guideline index

| PDF in `RCM/full-ig/site/` | Used in |
|---|---|
| Beneficiary Discovery Insurance – Integration Guide V1.0 | A3 discovery, Phase 3 |
| Chief Complaint Process | A3 claims |
| Claim Re-submission Guide | A3 communications, Phase 6 |
| Claim Submission Guideline V2.0 | A1, Phases 2 and 5 |
| Claims re-submission with supporting information guidance | A3 claims and communications, Phases 5 and 6 |
| Communication/CommunicationRequest referring to ClaimItem V1.1 | A3 communications, Q1 |
| EBP Pharmacy Technical Summary V3 | A3 prescriber, Phase 8 |
| Episode Identifier Guide v1.2 | A3 claims |
| Error received by HCP – Guideline | A1 error kinds, Phase 2 |
| Follow up, extension, amending and resubmitting an adjudicated Pre-Authorization v2.2 | A3 prior authorization, Phase 4 |
| Invoice Number for item Level Guide v1.2 | A3 claims |
| Newborn v1.2 | A3 eligibility and claims |
| nphies Definitions | A2 |
| Patient Transfer of Care v1.2 | A3 prior authorization, Phase 4 |
| Payment Reconciliation and Payment Notice Guideline V2.0 | A3 payments, Phase 7 |
| Requesting Additional Information Guideline v1.1 | A3 communications, Phase 6 |
| Site Eligibility v1.2 | A3 eligibility, Phase 3 |
| Transaction Statuses Guideline v1.0 | A2, Phase 2 |
