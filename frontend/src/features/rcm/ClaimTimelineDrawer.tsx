import {
  Alert,
  Button,
  Descriptions,
  Drawer,
  Empty,
  Space,
  Spin,
  Table,
  Tabs,
  Tag,
  Timeline,
  Typography,
} from 'antd';
import { CodeOutlined, EditOutlined } from '@ant-design/icons';
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { rcmApi } from '@/api/endpoints/rcm';
import { explorerApi } from '@/api/endpoints/explorer';
import type { ClaimAdjudication } from '@/types/rcm';
import type { ClaimFieldChange } from '@/types/claimEditor';
import { getApiErrorMessage } from '@/utils/apiError';
import { fmtDate, fmtMoney, renderValue, statusTag } from '@/utils/display';
import { useExplorerTables } from '@/features/data-explorer/useExplorerTables';
import { ExplorerGrid } from '@/features/data-explorer/ExplorerGrid';
import { BundlePreviewModal, TransactionJsonModal } from './JsonViewerModal';
import { ClaimDocumentsTab } from './ClaimDocumentsTab';

function ChangesTable({ changes }: { changes: ClaimFieldChange[] }) {
  return (
    <Table
      size="small"
      rowKey={(r) => r.field}
      pagination={false}
      dataSource={changes}
      columns={[
        { title: 'Field', dataIndex: 'field', width: 200 },
        { title: 'Before', dataIndex: 'before', render: (v) => <Typography.Text delete={!!v} type="secondary">{v ?? '—'}</Typography.Text> },
        { title: 'After', dataIndex: 'after', render: (v) => <Typography.Text strong>{v ?? '—'}</Typography.Text> },
      ]}
    />
  );
}

function HealthTab({ claimId }: { claimId: string }) {
  const q = useQuery({ queryKey: ['claim-health', claimId], queryFn: () => rcmApi.claims.health(claimId) });
  if (q.isLoading) return <Spin />;
  if (q.error) return <Alert type="error" message={getApiErrorMessage(q.error)} />;
  const h = q.data!;
  if (h.issues.length === 0) return <Alert type="success" showIcon message="No problems found — the claim is ready for NPHIES." />;
  return (
    <>
      <Alert
        style={{ marginBottom: 12 }}
        type={h.errorCount ? 'error' : 'warning'}
        showIcon
        message={`${h.errorCount} errors · ${h.warningCount} warnings`}
        description={h.ready ? 'Can be sent; review the warnings first.' : 'Fix the errors before sending.'}
      />
      <Table
        size="small"
        rowKey={(r, i) => `${r.code}-${r.field}-${i}`}
        pagination={false}
        dataSource={h.issues}
        columns={[
          { title: 'Severity', dataIndex: 'severity', render: (v) => <Tag color={v === 'Error' ? 'red' : v === 'Warning' ? 'orange' : 'blue'}>{v}</Tag> },
          { title: 'Field', dataIndex: 'field' },
          { title: 'Problem', dataIndex: 'message' },
          { title: 'Code', dataIndex: 'code' },
        ]}
      />
    </>
  );
}

function AuditTab({ claimId }: { claimId: string }) {
  const q = useQuery({ queryKey: ['claim-audit', claimId], queryFn: () => rcmApi.claims.audit(claimId) });
  if (q.isLoading) return <Spin />;
  if (q.error) return <Alert type="error" message={getApiErrorMessage(q.error)} />;
  if (!q.data?.length) return <Empty description="No edits recorded yet" />;
  return (
    <Timeline
      items={q.data.map((e) => ({
        key: e.id,
        color: e.action === 'ClaimCreated' ? 'green' : 'blue',
        children: (
          <>
            <Typography.Text strong>{e.action === 'ClaimCreated' ? 'Created' : 'Edited'}</Typography.Text>{' '}
            <Typography.Text type="secondary">
              {fmtDate(e.at, true)} · {e.user ?? 'system'}
            </Typography.Text>
            {e.reason && <div>Reason: {e.reason}</div>}
            {e.changes.length > 0 && (
              <div style={{ marginTop: 6 }}>
                <ChangesTable changes={e.changes} />
              </div>
            )}
          </>
        ),
      }))}
    />
  );
}

function CompareTab({ claimId }: { claimId: string }) {
  const q = useQuery({ queryKey: ['claim-compare', claimId], queryFn: () => rcmApi.claims.compare(claimId) });
  if (q.isLoading) return <Spin />;
  if (q.error) return <Alert type="error" message={getApiErrorMessage(q.error)} />;
  const c = q.data!;
  if (!c.previousClaimId) return <Empty description="No previous version — this record was not created from another claim / pre-auth." />;
  return (
    <>
      <Typography.Paragraph>
        Compared with <b>{c.previousClaimNumber}</b> ({c.relationship ?? 'related'}). {c.changes.length} differences.
      </Typography.Paragraph>
      {c.changes.length ? <ChangesTable changes={c.changes} /> : <Empty description="Identical content" />}
    </>
  );
}

function AdjudicationView({ a }: { a: ClaimAdjudication }) {
  return (
    <>
      <Descriptions size="small" bordered column={{ xs: 1, md: 3 }} style={{ marginBottom: 12 }}>
        <Descriptions.Item label="Outcome">{statusTag(a.outcome)}</Descriptions.Item>
        <Descriptions.Item label="Adjudication">
          {statusTag(a.adjudicationOutcome)}
        </Descriptions.Item>
        <Descriptions.Item label="Received">{fmtDate(a.receivedAt, true)}</Descriptions.Item>
        <Descriptions.Item label="Submitted">{fmtMoney(a.totalSubmitted)}</Descriptions.Item>
        <Descriptions.Item label="Eligible">{fmtMoney(a.totalEligible)}</Descriptions.Item>
        <Descriptions.Item label="Benefit">{fmtMoney(a.totalBenefit)}</Descriptions.Item>
        <Descriptions.Item label="Copay">{fmtMoney(a.totalCopay)}</Descriptions.Item>
        <Descriptions.Item label="Tax (VAT)">{fmtMoney(a.totalTax)}</Descriptions.Item>
        <Descriptions.Item label="Patient share">{fmtMoney(a.totalPatientShare)}</Descriptions.Item>
        {a.preAuthRef && <Descriptions.Item label="PreAuth ref">{a.preAuthRef}</Descriptions.Item>}
        {a.preAuthPeriodEnd && (
          <Descriptions.Item label="PreAuth valid until">
            {fmtDate(a.preAuthPeriodEnd)}
          </Descriptions.Item>
        )}
        {a.disposition && (
          <Descriptions.Item label="Disposition">{a.disposition}</Descriptions.Item>
        )}
      </Descriptions>
      <Table
        size="small"
        rowKey="sequence"
        pagination={false}
        dataSource={a.items}
        scroll={{ x: 'max-content' }}
        columns={[
          { title: '#', dataIndex: 'sequence' },
          { title: 'Outcome', dataIndex: 'outcome', render: statusTag },
          {
            title: 'Submitted',
            dataIndex: 'submitted',
            align: 'right',
            render: (v) => fmtMoney(v),
          },
          { title: 'Eligible', dataIndex: 'eligible', align: 'right', render: (v) => fmtMoney(v) },
          { title: 'Benefit', dataIndex: 'benefit', align: 'right', render: (v) => fmtMoney(v) },
          { title: 'Copay', dataIndex: 'copay', align: 'right', render: (v) => fmtMoney(v) },
          {
            title: 'Deductible',
            dataIndex: 'deductible',
            align: 'right',
            render: (v) => fmtMoney(v),
          },
          { title: 'Tax', dataIndex: 'tax', align: 'right', render: (v) => fmtMoney(v) },
          {
            title: 'Patient share',
            dataIndex: 'patientShare',
            align: 'right',
            render: (v) => fmtMoney(v),
          },
          { title: 'Approved qty', dataIndex: 'approvedQuantity', align: 'right' },
          {
            title: 'Reason',
            render: (_, r) => (r.reasonCode ? `${r.reasonCode} ${r.reasonDisplay ?? ''}` : '—'),
          },
        ]}
      />
    </>
  );
}

/** Claim / pre-auth master-detail: header, items, adjudication, NPHIES message timeline and related records. */
export function ClaimTimelineDrawer({
  claimId,
  onClose,
  onEdit,
}: {
  claimId: string;
  onClose: () => void;
  onEdit?: (claimId: string) => void;
}) {
  const { byName } = useExplorerTables();
  const timeline = useQuery({
    queryKey: ['claim-timeline', claimId],
    queryFn: () => rcmApi.claims.timeline(claimId),
  });
  const header = useQuery({
    queryKey: ['explorer-record', 'Claims', claimId],
    queryFn: () => explorerApi.get('Claims', claimId),
  });
  const t = timeline.data;
  const claimsMeta = byName.get('Claims');
  const [jsonTx, setJsonTx] = useState<string>();
  const [preview, setPreview] = useState(false);

  const childGrid = (table: string, fk = 'claimId') => {
    const meta = byName.get(table);
    return meta ? (
      <ExplorerGrid compact applyDateRange={false} table={meta} fixedFilters={{ [fk]: claimId }} />
    ) : (
      <Empty />
    );
  };

  const tabs = t
    ? [
        {
          key: 'header',
          label: 'Header',
          children: (
            <Descriptions size="small" bordered column={{ xs: 1, md: 2 }}>
              {(claimsMeta?.columns ?? [])
                .filter((c) => !c.large)
                .map((c) => (
                  <Descriptions.Item key={c.key} label={c.label}>
                    {renderValue(header.data?.row[c.key], c.type)}
                  </Descriptions.Item>
                ))}
            </Descriptions>
          ),
        },
        { key: 'items', label: 'Items', children: childGrid('ClaimItems') },
        { key: 'diagnoses', label: 'Diagnoses', children: childGrid('ClaimDiagnoses') },
        { key: 'careteam', label: 'Care team', children: childGrid('ClaimCareTeams') },
        {
          key: 'supporting',
          label: 'Supporting info',
          children: childGrid('ClaimSupportingInfos'),
        },
        {
          key: 'documents',
          label: 'Documents',
          children: <ClaimDocumentsTab claimId={claimId} nphiesStatus={t.claim.nphiesStatus} />,
        },
        {
          key: 'adjudication',
          label: `Adjudication (${t.adjudications.length})`,
          children: t.adjudications.length ? (
            <Tabs
              items={t.adjudications.map((a, i) => ({
                key: a.claimResponseId,
                label: `${i + 1}. ${fmtDate(a.receivedAt, true)}`,
                children: <AdjudicationView a={a} />,
              }))}
            />
          ) : (
            <Empty description="No adjudication yet" />
          ),
        },
        {
          key: 'messages',
          label: `NPHIES messages (${t.transactions.length})`,
          children: (
            <Timeline
              items={t.transactions.map((x) => ({
                color: /error|fail/i.test(x.status)
                  ? 'red'
                  : x.direction === 'inbound'
                    ? 'green'
                    : 'blue',
                children: (
                  <>
                    <Typography.Text strong>
                      {x.direction} {x.operation}
                    </Typography.Text>{' '}
                    {statusTag(x.status)} {x.outcome && statusTag(x.outcome)}{' '}
                    <Button
                      size="small"
                      type="link"
                      icon={<CodeOutlined />}
                      onClick={() => setJsonTx(x.id)}
                    >
                      View JSON
                    </Button>
                    <div>
                      <Typography.Text type="secondary">
                        {[fmtDate(x.sentAt, true), x.eventCode, x.messageId].join(' · ')}
                      </Typography.Text>
                    </div>
                    {x.errorSummary && (
                      <Typography.Text type="danger">{x.errorSummary}</Typography.Text>
                    )}
                  </>
                ),
              }))}
            />
          ),
        },
        {
          key: 'denials',
          label: `Denials (${t.denials.length})`,
          children: (
            <Table
              size="small"
              rowKey="id"
              pagination={false}
              dataSource={t.denials}
              columns={[
                { title: 'Item', dataIndex: 'itemSequence' },
                { title: 'Reason', render: (_, d) => `${d.reasonCode} ${d.reasonDisplay ?? ''}` },
                { title: 'Category', dataIndex: 'category' },
                {
                  title: 'Denied',
                  dataIndex: 'deniedAmount',
                  align: 'right',
                  render: (v) => fmtMoney(v),
                },
                { title: 'Status', dataIndex: 'status', render: statusTag },
                { title: 'Appeal by', dataIndex: 'appealDeadline', render: (v) => fmtDate(v) },
              ]}
            />
          ),
        },
        {
          key: 'comms',
          label: `Communications (${t.communicationRequests.length + t.communications.length})`,
          children: (
            <>
              <Typography.Title level={5}>Insurer requests</Typography.Title>
              <Table
                size="small"
                rowKey="id"
                pagination={false}
                dataSource={t.communicationRequests}
                columns={[
                  { title: 'Received', dataIndex: 'receivedAt', render: (v) => fmtDate(v, true) },
                  { title: 'Category', dataIndex: 'category' },
                  { title: 'Request', dataIndex: 'payloadText' },
                  { title: 'Responded', dataIndex: 'respondedAt', render: (v) => fmtDate(v, true) },
                ]}
              />
              <Typography.Title level={5} style={{ marginTop: 16 }}>
                Communications
              </Typography.Title>
              <Table
                size="small"
                rowKey="id"
                pagination={false}
                dataSource={t.communications}
                columns={[
                  { title: 'Date', dataIndex: 'createdAt', render: (v) => fmtDate(v, true) },
                  { title: 'Direction', dataIndex: 'direction' },
                  { title: 'Status', dataIndex: 'status', render: statusTag },
                  { title: 'Text', dataIndex: 'payloadText' },
                ]}
              />
            </>
          ),
        },
        {
          key: 'payments',
          label: `Payments (${t.payments.length})`,
          children: (
            <Table
              size="small"
              rowKey="id"
              pagination={false}
              dataSource={t.payments}
              columns={[
                { title: 'Date', dataIndex: 'date', render: (v) => fmtDate(v) },
                { title: 'Type', dataIndex: 'detailType' },
                {
                  title: 'Amount',
                  dataIndex: 'amount',
                  align: 'right',
                  render: (v) => fmtMoney(v),
                },
                {
                  title: 'Expected',
                  dataIndex: 'expectedAmount',
                  align: 'right',
                  render: (v) => fmtMoney(v),
                },
                {
                  title: 'Variance',
                  dataIndex: 'varianceAmount',
                  align: 'right',
                  render: (v) => fmtMoney(v),
                },
                { title: 'Match', dataIndex: 'matchStatus', render: statusTag },
              ]}
            />
          ),
        },
        {
          key: 'related',
          label: `Related (${t.related.length})`,
          children: (
            <Table
              size="small"
              rowKey={(r) => `${r.claimId}-${r.identifier}`}
              pagination={false}
              dataSource={t.related}
              columns={[
                { title: 'Relationship', dataIndex: 'relationship' },
                { title: 'Claim id', dataIndex: 'claimId' },
                { title: 'Identifier', dataIndex: 'identifier' },
              ]}
            />
          ),
        },
        { key: 'health', label: 'Health check', children: <HealthTab claimId={claimId} /> },
        { key: 'audit', label: 'Audit trail', children: <AuditTab claimId={claimId} /> },
        { key: 'compare', label: 'Compare with previous', children: <CompareTab claimId={claimId} /> },
      ]
    : [];

  return (
    <Drawer
      open
      width="90%"
      onClose={onClose}
      destroyOnClose
      extra={
        <Space>
          {onEdit && (
            <Button icon={<EditOutlined />} onClick={() => onEdit(claimId)}>
              {['Draft', 'Error', 'ValidationFailed'].includes(t?.claim.nphiesStatus ?? 'Draft') ? 'Edit' : 'View in editor'}
            </Button>
          )}
          <Button icon={<CodeOutlined />} onClick={() => setPreview(true)}>
            Preview request JSON
          </Button>
        </Space>
      }
      title={
        t ? (
          <>
            {[
              t.claim.claimNumber,
              t.claim.patientName ?? t.claim.patientId,
              fmtMoney(t.claim.total),
            ].join(' · ')}{' '}
            {statusTag(t.claim.nphiesStatus ?? t.claim.status)}
          </>
        ) : (
          'Claim'
        )
      }
    >
      {timeline.isLoading && <Spin />}
      {timeline.error && <Alert type="error" message={getApiErrorMessage(timeline.error)} />}
      {t && <Tabs items={tabs} />}
      {jsonTx && (
        <TransactionJsonModal transactionId={jsonTx} onClose={() => setJsonTx(undefined)} />
      )}
      {preview && <BundlePreviewModal claimId={claimId} onClose={() => setPreview(false)} />}
    </Drawer>
  );
}
