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
}

export interface SendCommunicationRequest {
  claimId?: string;
  texts: string[];
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
  receivedAt?: string | null;
  respondedCommunicationId?: string | null;
  respondedAt?: string | null;
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
  attachments: Attachment[];
  resubmit: boolean;
}

export interface Appeal {
  id: string;
  appealNumber: string;
  claimId: string;
  claimNumber?: string | null;
  resubmissionClaimId?: string | null;
  communicationId?: string | null;
  status: string;
  outcome?: string | null;
  level: number;
  reasonCodes: string;
  reason: string;
  appealedAmount: number;
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
