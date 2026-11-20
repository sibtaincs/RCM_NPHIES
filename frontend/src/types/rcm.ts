/** Mirrors Application/Rcm/Dtos/RcmDtos.cs (JSON is camelCase). */

export interface RcmPagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface RcmQuery {
  providerId?: string;
  insurerId?: string;
  patientId?: string;
  status?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export interface RcmClaimQuery extends RcmQuery {
  use?: string;
  claimType?: string;
  nphiesStatus?: string;
  search?: string;
}

export interface DenialQuery extends RcmQuery {
  category?: string;
  reasonCode?: string;
  claimId?: string;
}

export interface EligibilityCheckRequest {
  patientId: string;
  coverageId?: string;
  providerId: string;
  insurerId?: string;
  locationId?: string;
  purposes: string[];
  servicedDate?: string;
  servicedPeriodEnd?: string;
  /** Newborn checked against the mother's coverage (coverageId is then the mother's). */
  isNewborn?: boolean;
  /** Check by the receiving provider of a referral (transfer of care). */
  isTransfer?: boolean;
}

export interface EligibilityCheck {
  id: string;
  patientId: string;
  coverageId?: string | null;
  providerId: string;
  insurerId: string;
  purposes: string[];
  isNewborn?: boolean;
  isTransfer?: boolean;
  servicedDate: string;
  requestIdentifier: string;
  messageId?: string | null;
  status: string;
  outcome?: string | null;
  isEligible?: boolean | null;
  inForce?: boolean | null;
  siteEligibility?: string | null;
  disposition?: string | null;
  notInForceReason?: string | null;
  benefitPeriodStart?: string | null;
  benefitPeriodEnd?: string | null;
  responseIdentifier?: string | null;
  benefits?: unknown;
  discoveredCoverages?: unknown;
  errors?: unknown;
  createdAt: string;
  respondedAt?: string | null;
  /** What the site eligibility code means for the visit, in plain language. */
  siteEligibilityReason?: string | null;
  /** The errors, with advice for the payer codes that have any. */
  errorExplanations?: EligibilityError[];
  /** Confirmed on a payer portal or by phone; nothing was sent to NPHIES. */
  isOffline?: boolean;
  offlineReference?: string | null;
  offlineDate?: string | null;
}

export interface EligibilityError {
  code?: string | null;
  message?: string | null;
  explanation?: string | null;
}

export interface OfflineEligibilityRequest {
  patientId: string;
  coverageId?: string;
  providerId: string;
  insurerId?: string;
  reference: string;
  date: string;
  notes?: string;
}

export interface ItemAdjudication {
  sequence: number;
  outcome?: string | null;
  submitted?: number | null;
  eligible?: number | null;
  benefit?: number | null;
  copay?: number | null;
  deductible?: number | null;
  tax?: number | null;
  patientShare?: number | null;
  approvedQuantity?: number | null;
  reasonCode?: string | null;
  reasonDisplay?: string | null;
}

export interface ClaimAdjudication {
  claimResponseId: string;
  claimId: string;
  responseIdentifier?: string | null;
  outcome?: string | null;
  adjudicationOutcome?: string | null;
  disposition?: string | null;
  /** Why the payer reissued this response (reissue-reason), e.g. "adjudication" after a re-adjudication appeal. */
  reissueReason?: string | null;
  preAuthRef?: string | null;
  preAuthPeriodStart?: string | null;
  preAuthPeriodEnd?: string | null;
  totalSubmitted?: number | null;
  totalEligible?: number | null;
  totalBenefit?: number | null;
  totalCopay?: number | null;
  totalTax?: number | null;
  totalPatientShare?: number | null;
  paymentAmount?: number | null;
  paymentDate?: string | null;
  receivedAt?: string | null;
  items: ItemAdjudication[];
}

export interface SubmissionResult {
  claimId: string;
  claimNumber: string;
  use: string;
  operation: string;
  transactionId?: string | null;
  messageId?: string | null;
  transactionStatus: string;
  claimStatus?: string | null;
  outcome?: string | null;
  adjudicationOutcome?: string | null;
  errors: string[];
  adjudication?: ClaimAdjudication | null;
}

export interface DerivedClaimRequest {
  submit?: boolean;
  reason?: string;
  newPeriodEnd?: string;
  /** Prior authorization update or extension: item sequences to leave out (adjudicated authorizations only). */
  removeItemSequences?: number[];
  /** Items only partly done: each keeps the done quantity and the rest becomes a new item. */
  splitItems?: { sequence: number; doneQuantity: number; remainderServicedDate?: string }[];
}

export interface LinkPreAuthRequest {
  preAuthClaimId?: string;
  preAuthRef?: string;
}

export interface CancelClaimRequest {
  reason: string;
  nullify: boolean;
}

export interface TaskResult {
  claimId: string;
  operation: string;
  transactionId?: string | null;
  messageId?: string | null;
  transactionStatus: string;
  taskStatus?: string | null;
  processingStatus?: string | null;
  claimStatus?: string | null;
  errors: string[];
}

export interface RcmClaimSummary {
  id: string;
  claimNumber: string;
  identifier?: string | null;
  use: string;
  claimType: string;
  claimSubType?: string | null;
  patientId: string;
  patientName?: string | null;
  patientNameArabic?: string | null;
  providerId: string;
  insurerId: string;
  insurerName?: string | null;
  status: string;
  nphiesStatus?: string | null;
  outcome?: string | null;
  adjudicationOutcome?: string | null;
  total: number;
  approvedAmount?: number | null;
  approvedTax?: number | null;
  expectedPayment?: number | null;
  patientShareAmount?: number | null;
  paidAmount?: number | null;
  paymentVariance?: number | null;
  preAuthRef?: string | null;
  preAuthPeriodEnd?: string | null;
  isShadowBilling: boolean;
  batchIdentifier?: string | null;
  createdDate: string;
  submittedAt?: string | null;
  adjudicatedAt?: string | null;
  paidAt?: string | null;
  documentCount?: number | null;
}

export interface Transaction {
  id: string;
  direction: string;
  operation: string;
  eventCode: string;
  messageId: string;
  entityType?: string | null;
  entityId?: string | null;
  status: string;
  responseCode?: string | null;
  outcome?: string | null;
  errorSummary?: string | null;
  sentAt: string;
  completedAt?: string | null;
  /** NphiesValidation | PayerBusinessError | Rejection; absent when the exchange succeeded. */
  errorKind?: string | null;
  adjudicationOutcome?: string | null;
  metaTags?: string[];
  nphiesGenerated?: boolean;
  resendCount?: number;
  lastResentAt?: string | null;
}

/** Result of sending the identical message of a transaction again. */
export interface ResendResult {
  transactionId: string;
  operation: string;
  messageId: string;
  bundleId?: string | null;
  resendCount: number;
  transactionStatus: string;
  errorKind?: string | null;
  entityType?: string | null;
  entityId?: string | null;
  claimStatus?: string | null;
  errors: string[];
}

export interface RelatedClaim {
  claimId?: string | null;
  identifier?: string | null;
  relationship?: string | null;
}

export interface ClaimTimeline {
  claim: RcmClaimSummary;
  transactions: Transaction[];
  adjudications: ClaimAdjudication[];
  denials: Denial[];
  communicationRequests: CommunicationRequest[];
  communications: Communication[];
  payments: PaymentDetail[];
  related: RelatedClaim[];
}

export interface BatchSubmitRequest {
  claimIds: string[];
  batchIdentifier?: string;
}

export interface BatchClaim {
  claimId: string;
  claimNumber: string;
  batchNumber?: number | null;
  claimStatus?: string | null;
  outcome?: string | null;
  total: number;
  approvedAmount?: number | null;
}

export interface BatchSubmitResult {
  batchIdentifier: string;
  transactionId?: string | null;
  messageId?: string | null;
  transactionStatus: string;
  claimCount: number;
  submittedAt?: string | null;
  completedAt?: string | null;
  claims: BatchClaim[];
  errors: string[];
}

export interface PollRunResult {
  pollRequests: number;
  messagesReceived: number;
  processed: number;
  duplicates: number;
  unmatched: number;
  paymentNoticesSent: number;
  claimIds: string[];
  paymentReconciliationIds: string[];
  communicationRequestIds: string[];
  errors: string[];
  notes: string[];
}

export interface Attachment {
  contentType: string;
  title?: string;
  /** Base64 content. */
  data?: string;
  url?: string;
  creation?: string;
  /** The claim service (item sequence) the document supports; sent as extension-ClaimItemSequence. */
  itemSequence?: number;
}

/** A text about one claim service (Communication.payload with extension-ClaimItemSequence). */
export interface CommunicationItemNote {
  sequence: number;
  text: string;
}

/** NPHIES communication-reason codes. */
export const COMMUNICATION_REASONS = ['missing-info', 'missing-attach', 'info-correct', 're-adjudication'] as const;

export interface SendCommunicationRequest {
  claimId?: string;
  /** An advanced authorization to write about instead of a claim. */
  advancedAuthorizationId?: string;
  texts: string[];
  /** Texts about single services of the claim. */
  items?: CommunicationItemNote[];
  attachments: Attachment[];
  category?: string;
  priority?: string;
  reasonCode?: string;
}

export interface CommunicationRequest {
  id: string;
  identifier?: string | null;
  status: string;
  category?: string | null;
  priority?: string | null;
  claimId?: string | null;
  claimIdentifier?: string | null;
  payloadText?: string | null;
  payloads?: unknown;
  reason?: string | null;
  /** Claim services the payer's request refers to. */
  itemSequences: number[];
  receivedAt?: string | null;
  respondedCommunicationId?: string | null;
  respondedAt?: string | null;
  /** False for a notification, which only informs. */
  requiresResponse: boolean;
  isOpen: boolean;
}

export interface Communication {
  id: string;
  identifier?: string | null;
  direction: string;
  status: string;
  category?: string | null;
  priority?: string | null;
  claimId?: string | null;
  communicationRequestId?: string | null;
  payloadText?: string | null;
  payloads?: unknown;
  reasonCode?: string | null;
  itemSequences: number[];
  processingStatus: string;
  sentAt?: string | null;
  createdAt: string;
  transactionStatus?: string | null;
  errors: string[];
}

export interface PaymentDetail {
  id: string;
  paymentReconciliationId: string;
  detailType: string;
  requestIdentifier?: string | null;
  responseIdentifier?: string | null;
  date?: string | null;
  amount: number;
  componentPayment?: number | null;
  earlyFee?: number | null;
  nphiesFee?: number | null;
  matchedClaimId?: string | null;
  claimNumber?: string | null;
  expectedAmount?: number | null;
  varianceAmount?: number | null;
  matchStatus: string;
}

export interface PaymentReconciliation {
  id: string;
  paymentReconciliationId: string;
  identifier?: string | null;
  status: string;
  outcome?: string | null;
  disposition?: string | null;
  periodStart: string;
  periodEnd: string;
  paymentDate?: string | null;
  paymentAmount: number;
  paymentCurrency: string;
  paymentIdentifier?: string | null;
  payerId?: string | null;
  payerName?: string | null;
  matchStatus: string;
  expectedAmount?: number | null;
  varianceAmount?: number | null;
  totalNphiesFee: number;
  totalEarlyFee: number;
  acknowledgementStatus?: string | null;
  acknowledgedAt?: string | null;
  acknowledgementError?: string | null;
  receivedAt?: string | null;
  details: PaymentDetail[];
}

export interface Denial {
  id: string;
  claimId: string;
  claimNumber?: string | null;
  claimResponseId?: string | null;
  itemSequence?: number | null;
  itemCode?: string | null;
  reasonCode: string;
  reasonDisplay?: string | null;
  category: string;
  submittedAmount: number;
  deniedAmount: number;
  status: string;
  denialDate: string;
  appealDeadline: string;
  daysToDeadline: number;
  appealRequestId?: string | null;
  resubmissionClaimId?: string | null;
  providerId?: string | null;
  insurerId?: string | null;
  insurerName?: string | null;
  notes?: string | null;
}

export interface Breakdown {
  key: string;
  label?: string | null;
  count: number;
  amount: number;
}

export interface DenialSummary {
  totalDenials: number;
  openDenials: number;
  totalDeniedAmount: number;
  openDeniedAmount: number;
  recoveredAmount: number;
  expiringIn7Days: number;
  byCategory: Breakdown[];
  byReason: Breakdown[];
  byPayer: Breakdown[];
  byStatus: Breakdown[];
}

export interface CreateAppealRequest {
  claimId: string;
  denialIds: string[];
  reason: string;
  notes?: string;
  texts: string[];
  /** Re-adjudication only: a justification per appealed service. */
  items?: CommunicationItemNote[];
  attachments: Attachment[];
  /** true: a new claim related to this one; false: a re-adjudication communication about the same claim. */
  resubmit: boolean;
}

export interface Appeal {
  id: string;
  appealNumber: string;
  claimId: string;
  claimNumber?: string | null;
  resubmissionClaimId?: string | null;
  communicationId?: string | null;
  /** Communication (re-adjudication in the same claim) or Resubmission. */
  method: string;
  status: string;
  outcome?: string | null;
  level: number;
  reasonCodes: string;
  reason: string;
  appealedAmount: number;
  /** The part of the appealed amount the payer no longer denies. */
  recoveredAmount: number;
  approvedAmount?: number | null;
  denialDate: string;
  deadlineDate: string;
  submittedDate?: string | null;
  lastStatusUpdate: string;
  decisionExplanation?: string | null;
  denialIds: string[];
  errors: string[];
}

export interface RcmDashboard {
  from: string;
  to: string;
  claimsSubmitted: number;
  claimsAdjudicated: number;
  claimsPending: number;
  claimsApproved: number;
  claimsPartiallyApproved: number;
  claimsRejected: number;
  claimsErrored: number;
  preAuthsSubmitted: number;
  preAuthsApproved: number;
  billedAmount: number;
  approvedAmount: number;
  patientShareAmount: number;
  paidAmount: number;
  outstandingAmount: number;
  deniedAmount: number;
  denialRate: number;
  cleanClaimRate: number;
  collectionRate: number;
  averageDaysToAdjudication?: number | null;
  averageDaysToPayment?: number | null;
  openDenials: number;
  openCommunicationRequests: number;
  unacknowledgedPayments: number;
  paymentDiscrepancies: number;
  eligibilityChecks: number;
  eligibilityRate: number;
}

export interface AgingBucket {
  bucket: string;
  minDays: number;
  maxDays?: number | null;
  count: number;
  amount: number;
}

export interface DenialRate {
  period: string;
  adjudicated: number;
  denied: number;
  partiallyDenied: number;
  billedAmount: number;
  deniedAmount: number;
  denialRate: number;
  amountDenialRate: number;
}

export interface PayerBreakdown {
  insurerId: string;
  insurerName?: string | null;
  claims: number;
  approved: number;
  rejected: number;
  pending: number;
  billedAmount: number;
  approvedAmount: number;
  paidAmount: number;
  outstandingAmount: number;
  denialRate: number;
  averageDaysToPayment?: number | null;
}

export interface MessageJson {
  transactionId: string;
  direction: string;
  operation: string;
  eventCode: string;
  messageId: string;
  responseEventCode?: string | null;
  httpStatus?: number | null;
  requestJson?: string | null;
  responseJson?: string | null;
  /** request | sync-response | poll-response | '' (nothing logged) */
  source: string;
  error?: string | null;
  loggedAt?: string | null;
}

export interface BundlePreview {
  claimId: string;
  claimNumber: string;
  operation: string;
  json: string;
  warnings: string[];
}

// ---- Insurance discovery (NPHIES Check Insurance API) ----

export interface InsuranceDiscoveryRequest {
  patientId?: string;
  /** National id, Iqama, passport, visa or border number. Defaults to the patient's identity number. */
  patientKey?: string;
  /** 1 nationals and residents, 2 visitors, 3 tourists, 4 premium residency, 5 Hajj and Umrah. */
  systemType: number;
}

export interface DiscoveredPolicy {
  policyNumber?: string | null;
  insuranceCompanyEn?: string | null;
  insuranceCompanyAr?: string | null;
  insuranceCompanyId?: string | null;
  issueDate?: string | null;
  expiryDate?: string | null;
  expired: boolean;
  className?: string | null;
  networkId?: string | null;
  deductibleRate?: number | null;
  maxLimit?: number | null;
  beneficiaryType?: string | null;
  beneficiaryNumber?: string | null;
  name?: string | null;
  insuranceStatus?: string | null;
  matchedInsurerId?: string | null;
  matchedInsurerName?: string | null;
  existingCoverageId?: string | null;
}

export interface InsuranceDiscovery {
  patientId?: string | null;
  patientKey: string;
  systemType: number;
  found: boolean;
  errorCode?: string | null;
  errorMessage?: string | null;
  simulated: boolean;
  policies: DiscoveredPolicy[];
}

export interface AddDiscoveredCoverageRequest {
  patientId: string;
  patientKey?: string;
  systemType: number;
  policyNumber: string;
  insurerId?: string;
  /** For a dependent: child, spouse, parent, common, injured or other. */
  relationship?: string;
  /** For a dependent: the policy holder (for example the mother or father) when on file. */
  subscriberPatientId?: string;
}

export interface DiscoveredCoverage {
  coverageId: string;
  insurerId: string;
  policyNumber: string;
  created: boolean;
}

// ---- Stuck requests ----

export interface StuckRequest {
  id: string;
  claimId: string;
  claimNumber?: string | null;
  providerId?: string | null;
  insurerId?: string | null;
  use?: string | null;
  claimStatus: string;
  waitingSince: string;
  daysWaiting: number;
  transactionId?: string | null;
  bundleId?: string | null;
  messageId?: string | null;
  requestIdentifier?: string | null;
  heldByNphies: boolean;
  /** Flagged | Escalated | Resolved */
  level: string;
  flaggedAt: string;
  escalatedAt?: string | null;
  resolvedAt?: string | null;
  guidance?: string | null;
}

export interface StuckScanResult {
  flagged: number;
  escalated: number;
  resolved: number;
  open: number;
}

// ---- Advanced (payer-initiated) authorizations ----

export interface AdvancedAuthorization {
  id: string;
  identifier?: string | null;
  preAuthRef?: string | null;
  periodStart?: string | null;
  periodEnd?: string | null;
  reissueReason?: string | null;
  reason?: string | null;
  claimType?: string | null;
  subType?: string | null;
  outcome?: string | null;
  adjudicationOutcome?: string | null;
  disposition?: string | null;
  patientId?: string | null;
  patientIdentifier?: string | null;
  patientName?: string | null;
  insurerId?: string | null;
  totalEligible?: number | null;
  totalBenefit?: number | null;
  /** Active | Used | Expired | Cancelled | Superseded */
  status: string;
  usedByClaimId?: string | null;
  receivedAt: string;
  items?: unknown;
}
