import { useState } from 'react';
import { Button, Card, Descriptions, Drawer, Input, Modal, Space, Table, Tabs } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { CheckOutlined, LinkOutlined, ReloadOutlined } from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { rcmApi } from '@/api/endpoints/rcm';
import { useGlobalFilters } from '@/hooks/useGlobalFilters';
import { PERMISSIONS, usePermission } from '@/hooks/usePermission';
import type { PaymentDetail, PaymentReconciliation } from '@/types/rcm';
import { fmtDate, fmtMoney, statusTag } from '@/utils/display';
import { ClaimTimelineDrawer } from './ClaimTimelineDrawer';
import { PageHeader, showError } from './shared';

export function PaymentsPage() {
  const global = useGlobalFilters();
  const { can } = usePermission();
  const canManage = can(PERMISSIONS.PaymentsManage);
  const qc = useQueryClient();
  const [paging, setPaging] = useState({ page: 1, pageSize: 25 });
  const [selectedId, setSelectedId] = useState<string>();
  const [matching, setMatching] = useState<PaymentDetail>();
  const [matchClaimId, setMatchClaimId] = useState('');
  const [claimId, setClaimId] = useState<string>();

  const q = { ...global.rcm, ...paging };
  const list = useQuery({ queryKey: ['rcm', 'payments', q], queryFn: () => rcmApi.payments.list(q), placeholderData: keepPreviousData });
  const discrepancies = useQuery({ queryKey: ['rcm', 'payment-discrepancies', global.rcm], queryFn: () => rcmApi.payments.discrepancies(global.rcm) });
  const detail = useQuery({ queryKey: ['rcm', 'payment', selectedId], queryFn: () => rcmApi.payments.get(selectedId!), enabled: !!selectedId });

  const refreshAll = () => void qc.invalidateQueries({ queryKey: ['rcm'] });

  const ack = useMutation({
    mutationFn: (id: string) => rcmApi.payments.acknowledge(id),
    onSuccess: (r) => {
      refreshAll();
      (r.acknowledgementError ? Modal.warning : Modal.success)({
        title: 'Payment notice',
        content: r.acknowledgementError ?? `Acknowledgement ${r.acknowledgementStatus ?? 'sent'}`,
      });
    },
    onError: (e) => showError('Acknowledgement failed', e),
  });

  const match = useMutation({
    mutationFn: () => rcmApi.payments.match(matching!.id, matchClaimId.trim()),
    onSuccess: () => {
      refreshAll();
      setMatching(undefined);
      setMatchClaimId('');
    },
    onError: (e) => showError('Matching failed', e),
  });

  const columns: ColumnsType<PaymentReconciliation> = [
    { title: 'Received', dataIndex: 'receivedAt', render: (v) => fmtDate(v, true) },
    { title: 'Identifier', dataIndex: 'identifier', render: (v, r) => <a onClick={() => setSelectedId(r.id)}>{v ?? r.paymentReconciliationId}</a> },
    { title: 'Payer', dataIndex: 'payerName' },
    { title: 'Period', render: (_, r) => `${fmtDate(r.periodStart)} – ${fmtDate(r.periodEnd)}` },
    { title: 'Payment date', dataIndex: 'paymentDate', render: (v) => fmtDate(v) },
    { title: 'Amount', dataIndex: 'paymentAmount', align: 'right', render: (v, r) => fmtMoney(v, r.paymentCurrency) },
    { title: 'Expected', dataIndex: 'expectedAmount', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Variance', dataIndex: 'varianceAmount', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Match', dataIndex: 'matchStatus', render: statusTag },
    { title: 'Ack', dataIndex: 'acknowledgementStatus', render: (v) => statusTag(v ?? 'pending') },
    {
      title: '',
      render: (_, r) =>
        !r.acknowledgedAt && (
          <Button size="small" icon={<CheckOutlined />} disabled={!canManage} loading={ack.isPending && ack.variables === r.id} onClick={() => ack.mutate(r.id)}>
            Acknowledge
          </Button>
        ),
    },
  ];

  const detailColumns = (withMatch: boolean): ColumnsType<PaymentDetail> => [
    { title: 'Type', dataIndex: 'detailType' },
    { title: 'Request', dataIndex: 'requestIdentifier' },
    { title: 'Claim', render: (_, r) => (r.matchedClaimId ? <a onClick={() => setClaimId(r.matchedClaimId!)}>{r.claimNumber ?? r.matchedClaimId}</a> : '—') },
    { title: 'Date', dataIndex: 'date', render: (v) => fmtDate(v) },
    { title: 'Amount', dataIndex: 'amount', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Expected', dataIndex: 'expectedAmount', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Variance', dataIndex: 'varianceAmount', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'NPHIES fee', dataIndex: 'nphiesFee', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Early fee', dataIndex: 'earlyFee', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Match', dataIndex: 'matchStatus', render: statusTag },
    ...(withMatch
      ? [
          {
            title: '',
            render: (_: unknown, r: PaymentDetail) => (
              <Button size="small" icon={<LinkOutlined />} disabled={!canManage} onClick={() => setMatching(r)}>
                Match
              </Button>
            ),
          },
        ]
      : []),
  ];

  const p = detail.data;

  return (
    <>
      <PageHeader title="Payment reconciliation" extra={<Button icon={<ReloadOutlined />} onClick={refreshAll} />} />
      <Card>
        <Tabs
          items={[
            {
              key: 'list',
              label: `Reconciliations (${list.data?.total ?? 0})`,
              children: (
                <Table<PaymentReconciliation>
                  rowKey="id"
                  size="middle"
                  loading={list.isFetching}
                  dataSource={list.data?.items ?? []}
                  columns={columns}
                  scroll={{ x: 'max-content' }}
                  pagination={{ current: paging.page, pageSize: paging.pageSize, total: list.data?.total ?? 0, showSizeChanger: true, onChange: (page, pageSize) => setPaging({ page, pageSize }) }}
                />
              ),
            },
            {
              key: 'discrepancies',
              label: `Discrepancies (${discrepancies.data?.length ?? 0})`,
              children: (
                <Table<PaymentDetail>
                  rowKey="id"
                  size="middle"
                  loading={discrepancies.isFetching}
                  dataSource={discrepancies.data ?? []}
                  columns={detailColumns(true)}
                  scroll={{ x: 'max-content' }}
                  pagination={{ pageSize: 25, showSizeChanger: true }}
                />
              ),
            },
          ]}
        />
      </Card>

      <Drawer open={!!selectedId} width={1000} title="Payment reconciliation" onClose={() => setSelectedId(undefined)} loading={detail.isLoading}>
        {p && (
          <>
            <Descriptions size="small" bordered column={3}>
              <Descriptions.Item label="Identifier">{p.identifier ?? p.paymentReconciliationId}</Descriptions.Item>
              <Descriptions.Item label="Status">{statusTag(p.status)}</Descriptions.Item>
              <Descriptions.Item label="Outcome">{statusTag(p.outcome)}</Descriptions.Item>
              <Descriptions.Item label="Payer">{p.payerName ?? p.payerId}</Descriptions.Item>
              <Descriptions.Item label="Payment date">{fmtDate(p.paymentDate)}</Descriptions.Item>
              <Descriptions.Item label="Payment ref">{p.paymentIdentifier}</Descriptions.Item>
              <Descriptions.Item label="Amount">{fmtMoney(p.paymentAmount, p.paymentCurrency)}</Descriptions.Item>
              <Descriptions.Item label="Expected">{fmtMoney(p.expectedAmount)}</Descriptions.Item>
              <Descriptions.Item label="Variance">{fmtMoney(p.varianceAmount)}</Descriptions.Item>
              <Descriptions.Item label="NPHIES fees">{fmtMoney(p.totalNphiesFee)}</Descriptions.Item>
              <Descriptions.Item label="Early fees">{fmtMoney(p.totalEarlyFee)}</Descriptions.Item>
              <Descriptions.Item label="Match">{statusTag(p.matchStatus)}</Descriptions.Item>
              <Descriptions.Item label="Acknowledgement">{statusTag(p.acknowledgementStatus ?? 'pending')}</Descriptions.Item>
              <Descriptions.Item label="Acknowledged at">{fmtDate(p.acknowledgedAt, true)}</Descriptions.Item>
              <Descriptions.Item label="Disposition">{p.disposition}</Descriptions.Item>
            </Descriptions>
            {p.acknowledgementError && <p style={{ color: '#cf1322', marginTop: 8 }}>{p.acknowledgementError}</p>}
            <Space style={{ margin: '16px 0' }}>
              {!p.acknowledgedAt && (
                <Button type="primary" icon={<CheckOutlined />} disabled={!canManage} loading={ack.isPending} onClick={() => ack.mutate(p.id)}>
                  Send payment notice
                </Button>
              )}
            </Space>
            <Table<PaymentDetail> rowKey="id" size="small" dataSource={p.details} columns={detailColumns(true)} scroll={{ x: 'max-content' }} pagination={false} />
          </>
        )}
      </Drawer>

      <Modal
        open={!!matching}
        title={`Match payment line ${matching?.requestIdentifier ?? ''}`}
        okText="Match"
        okButtonProps={{ disabled: !matchClaimId.trim(), loading: match.isPending }}
        onCancel={() => setMatching(undefined)}
        onOk={() => match.mutate()}
        destroyOnClose
      >
        <p>Amount: {fmtMoney(matching?.amount)}</p>
        <Input placeholder="Claim id" value={matchClaimId} onChange={(e) => setMatchClaimId(e.target.value)} />
      </Modal>

      {claimId && <ClaimTimelineDrawer claimId={claimId} onClose={() => setClaimId(undefined)} />}
    </>
  );
}
