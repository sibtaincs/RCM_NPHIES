import { Select, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { documentsApi } from '@/api/endpoints/documents';
import { fmtDate } from '@/utils/display';

interface Props {
  /** The claim whose patient's documents (its encounter or billable period) are offered. */
  claimId?: string | null;
  value?: string[];
  onChange?: (ids: string[]) => void;
}

const size = (bytes: number) => (bytes >= 1024 * 1024 ? `${(bytes / 1024 / 1024).toFixed(1)} MB` : `${Math.max(1, Math.round(bytes / 1024))} KB`);

/** Picks stored claim documents to send as communication attachments (sent from the document store, not re-uploaded). */
export function ClaimDocumentPicker({ claimId, value = [], onChange }: Props) {
  const candidates = useQuery({
    queryKey: ['rcm', 'claim-document-candidates', claimId],
    queryFn: () => documentsApi.claim.candidates(claimId!),
    enabled: !!claimId,
  });

  if (!claimId) return <Typography.Text type="secondary">Choose the claim first.</Typography.Text>;
  return (
    <Select
      mode="multiple"
      allowClear
      value={value}
      onChange={onChange}
      loading={candidates.isLoading}
      placeholder={candidates.data?.length === 0 ? "No stored documents for this claim's patient" : 'Stored documents'}
      optionFilterProp="label"
      options={(candidates.data ?? []).map((d) => ({
        value: d.id,
        label: `${d.title || d.fileName} · ${d.documentType} · ${fmtDate(d.serviceDate ?? d.invoiceDate)} · ${size(d.sizeBytes)}`,
        disabled: d.sizeBytes > 5 * 1024 * 1024,
      }))}
    />
  );
}
