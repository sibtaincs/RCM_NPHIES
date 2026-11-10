import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { message } from 'antd';
import { claimsApi } from '@/api/endpoints/claims';
import { getApiErrorMessage } from '@/utils/apiError';
import type { ClaimReviewPayload, ClaimReviewStage, ClaimsReviewQueue } from '@/types/claims';

const CLAIMS_KEY = ['claims'];

export function useClaimsReviewQueue(stage: ClaimReviewStage, enabled = true) {
  return useQuery<ClaimsReviewQueue>({
    queryKey: [...CLAIMS_KEY, 'awaiting-review', stage],
    queryFn: () => claimsApi.listAwaitingReview(stage),
    enabled,
    placeholderData: keepPreviousData,
  });
}

export function useClaimReviewStatus(id: number | string | null, enabled = true) {
  return useQuery({
    queryKey: [...CLAIMS_KEY, 'review-status', id],
    queryFn: () => claimsApi.getReviewStatus(id!),
    enabled: enabled && id !== null,
  });
}

export function useClaimReviewMutations() {
  const queryClient = useQueryClient();
  const invalidateClaims = () => queryClient.invalidateQueries({ queryKey: CLAIMS_KEY });
  const onError = (error: unknown) => message.error(getApiErrorMessage(error));

  const review = useMutation({
    mutationFn: ({
      id,
      stage,
      payload,
    }: {
      id: number | string;
      stage: ClaimReviewStage;
      payload: ClaimReviewPayload;
    }) => claimsApi.review(id, stage, payload),
    onSuccess: (_data, variables) => {
      message.success(`${variables.stage === 'technical' ? 'Technical' : 'Medical'} review recorded`);
      void invalidateClaims();
    },
    onError,
  });

  const submit = useMutation({
    mutationFn: (id: number | string) => claimsApi.submit(id),
    onSuccess: () => {
      message.success('Claim submitted');
      void invalidateClaims();
    },
    onError,
  });

  return { review, submit };
}

export function useClaimsReadyForSubmission(enabled = true) {
  return useQuery({
    queryKey: [...CLAIMS_KEY, 'ready-for-submission'],
    queryFn: () => claimsApi.listReadyForSubmission(),
    enabled,
  });
}

export function useClaimWorkflowDetails(id: number | string | null, enabled = true) {
  return useQuery({
    queryKey: [...CLAIMS_KEY, 'workflow-details', id],
    queryFn: () => claimsApi.getWorkflowDetails(id!),
    enabled: enabled && id !== null,
  });
}