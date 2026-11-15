import { Button, Popconfirm, message } from 'antd';
import { RedoOutlined } from '@ant-design/icons';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { rcmApi } from '@/api/endpoints/rcm';
import type { Transaction } from '@/types/rcm';
import { getApiErrorMessage } from '@/utils/apiError';

/** Use cases NPHIES can queue: resending the identical message returns its stored response. */
const RESENDABLE = ['priorauth', 'claim', 'communication', 'cancel', 'nullify'];

/** True when a transaction lost its answer (timeout, or a transport failure with no reply) and can be resent identically. */
function canResend(tx: Transaction) {
  if (tx.direction !== 'outbound' || !RESENDABLE.includes(tx.operation)) return false;
  return tx.status === 'Timeout' || (tx.status === 'Error' && !tx.responseCode);
}

/**
 * "Resend identical message": sends the same Bundle.id and MessageHeader.id again, which NPHIES answers with its stored
 * response. Never creates a duplicate, unlike submitting again.
 */
export function ResendButton({ tx, claimId }: { tx: Transaction; claimId: string }) {
  const queryClient = useQueryClient();
  const resend = useMutation({
    mutationFn: () => rcmApi.messages.resend(tx.id),
    onSuccess: (r) => {
      if (r.transactionStatus === 'Timeout') void message.warning('NPHIES did not answer again. Try later, or run a status-check.');
      else void message.success(`Resent (attempt ${r.resendCount}): ${r.transactionStatus}${r.claimStatus ? ` · claim ${r.claimStatus}` : ''}`);
      void queryClient.invalidateQueries({ queryKey: ['claim-timeline', claimId] });
    },
    onError: (e) => void message.error(getApiErrorMessage(e)),
  });

  if (!canResend(tx)) return null;
  return (
    <Popconfirm
      title="Resend the identical message?"
      description="NPHIES will return the response it already holds, or 'queued'. The ids and content stay the same."
      onConfirm={() => resend.mutate()}
    >
      <Button size="small" type="link" icon={<RedoOutlined />} loading={resend.isPending}>
        Resend identical message
      </Button>
    </Popconfirm>
  );
}
