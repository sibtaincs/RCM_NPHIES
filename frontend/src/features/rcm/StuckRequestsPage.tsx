import { useState } from 'react';
import { Alert, Button, Card, Select, Space, Table, Tag, Typography, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { ReloadOutlined, SyncOutlined } from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { rcmApi } from '@/api/endpoints/rcm';
import { useGlobalFilters } from '@/hooks/useGlobalFilters';
import { PERMISSIONS, usePermission } from '@/hooks/usePermission';
import type { StuckRequest } from '@/types/rcm';
import { fmtDate, statusTag } from '@/utils/display';
import { ClaimTimelineDrawer } from './ClaimTimelineDrawer';
import { PageHeader, showError } from './shared';

const LEVELS = [
  { value: 'Flagged', label: 'Flagged' },
  { value: 'Escalated', label: 'Escalated (contact NPHIES support)' },
  { value: 'Resolved', label: 'Resolved' },
  { value: 'All', label: 'All' },
];

/** Claims and prior authorizations that were sent but have had no final response for too long. */
export function StuckRequestsPage() {
  const global = useGlobalFilters();
  const { can } = usePermission();
  const qc = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [level, setLevel] = useState<string>();
  const [claimId, setClaimId] = useState<string>();

  const q = { ...global.rcm, status: level, page, pageSize };
  const list = useQuery({ queryKey: ['rcm', 'stuck', q], queryFn: () => rcmApi.messages.stuck(q), placeholderData: keepPreviousData });

  const scan = useMutation({
    mutationFn: () => rcmApi.messages.scanStuck(),
    onSuccess: (r) => {
      void message.success(`Scan done: ${r.flagged} flagged, ${r.escalated} escalated, ${r.resolved} resolved, ${r.open} still waiting`);
      void qc.invalidateQueries({ queryKey: ['rcm', 'stuck'] });
    },
    onError: (e) => showError('The scan failed', e),
  });

  const escalated = (list.data?.items ?? []).filter((r) => r.level === 'Escalated').length;

  const columns: ColumnsType<StuckRequest> = [
    { title: 'Claim', dataIndex: 'claimNumber', render: (v, r) => <a onClick={() => setClaimId(r.claimId)}>{v ?? r.claimId}</a> },
    { title: 'Type', dataIndex: 'use', render: (v: string | null) => (v?.startsWith('pre') ? 'Prior authorization' : 'Claim') },
    { title: 'Status', dataIndex: 'claimStatus', render: statusTag },
    { title: 'Level', dataIndex: 'level', render: (v: string) => <Tag color={v === 'Escalated' ? 'red' : v === 'Flagged' ? 'orange' : 'green'}>{v}</Tag> },
    { title: 'Waiting since', dataIndex: 'waitingSince', render: (v) => fmtDate(v, true) },
    { title: 'Days', dataIndex: 'daysWaiting', align: 'right' },
    { title: 'Held by', dataIndex: 'heldByNphies', render: (v: boolean) => (v ? 'NPHIES' : 'Payer') },
    { title: 'What to do', dataIndex: 'guidance', render: (v) => <div style={{ maxWidth: 360, whiteSpace: 'normal' }}>{v ?? '—'}</div> },
    { title: 'Bundle id', dataIndex: 'bundleId', render: (v) => (v ? <Typography.Text copyable>{v}</Typography.Text> : '—') },
    { title: 'Message id', dataIndex: 'messageId', render: (v) => (v ? <Typography.Text copyable>{v}</Typography.Text> : '—') },
  ];

  return (
    <>
      <PageHeader
        title="Stuck requests"
        extra={
          <Space>
            <Button icon={<SyncOutlined />} loading={scan.isPending} disabled={!can(PERMISSIONS.ClaimsSubmit)} onClick={() => scan.mutate()}>
              Scan now
            </Button>
            <Button icon={<ReloadOutlined />} onClick={() => void list.refetch()} />
          </Space>
        }
      />
      {escalated > 0 && (
        <Alert
          type="error"
          showIcon
          style={{ marginBottom: 12 }}
          message={`${escalated} request(s) on this page have had no final response within the NPHIES limit. Contact NPHIES support and quote the bundle id and message id.`}
        />
      )}
      <Card>
        <Space wrap style={{ marginBottom: 12 }}>
          <Select allowClear placeholder="Open (flagged and escalated)" style={{ width: 300 }} value={level} onChange={(v) => { setLevel(v); setPage(1); }} options={LEVELS} />
        </Space>
        <Table<StuckRequest>
          rowKey="id"
          size="middle"
          loading={list.isFetching}
          dataSource={list.data?.items ?? []}
          columns={columns}
          scroll={{ x: 'max-content' }}
          pagination={{
            current: page,
            pageSize,
            total: list.data?.total ?? 0,
            showSizeChanger: true,
            showTotal: (t) => `${t} requests`,
            onChange: (p, s) => {
              setPage(p);
              setPageSize(s);
            },
          }}
        />
      </Card>
      {claimId && <ClaimTimelineDrawer claimId={claimId} onClose={() => setClaimId(undefined)} />}
    </>
  );
}
