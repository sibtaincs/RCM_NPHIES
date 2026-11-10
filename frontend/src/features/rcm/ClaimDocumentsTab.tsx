import { useState } from 'react';
import { Alert, Button, Card, Empty, Popconfirm, Progress, Space, Table, Tag, Tooltip, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { DisconnectOutlined, EyeOutlined, LinkOutlined, UploadOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { documentsApi } from '@/api/endpoints/documents';
import { PERMISSIONS, usePermission } from '@/hooks/usePermission';
import type { ClaimDocument, ClaimSizeBudget } from '@/types/documents';
import { fmtDate } from '@/utils/display';
import { DocumentUploadModal } from './DocumentUploadModal';
import { fmtBytes } from './documentUtils';
import { showError } from './shared';

const EDITABLE = ['Draft', 'Error', 'ValidationFailed'];

function SizeBudgetBar({ budget }: { budget?: ClaimSizeBudget }) {
  if (!budget) return null;
  const pct = Math.min(100, Math.round((budget.estimatedMessageBytes / budget.limitBytes) * 100));
  const warnPct = Math.round((budget.warnBytes / budget.limitBytes) * 100);
  const status = budget.status === 'over-limit' ? 'exception' : budget.status === 'warning' ? 'active' : 'success';
  const color = budget.status === 'over-limit' ? '#ff4d4f' : budget.status === 'warning' ? '#faad14' : '#52c41a';
  return (
    <Card size="small" style={{ marginBottom: 12 }}>
      <Space direction="vertical" style={{ width: '100%' }} size={4}>
        <Space style={{ justifyContent: 'space-between', width: '100%' }}>
          <Typography.Text strong>NPHIES message size</Typography.Text>
          <Typography.Text type="secondary">
            {budget.attachmentCount} attachment(s) · {fmtBytes(budget.estimatedMessageBytes)} of {fmtBytes(budget.limitBytes)} · {fmtBytes(Math.max(0, budget.remainingBytes))} left
          </Typography.Text>
        </Space>
        <Progress percent={pct} status={status} strokeColor={color} success={{ percent: 0 }} format={() => `${pct}%`} />
        {budget.status === 'warning' && (
          <Alert type="warning" showIcon message={`Above the ${warnPct}% warning threshold. Attach only what the payer needs.`} />
        )}
        {budget.status === 'over-limit' && (
          <Alert
            type="error"
            showIcon
            message="The claim message would exceed the NPHIES 10 MB limit and will be rejected."
            description="Detach documents, upload a compressed/scanned-at-lower-DPI copy, or send the rest later as an unsolicited communication."
          />
        )}
      </Space>
    </Card>
  );
}

/** Claim drawer tab: attached documents, the 10 MB size budget and candidate documents of the same patient/encounter. */
export function ClaimDocumentsTab({ claimId, nphiesStatus }: { claimId: string; nphiesStatus?: string | null }) {
  const { can } = usePermission();
  const editable = EDITABLE.includes(nphiesStatus ?? 'Draft') && can(PERMISSIONS.ClaimsUpdate);
  const qc = useQueryClient();
  const [uploading, setUploading] = useState(false);

  const attached = useQuery({ queryKey: ['rcm', 'claim-documents', claimId], queryFn: () => documentsApi.claim.list(claimId) });
  const budget = useQuery({ queryKey: ['rcm', 'claim-size-budget', claimId], queryFn: () => documentsApi.claim.sizeBudget(claimId) });
  const candidates = useQuery({
    queryKey: ['rcm', 'claim-document-candidates', claimId],
    queryFn: () => documentsApi.claim.candidates(claimId),
    enabled: editable,
  });

  const refresh = () => {
    void qc.invalidateQueries({ queryKey: ['rcm'] });
    void qc.invalidateQueries({ queryKey: ['claim-timeline', claimId] });
    void qc.invalidateQueries({ queryKey: ['claim-health', claimId] });
    void qc.invalidateQueries({ queryKey: ['explorer'] });
    void qc.invalidateQueries({ queryKey: ['claim-edit', claimId] });
  };

  const attach = useMutation({
    mutationFn: (ids: string[]) => documentsApi.claim.attach(claimId, ids),
    onSuccess: refresh,
    onError: (e) => showError('Attach failed', e),
  });
  const detach = useMutation({
    mutationFn: (documentId: string) => documentsApi.claim.detach(claimId, documentId),
    onSuccess: refresh,
    onError: (e) => showError('Detach failed', e),
  });

  const open = (doc: ClaimDocument) => documentsApi.open(doc).catch((e) => showError('Could not open the document', e));
  const budgetBytes = new Map((budget.data?.attachments ?? []).filter((a) => a.documentId).map((a) => [a.documentId!, a]));
  const attachedIds = new Set((attached.data?.items ?? []).map((d) => d.id));
  const free = (candidates.data ?? []).filter((d) => !attachedIds.has(d.id));
  const inlineCount = (budget.data?.attachments ?? []).filter((a) => a.source === 'inline').length;

  const common: ColumnsType<ClaimDocument> = [
    {
      title: 'Document',
      render: (_, r) => (
        <Space direction="vertical" size={0}>
          <a onClick={() => open(r)}>{r.title || r.fileName}</a>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {r.documentType}
          </Typography.Text>
        </Space>
      ),
    },
    { title: 'Encounter', render: (_, r) => [r.encounterNumber, r.encounterType].filter(Boolean).join(' · ') || '—' },
    { title: 'Invoice', dataIndex: 'invoiceNumber', render: (v) => v ?? '—' },
    { title: 'Service date', dataIndex: 'serviceDate', render: (v) => fmtDate(v) },
  ];

  const attachedColumns: ColumnsType<ClaimDocument> = [
    { title: '#', width: 50, render: (_, r) => r.claims.find((c) => c.claimId === claimId)?.sequence ?? '' },
    ...common,
    { title: 'In message', align: 'right', render: (_, r) => fmtBytes(budgetBytes.get(r.id)?.encodedBytes ?? r.encodedSizeBytes) },
    {
      title: '',
      width: 90,
      render: (_, r) => (
        <Space size={0}>
          <Button type="text" size="small" icon={<EyeOutlined />} onClick={() => open(r)} />
          <Popconfirm title="Detach from this claim?" disabled={!editable} onConfirm={() => detach.mutate(r.id)}>
            <Tooltip title={editable ? 'Detach' : 'Claim is no longer editable'}>
              <Button type="text" size="small" danger icon={<DisconnectOutlined />} disabled={!editable} loading={detach.isPending && detach.variables === r.id} />
            </Tooltip>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const candidateColumns: ColumnsType<ClaimDocument> = [
    ...common,
    { title: 'Size', align: 'right', render: (_, r) => fmtBytes(r.encodedSizeBytes) },
    {
      title: 'Match',
      render: (_, r) =>
        r.inClaimPeriod ? <Tag color="green">in claim period</Tag> : <Tooltip title="Same patient, outside this claim's billable period / encounter"><Tag>other period</Tag></Tooltip>,
    },
    {
      title: '',
      width: 100,
      render: (_, r) => (
        <Button size="small" icon={<LinkOutlined />} loading={attach.isPending && attach.variables?.includes(r.id)} onClick={() => attach.mutate([r.id])}>
          Attach
        </Button>
      ),
    },
  ];

  return (
    <>
      <SizeBudgetBar budget={budget.data} />
      {!editable && (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message={`Claim status is ${nphiesStatus ?? 'unknown'}; documents can only be changed while the claim is Draft, Error or ValidationFailed. Send further documents as a communication.`}
        />
      )}
      {inlineCount > 0 && (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message={`${inlineCount} legacy inline attachment(s) are stored on the claim itself and count towards the size budget.`}
        />
      )}

      <Space style={{ justifyContent: 'space-between', width: '100%', marginBottom: 8 }}>
        <Typography.Text strong>Attached ({attached.data?.items.length ?? 0})</Typography.Text>
        <Button type="primary" size="small" icon={<UploadOutlined />} disabled={!editable} onClick={() => setUploading(true)}>
          Upload & attach
        </Button>
      </Space>
      <Table
        rowKey="id"
        size="small"
        pagination={false}
        loading={attached.isLoading}
        columns={attachedColumns}
        dataSource={attached.data?.items}
        locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="No documents attached" /> }}
      />

      {editable && (
        <>
          <Space style={{ justifyContent: 'space-between', width: '100%', margin: '20px 0 8px' }}>
            <Typography.Text strong>Available for this patient ({free.length})</Typography.Text>
            {free.some((d) => d.inClaimPeriod) && (
              <Button size="small" loading={attach.isPending} onClick={() => attach.mutate(free.filter((d) => d.inClaimPeriod).map((d) => d.id))}>
                Attach all in claim period
              </Button>
            )}
          </Space>
          <Table
            rowKey="id"
            size="small"
            pagination={{ pageSize: 10, hideOnSinglePage: true }}
            loading={candidates.isLoading}
            columns={candidateColumns}
            dataSource={free}
            locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="No other documents for this patient" /> }}
          />
        </>
      )}

      <DocumentUploadModal open={uploading} claimId={claimId} onClose={() => setUploading(false)} onUploaded={refresh} />
    </>
  );
}
