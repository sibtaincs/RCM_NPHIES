import { Button, Card, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { ReloadOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { rcmApi } from '@/api/endpoints/rcm';
import { useGlobalFilters } from '@/hooks/useGlobalFilters';
import type { BatchClaim, BatchSubmitResult } from '@/types/rcm';
import { fmtDate, fmtMoney, statusTag } from '@/utils/display';
import { ClaimTimelineDrawer } from './ClaimTimelineDrawer';
import { PageHeader } from './shared';

export function BatchesPage() {
  const global = useGlobalFilters();
  const [claimId, setClaimId] = useState<string>();
  const list = useQuery({ queryKey: ['rcm', 'batches', global.rcm], queryFn: () => rcmApi.batches.list(global.rcm) });

  const columns: ColumnsType<BatchSubmitResult> = [
    { title: 'Batch', dataIndex: 'batchIdentifier' },
    { title: 'Claims', dataIndex: 'claimCount', align: 'right' },
    { title: 'Transaction', dataIndex: 'transactionStatus', render: statusTag },
    { title: 'Total', align: 'right', render: (_, r) => fmtMoney(r.claims.reduce((s, c) => s + (c.total ?? 0), 0)) },
    { title: 'Approved', align: 'right', render: (_, r) => fmtMoney(r.claims.reduce((s, c) => s + (c.approvedAmount ?? 0), 0)) },
    { title: 'Submitted', dataIndex: 'submittedAt', render: (v) => fmtDate(v, true) },
    { title: 'Completed', dataIndex: 'completedAt', render: (v) => fmtDate(v, true) },
    { title: 'Message id', dataIndex: 'messageId' },
    { title: 'Errors', render: (_, r) => (r.errors.length ? r.errors.join('; ') : '') },
  ];

  const claimColumns: ColumnsType<BatchClaim> = [
    { title: '#', dataIndex: 'batchNumber', width: 60 },
    { title: 'Claim', dataIndex: 'claimNumber', render: (v, r) => <a onClick={() => setClaimId(r.claimId)}>{v}</a> },
    { title: 'Status', dataIndex: 'claimStatus', render: statusTag },
    { title: 'Outcome', dataIndex: 'outcome', render: statusTag },
    { title: 'Total', dataIndex: 'total', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Approved', dataIndex: 'approvedAmount', align: 'right', render: (v) => fmtMoney(v) },
  ];

  return (
    <>
      <PageHeader title="Claim batches" extra={<Button icon={<ReloadOutlined />} onClick={() => void list.refetch()} />} />
      <Card>
        <Table<BatchSubmitResult>
          rowKey="batchIdentifier"
          size="middle"
          loading={list.isFetching}
          dataSource={list.data ?? []}
          columns={columns}
          scroll={{ x: 'max-content' }}
          expandable={{
            expandedRowRender: (r) => <Table<BatchClaim> rowKey="claimId" size="small" pagination={false} dataSource={r.claims} columns={claimColumns} />,
          }}
          pagination={{ pageSize: 25, showSizeChanger: true, showTotal: (t) => `${t} batches` }}
        />
      </Card>
      {claimId && <ClaimTimelineDrawer claimId={claimId} onClose={() => setClaimId(undefined)} />}
    </>
  );
}
