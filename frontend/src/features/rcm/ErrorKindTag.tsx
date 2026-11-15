import { Tag, Tooltip } from 'antd';

/**
 * The three kinds of failure NPHIES distinguishes (Error received by HCP guideline). Each one is recovered
 * differently, so the label and the hint say what to do next.
 */
const KINDS: Record<string, { label: string; color: string; hint: string }> = {
  NphiesValidation: {
    label: 'NPHIES validation error',
    color: 'volcano',
    hint: 'NPHIES rejected the message itself. Fix the data and send a new message.',
  },
  PayerBusinessError: {
    label: 'Payer error',
    color: 'orange',
    hint: 'The payer did not adjudicate the request. Correct it and send a new request with a new identifier.',
  },
  Rejection: {
    label: 'Rejected by payer',
    color: 'red',
    hint: 'The payer adjudicated and rejected it. Handle it as a denial: appeal, resubmit or write off.',
  },
};

/** Tags for a transaction's error kind and NPHIES meta-tags (nphies-generated, large-claim, queued-messages). */
export function ErrorKindTag({
  errorKind,
  nphiesGenerated,
  metaTags,
}: {
  errorKind?: string | null;
  nphiesGenerated?: boolean;
  metaTags?: string[];
}) {
  const kind = errorKind ? KINDS[errorKind] : undefined;
  return (
    <>
      {kind && (
        <Tooltip title={kind.hint}>
          <Tag color={kind.color}>{kind.label}</Tag>
        </Tooltip>
      )}
      {nphiesGenerated && (
        <Tooltip title="NPHIES answered on the payer's behalf (payer offline or slow, batch or large claim). The final answer arrives by poll.">
          <Tag color="purple">NPHIES-generated</Tag>
        </Tooltip>
      )}
      {metaTags?.includes('large-claim') && (
        <Tooltip title="Accepted for deferred validation. The result, including any validation errors, arrives by poll.">
          <Tag color="geekblue">Large claim</Tag>
        </Tooltip>
      )}
    </>
  );
}
