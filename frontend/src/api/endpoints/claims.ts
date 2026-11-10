import { apiClient } from '@/api/client';
import type {
  ClaimRecord,
  ClaimReviewPayload,
  ClaimReviewStage,
  ClaimReviewStatus,
  ClaimWorkflowDetails,
  ClaimsReviewQueue,
  ClaimsSubmissionQueue,
} from '@/types/claims';

/** Typed wrappers for the claims review workflow. */
export const claimsApi = {
  listAwaitingReview: async (stage: ClaimReviewStage): Promise<ClaimsReviewQueue> => {
    const { data } = await apiClient.get<ClaimsReviewQueue>(`/claims/awaiting-review/${stage}`);
    return { ...data, claims: data?.claims ?? [], count: data?.count ?? 0 };
  },

  listReadyForSubmission: async (): Promise<ClaimsSubmissionQueue> => {
    const { data } = await apiClient.get<ClaimsSubmissionQueue>('/claims/ready-for-submission');
    return { ...data, claims: data?.claims ?? [], count: data?.count ?? 0 };
  },

  getReviewStatus: async (id: number | string): Promise<ClaimReviewStatus> => {
    const { data } = await apiClient.get<ClaimReviewStatus>(`/claims/${id}/review-status`);
    return data;
  },

  getWorkflowDetails: async (id: number | string): Promise<ClaimWorkflowDetails> => {
    const { data } = await apiClient.get<ClaimWorkflowDetails>(`/claims/${id}/workflow-details`);
    return data;
  },

  review: async (
    id: number | string,
    stage: ClaimReviewStage,
    payload: ClaimReviewPayload,
  ): Promise<ClaimReviewStatus> => {
    const { data } = await apiClient.post<ClaimReviewStatus>(
      `/claims/${id}/${stage}-review`,
      payload,
    );
    return data;
  },

  submit: async (id: number | string): Promise<ClaimRecord> => {
    const { data } = await apiClient.post<ClaimRecord>(`/claims/${id}/submit`);
    return data;
  },
};