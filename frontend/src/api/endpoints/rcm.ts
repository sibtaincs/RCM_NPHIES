import { apiClient } from '@/api/client';
import type {
  AgingBucket,
  Appeal,
  BatchSubmitRequest,
  BatchSubmitResult,
  BundlePreview,
  CancelClaimRequest,
  ClaimAdjudication,
  ClaimTimeline,
  Communication,
  CommunicationRequest,
  CreateAppealRequest,
  Denial,
  DenialQuery,
  DenialRate,
  DenialSummary,
  DerivedClaimRequest,
  EligibilityCheck,
  EligibilityCheckRequest,
  LinkPreAuthRequest,
  MessageJson,
  PayerBreakdown,
  PaymentDetail,
  PaymentReconciliation,
  PollRunResult,
  RcmClaimQuery,
  RcmClaimSummary,
  RcmDashboard,
  RcmPagedResult,
  RcmQuery,
  SendCommunicationRequest,
  SubmissionResult,
  TaskResult,
} from '@/types/rcm';
import type {
  ClaimAuditEntry,
  ClaimCorrectionOptions,
  ClaimEdit,
  ClaimHealth,
  ClaimVersionCompare,
} from '@/types/claimEditor';

const get = <T>(url: string, params?: object) =>
  apiClient.get<T>(url, { params }).then((r) => r.data);
const post = <T>(url: string, body?: unknown, params?: object) =>
  apiClient.post<T>(url, body ?? {}, { params }).then((r) => r.data);

export const rcmApi = {
  eligibility: {
    list: (q: RcmQuery) => get<RcmPagedResult<EligibilityCheck>>('/rcm/eligibility', q),
    get: (id: string) => get<EligibilityCheck>(`/rcm/eligibility/${id}`),
    check: (body: EligibilityCheckRequest) => post<EligibilityCheck>('/rcm/eligibility', body),
  },

  claims: {
    list: (q: RcmClaimQuery) => get<RcmPagedResult<RcmClaimSummary>>('/rcm/claims', q),
    getForEdit: (id: string) => get<ClaimEdit>(`/rcm/claims/${id}/edit`),
    createDraft: (body: ClaimEdit) => post<ClaimEdit>('/rcm/claims/draft', body),
    saveDraft: (id: string, body: ClaimEdit) =>
      apiClient.put<ClaimEdit>(`/rcm/claims/${id}`, body).then((r) => r.data),
    health: (id: string) => get<ClaimHealth>(`/rcm/claims/${id}/health`),
    healthReport: (q: RcmClaimQuery & { problemsOnly?: boolean }) => get<ClaimHealth[]>('/rcm/claims/health', q),
    corrections: (id: string) => get<ClaimCorrectionOptions>(`/rcm/claims/${id}/corrections`),
    audit: (id: string) => get<ClaimAuditEntry[]>(`/rcm/claims/${id}/audit`),
    compare: (id: string) => get<ClaimVersionCompare>(`/rcm/claims/${id}/compare`),
    submit: (id: string) => post<SubmissionResult>(`/rcm/claims/${id}/submit`),
    resubmit: (id: string, body: DerivedClaimRequest) =>
      post<SubmissionResult>(`/rcm/claims/${id}/resubmit`, body),
    linkPreAuth: (id: string, body: LinkPreAuthRequest) =>
      post<RcmClaimSummary>(`/rcm/claims/${id}/link-preauth`, body),
    statusCheck: (id: string) => post<TaskResult>(`/rcm/claims/${id}/status-check`),
    cancel: (id: string, body: CancelClaimRequest) =>
      post<TaskResult>(`/rcm/claims/${id}/cancel`, body),
    timeline: (id: string) => get<ClaimTimeline>(`/rcm/claims/${id}/timeline`),
    adjudication: (id: string) => get<ClaimAdjudication>(`/rcm/claims/${id}/adjudication`),
    previewBundle: (id: string) => get<BundlePreview>(`/rcm/claims/${id}/preview-bundle`),
    transactionJson: (transactionId: string) =>
      get<MessageJson>(`/rcm/claims/transactions/${transactionId}/json`),
  },

  preauths: {
    list: (q: RcmClaimQuery) => get<RcmPagedResult<RcmClaimSummary>>('/rcm/preauths', q),
    submit: (id: string) => post<SubmissionResult>(`/rcm/preauths/${id}/submit`),
    update: (id: string, body: DerivedClaimRequest) =>
      post<SubmissionResult>(`/rcm/preauths/${id}/update`, body),
    extend: (id: string, body: DerivedClaimRequest) =>
      post<SubmissionResult>(`/rcm/preauths/${id}/extend`, body),
    toClaim: (id: string, body: DerivedClaimRequest) =>
      post<SubmissionResult>(`/rcm/preauths/${id}/claim`, body),
    statusCheck: (id: string) => post<TaskResult>(`/rcm/preauths/${id}/status-check`),
    cancel: (id: string, body: CancelClaimRequest) =>
      post<TaskResult>(`/rcm/preauths/${id}/cancel`, body),
  },

  batches: {
    list: (q: RcmQuery) => get<BatchSubmitResult[]>('/rcm/batches', q),
    get: (batchId: string) => get<BatchSubmitResult>(`/rcm/batches/${encodeURIComponent(batchId)}`),
    submit: (body: BatchSubmitRequest) => post<BatchSubmitResult>('/rcm/batches', body),
  },

  messages: {
    poll: (providerId?: string) =>
      post<PollRunResult>('/rcm/messages/poll', undefined, { providerId }),
  },

  communications: {
    requests: (q: RcmQuery & { openOnly?: boolean }) =>
      get<RcmPagedResult<CommunicationRequest>>('/rcm/communication-requests', q),
    respond: (id: string, body: SendCommunicationRequest) =>
      post<Communication>(`/rcm/communication-requests/${id}/respond`, body),
    list: (q: RcmQuery & { claimId?: string }) =>
      get<RcmPagedResult<Communication>>('/rcm/communications', q),
    send: (body: SendCommunicationRequest) => post<Communication>('/rcm/communications', body),
  },

  payments: {
    list: (q: RcmQuery) => get<RcmPagedResult<PaymentReconciliation>>('/rcm/payments', q),
    get: (id: string) => get<PaymentReconciliation>(`/rcm/payments/${id}`),
    discrepancies: (q: RcmQuery) => get<PaymentDetail[]>('/rcm/payments/discrepancies', q),
    acknowledge: (id: string) => post<PaymentReconciliation>(`/rcm/payments/${id}/acknowledge`),
    match: (detailId: string, claimId: string) =>
      post<PaymentReconciliation>(`/rcm/payments/details/${detailId}/match`, { claimId }),
  },

  denials: {
    list: (q: DenialQuery) => get<RcmPagedResult<Denial>>('/rcm/denials', q),
    summary: (q: RcmQuery) => get<DenialSummary>('/rcm/denials/summary', q),
    update: (id: string, body: { status: string; notes?: string }) =>
      apiClient.patch<Denial>(`/rcm/denials/${id}`, body).then((r) => r.data),
  },

  appeals: {
    list: (q: RcmQuery) => get<RcmPagedResult<Appeal>>('/rcm/appeals', q),
    create: (body: CreateAppealRequest) => post<Appeal>('/rcm/appeals', body),
    withdraw: (id: string, reason?: string) =>
      post<Appeal>(`/rcm/appeals/${id}/withdraw`, { reason }),
  },

  reports: {
    dashboard: (q: RcmQuery) => get<RcmDashboard>('/rcm/reports/dashboard', q),
    aging: (q: RcmQuery) => get<AgingBucket[]>('/rcm/reports/aging', q),
    denialRate: (q: RcmQuery, groupBy: string) =>
      get<DenialRate[]>('/rcm/reports/denial-rate', { ...q, groupBy }),
    payers: (q: RcmQuery) => get<PayerBreakdown[]>('/rcm/reports/payers', q),
  },
};
