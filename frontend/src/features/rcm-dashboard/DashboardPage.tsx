import { useState } from 'react';
import { Alert, Button, Card, Col, Modal, Progress, Row, Segmented, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { ReloadOutlined, SyncOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { rcmApi } from '@/api/endpoints/rcm';
import { useGlobalFilters } from '@/hooks/useGlobalFilters';
import { PERMISSIONS, usePermission } from '@/hooks/usePermission';
import type { AgingBucket, DenialRate, PayerBreakdown } from '@/types/rcm';
import { fmtMoney, fmtPct } from '@/utils/display';
import { Kpi, PageHeader, showError } from '@/features/rcm/shared';

const GROUPS = [
  { value: 'month', label: 'Month' },
  { value: 'week', label: 'Week' },
  { value: 'payer', label: 'Payer' },
  { value: 'reason', label: 'Reason' },
];

/** RCM dashboard: KPIs, A/R aging, denial rate and payer breakdown for the selected hospital and date range. */
export function DashboardPage() {
  const global = useGlobalFilters();
  const { can } = usePermission();
  const qc = useQueryClient();
  const [groupBy, setGroupBy] = useState('month');
  const enabled = can(PERMISSIONS.ReportsRead);

  const dash = useQuery({ queryKey: ['rcm', 'dashboard', global.rcm], queryFn: () => rcmApi.reports.dashboard(global.rcm), enabled });
  const aging = useQuery({ queryKey: ['rcm', 'aging', global.rcm], queryFn: () => rcmApi.reports.aging(global.rcm), enabled });
  const rate = useQuery({ queryKey: ['rcm', 'denial-rate', global.rcm, groupBy], queryFn: () => rcmApi.reports.denialRate(global.rcm, groupBy), enabled });
  const payers = useQuery({ queryKey: ['rcm', 'payers', global.rcm], queryFn: () => rcmApi.reports.payers(global.rcm), enabled });

  const poll = useMutation({
    mutationFn: () => rcmApi.messages.poll(global.hospitalId),
    onSuccess: (r) => {
      void qc.invalidateQueries({ queryKey: ['rcm'] });
      (r.errors.length ? Modal.warning : Modal.success)({
        title: 'NPHIES poll complete',
        content: (
          <>
            <p>
              {r.messagesReceived} messages received, {r.processed} processed, {r.duplicates} duplicates, {r.unmatched} unmatched,{' '}
              {r.paymentNoticesSent} payment notices sent.
            </p>
            {r.errors.length > 0 && <ul>{r.errors.map((e) => <li key={e}>{e}</li>)}</ul>}
          </>
        ),
      });
    },
    onError: (e) => showError('Poll failed', e),
  });

  const d = dash.data;
  const agingMax = Math.max(1, ...(aging.data ?? []).map((a) => a.amount));

  const agingColumns: ColumnsType<AgingBucket> = [
    { title: 'Bucket', dataIndex: 'bucket', width: 110 },
    { title: 'Claims', dataIndex: 'count', align: 'right', width: 80 },
    { title: 'Outstanding', dataIndex: 'amount', align: 'right', width: 160, render: (v) => fmtMoney(v) },
    { title: '', dataIndex: 'amount', render: (v: number) => <Progress percent={(v / agingMax) * 100} showInfo={false} size="small" /> },
  ];

  const rateColumns: ColumnsType<DenialRate> = [
    { title: GROUPS.find((g) => g.value === groupBy)?.label, dataIndex: 'period' },
    { title: 'Adjudicated', dataIndex: 'adjudicated', align: 'right' },
    { title: 'Denied', dataIndex: 'denied', align: 'right' },
    { title: 'Partial', dataIndex: 'partiallyDenied', align: 'right' },
    { title: 'Billed', dataIndex: 'billedAmount', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Denied amount', dataIndex: 'deniedAmount', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Denial rate', dataIndex: 'denialRate', width: 180, render: (v: number) => <Progress percent={Number(v.toFixed(1))} size="small" status={v > 15 ? 'exception' : 'normal'} /> },
    { title: 'Amount rate', dataIndex: 'amountDenialRate', align: 'right', render: (v) => fmtPct(v) },
  ];

  const payerColumns: ColumnsType<PayerBreakdown> = [
    { title: 'Payer', render: (_, r) => r.insurerName ?? r.insurerId },
    { title: 'Claims', dataIndex: 'claims', align: 'right' },
    { title: 'Approved', dataIndex: 'approved', align: 'right' },
    { title: 'Rejected', dataIndex: 'rejected', align: 'right' },
    { title: 'Pending', dataIndex: 'pending', align: 'right' },
    { title: 'Billed', dataIndex: 'billedAmount', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Approved amt', dataIndex: 'approvedAmount', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Paid', dataIndex: 'paidAmount', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Outstanding', dataIndex: 'outstandingAmount', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Denial rate', dataIndex: 'denialRate', align: 'right', render: (v) => fmtPct(v) },
    { title: 'Days to pay', dataIndex: 'averageDaysToPayment', align: 'right', render: (v?: number | null) => (v === null || v === undefined ? '—' : v.toFixed(1)) },
  ];

  const refresh = () => void qc.invalidateQueries({ queryKey: ['rcm'] });

  return (
    <>
      <PageHeader
        title="RCM dashboard"
        extra={
          <>
            <Button icon={<SyncOutlined />} loading={poll.isPending} disabled={!can(PERMISSIONS.ClaimsSubmit)} onClick={() => poll.mutate()}>
              Poll NPHIES now
            </Button>{' '}
            <Button icon={<ReloadOutlined />} onClick={refresh} />
          </>
        }
      />
      {!enabled && <Alert type="info" showIcon message="You need the reports:read permission to see the dashboard." />}
      {enabled && (
        <>
          <Row gutter={[12, 12]}>
            <Col xs={12} md={6} xl={4}><Kpi title="Claims submitted" value={d?.claimsSubmitted} /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Adjudicated" value={d?.claimsAdjudicated} /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Pending at payer" value={d?.claimsPending} color="#d48806" /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Approved" value={d?.claimsApproved} color="#389e0d" /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Partially approved" value={d?.claimsPartiallyApproved} /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Rejected" value={d?.claimsRejected} color="#cf1322" /></Col>

            <Col xs={12} md={6} xl={4}><Kpi title="Billed" value={d?.billedAmount} precision={2} suffix="SAR" /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Approved amount" value={d?.approvedAmount} precision={2} suffix="SAR" /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Paid" value={d?.paidAmount} precision={2} suffix="SAR" color="#389e0d" /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Outstanding" value={d?.outstandingAmount} precision={2} suffix="SAR" color="#d48806" /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Denied amount" value={d?.deniedAmount} precision={2} suffix="SAR" color="#cf1322" /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Patient share" value={d?.patientShareAmount} precision={2} suffix="SAR" /></Col>

            <Col xs={12} md={6} xl={4}><Kpi title="Denial rate" value={d?.denialRate} precision={1} suffix="%" /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Clean claim rate" value={d?.cleanClaimRate} precision={1} suffix="%" /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Collection rate" value={d?.collectionRate} precision={1} suffix="%" /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Days to adjudication" value={d?.averageDaysToAdjudication} precision={1} /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Days to payment" value={d?.averageDaysToPayment} precision={1} /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Eligibility rate" value={d?.eligibilityRate} precision={1} suffix="%" /></Col>

            <Col xs={12} md={6} xl={4}><Kpi title="Pre-auths submitted" value={d?.preAuthsSubmitted} /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Pre-auths approved" value={d?.preAuthsApproved} /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Open denials" value={d?.openDenials} color={d?.openDenials ? '#cf1322' : undefined} /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Open insurer requests" value={d?.openCommunicationRequests} color={d?.openCommunicationRequests ? '#d48806' : undefined} /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Unacknowledged payments" value={d?.unacknowledgedPayments} /></Col>
            <Col xs={12} md={6} xl={4}><Kpi title="Payment discrepancies" value={d?.paymentDiscrepancies} /></Col>
          </Row>

          <Row gutter={[12, 12]} style={{ marginTop: 12 }}>
            <Col xs={24} xl={10}>
              <Card title="A/R aging (outstanding)" size="small">
                <Table<AgingBucket> rowKey="bucket" size="small" pagination={false} loading={aging.isFetching} dataSource={aging.data ?? []} columns={agingColumns} />
              </Card>
            </Col>
            <Col xs={24} xl={14}>
              <Card
                size="small"
                title="Denial rate"
                extra={<Segmented size="small" options={GROUPS} value={groupBy} onChange={(v) => setGroupBy(String(v))} />}
              >
                <Table<DenialRate> rowKey="period" size="small" pagination={{ pageSize: 8 }} loading={rate.isFetching} dataSource={rate.data ?? []} columns={rateColumns} scroll={{ x: 'max-content' }} />
              </Card>
            </Col>
          </Row>

          <Card title="Payer breakdown" size="small" style={{ marginTop: 12 }}>
            <Table<PayerBreakdown> rowKey="insurerId" size="small" pagination={false} loading={payers.isFetching} dataSource={payers.data ?? []} columns={payerColumns} scroll={{ x: 'max-content' }} />
          </Card>
          <Typography.Paragraph type="secondary" style={{ marginTop: 8 }}>
            Figures cover the hospital and date range selected in the header.
          </Typography.Paragraph>
        </>
      )}
    </>
  );
}
