import { useState } from 'react';
import { Button, Card, Checkbox, Col, Form, Input, Modal, Row, Select, Space, Table, Tabs, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { ReloadOutlined, RollbackOutlined } from '@ant-design/icons';
import type { UploadFile } from 'antd/es/upload/interface';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { rcmApi } from '@/api/endpoints/rcm';
import { AttachmentPicker, toAttachments } from '@/components/AttachmentPicker/AttachmentPicker';
import { useGlobalFilters } from '@/hooks/useGlobalFilters';
import { PERMISSIONS, usePermission } from '@/hooks/usePermission';
import type { Appeal, Breakdown, Denial } from '@/types/rcm';
import { fmtDate, fmtMoney, statusTag } from '@/utils/display';
import { ClaimTimelineDrawer } from './ClaimTimelineDrawer';
import { InsurerSelect, Kpi, PageHeader, showError } from './shared';

/** Mirrors RcmResponseIngestor.DenialCategory and RcmDenialStatus. */
// The first five are the NPHIES rejection categories (AD, CV, MN, SE, BE); the rest cover older or non-NPHIES codes.
const CATEGORIES = ['Administrative', 'Coverage', 'MedicalNecessity', 'SupportingEvidence', 'BillingError', 'Authorization', 'Duplicate', 'Contractual', 'Technical', 'Other'];
const DENIAL_STATUSES = ['Open', 'Appealed', 'Resubmitted', 'Accepted', 'WrittenOff', 'Overturned', 'Upheld'];
const MANUAL_STATUSES = ['Open', 'Accepted', 'WrittenOff'];

function BreakdownTable({ title, data }: { title: string; data?: Breakdown[] }) {
  return (
    <Card size="small" title={title}>
      <Table<Breakdown>
        rowKey="key"
        size="small"
        pagination={false}
        dataSource={(data ?? []).slice(0, 8)}
        columns={[
          { title: title, render: (_, r) => r.label ?? r.key, ellipsis: true },
          { title: 'Count', dataIndex: 'count', align: 'right', width: 70 },
          { title: 'Amount', dataIndex: 'amount', align: 'right', render: (v) => fmtMoney(v) },
        ]}
      />
    </Card>
  );
}

interface AppealValues {
  reason: string;
  notes?: string;
  text?: string;
  files?: UploadFile[];
  resubmit: boolean;
}

export function DenialsPage() {
  const global = useGlobalFilters();
  const { can } = usePermission();
  const canManage = can(PERMISSIONS.ClaimsUpdate);
  const qc = useQueryClient();
  const [paging, setPaging] = useState({ page: 1, pageSize: 25 });
  const [appealPaging, setAppealPaging] = useState({ page: 1, pageSize: 25 });
  const [category, setCategory] = useState<string>();
  const [status, setStatus] = useState<string>();
  const [reasonCode, setReasonCode] = useState('');
  const [insurerId, setInsurerId] = useState<string>();
  const [selected, setSelected] = useState<Denial[]>([]);
  const [editing, setEditing] = useState<Denial>();
  const [appealing, setAppealing] = useState(false);
  const [claimId, setClaimId] = useState<string>();
  const [statusForm] = Form.useForm<{ status: string; notes?: string }>();
  const [appealForm] = Form.useForm<AppealValues>();

  const q = { ...global.rcm, ...paging, category, status, insurerId, reasonCode: reasonCode.trim() || undefined };
  const denials = useQuery({ queryKey: ['rcm', 'denials', q], queryFn: () => rcmApi.denials.list(q), placeholderData: keepPreviousData });
  const summary = useQuery({ queryKey: ['rcm', 'denial-summary', global.rcm], queryFn: () => rcmApi.denials.summary(global.rcm) });
  const aq = { ...global.rcm, ...appealPaging };
  const appeals = useQuery({ queryKey: ['rcm', 'appeals', aq], queryFn: () => rcmApi.appeals.list(aq), placeholderData: keepPreviousData });

  const refresh = () => void qc.invalidateQueries({ queryKey: ['rcm'] });

  const updateStatus = useMutation({
    mutationFn: (v: { status: string; notes?: string }) => rcmApi.denials.update(editing!.id, v),
    onSuccess: () => {
      refresh();
      setEditing(undefined);
    },
    onError: (e) => showError('Update failed', e),
  });

  const createAppeal = useMutation({
    mutationFn: async (v: AppealValues) =>
      rcmApi.appeals.create({
        claimId: selected[0].claimId,
        denialIds: selected.map((d) => d.id),
        reason: v.reason,
        notes: v.notes,
        texts: v.text?.trim() ? [v.text.trim()] : [],
        attachments: await toAttachments(v.files),
        resubmit: v.resubmit,
      }),
    onSuccess: (a) => {
      refresh();
      setAppealing(false);
      setSelected([]);
      appealForm.resetFields();
      (a.errors.length ? Modal.warning : Modal.success)({
        title: `Appeal ${a.appealNumber}`,
        content: (
          <>
            <p>Status: {statusTag(a.status)}</p>
            {a.errors.length > 0 && <ul>{a.errors.map((e) => <li key={e}>{e}</li>)}</ul>}
          </>
        ),
      });
    },
    onError: (e) => showError('Appeal failed', e),
  });

  const withdraw = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) => rcmApi.appeals.withdraw(id, reason),
    onSuccess: refresh,
    onError: (e) => showError('Withdraw failed', e),
  });

  const sameClaim = selected.length > 0 && selected.every((d) => d.claimId === selected[0].claimId);

  const columns: ColumnsType<Denial> = [
    { title: 'Denied', dataIndex: 'denialDate', render: (v) => fmtDate(v) },
    { title: 'Claim', render: (_, r) => <a onClick={() => setClaimId(r.claimId)}>{r.claimNumber ?? r.claimId}</a> },
    { title: 'Item', render: (_, r) => (r.itemSequence ? `#${r.itemSequence} ${r.itemCode ?? ''}` : 'Header') },
    { title: 'Reason', render: (_, r) => <span title={r.reasonDisplay ?? ''}><Tag>{r.reasonCode}</Tag>{r.reasonDisplay}</span> },
    { title: 'Category', dataIndex: 'category', render: (v) => <Tag>{v}</Tag> },
    { title: 'Payer', dataIndex: 'insurerName' },
    { title: 'Submitted', dataIndex: 'submittedAmount', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Denied', dataIndex: 'deniedAmount', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Status', dataIndex: 'status', render: statusTag },
    {
      title: 'Appeal deadline',
      dataIndex: 'appealDeadline',
      render: (v, r) => (
        <span style={{ color: r.daysToDeadline <= 7 ? '#cf1322' : undefined }}>
          {fmtDate(v)} ({r.daysToDeadline}d)
        </span>
      ),
    },
    {
      title: '',
      render: (_, r) => (
        <Button size="small" disabled={!canManage} onClick={() => { statusForm.setFieldsValue({ status: r.status, notes: r.notes ?? undefined }); setEditing(r); }}>
          Status
        </Button>
      ),
    },
  ];

  const appealColumns: ColumnsType<Appeal> = [
    { title: 'Appeal', dataIndex: 'appealNumber' },
    { title: 'Claim', render: (_, r) => <a onClick={() => setClaimId(r.claimId)}>{r.claimNumber ?? r.claimId}</a> },
    { title: 'Level', dataIndex: 'level', align: 'right' },
    { title: 'Reason codes', dataIndex: 'reasonCodes' },
    { title: 'Reason', dataIndex: 'reason', ellipsis: true },
    { title: 'Appealed', dataIndex: 'appealedAmount', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Approved', dataIndex: 'approvedAmount', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Status', dataIndex: 'status', render: statusTag },
    { title: 'Outcome', dataIndex: 'outcome', render: statusTag },
    { title: 'Submitted', dataIndex: 'submittedDate', render: (v) => fmtDate(v) },
    { title: 'Deadline', dataIndex: 'deadlineDate', render: (v) => fmtDate(v) },
    { title: 'Resubmission', dataIndex: 'resubmissionClaimId', render: (v) => v && <a onClick={() => setClaimId(v)}>open</a> },
    {
      title: '',
      render: (_, r) =>
        ['draft', 'submitted'].includes(r.status.toLowerCase()) && (
          <Button
            size="small"
            danger
            icon={<RollbackOutlined />}
            disabled={!canManage}
            onClick={() => {
              let reason = '';
              Modal.confirm({
                title: `Withdraw appeal ${r.appealNumber}?`,
                content: <Input placeholder="Reason (optional)" onChange={(e) => (reason = e.target.value)} />,
                onOk: () => withdraw.mutateAsync({ id: r.id, reason: reason || undefined }).catch(() => undefined),
              });
            }}
          >
            Withdraw
          </Button>
        ),
    },
  ];

  const s = summary.data;

  return (
    <>
      <PageHeader title="Denials & appeals" extra={<Button icon={<ReloadOutlined />} onClick={refresh} />} />
      <Row gutter={[12, 12]} style={{ marginBottom: 12 }}>
        <Col xs={12} md={4}><Kpi title="Total denials" value={s?.totalDenials} /></Col>
        <Col xs={12} md={4}><Kpi title="Open denials" value={s?.openDenials} /></Col>
        <Col xs={12} md={4}><Kpi title="Denied amount" value={s?.totalDeniedAmount} precision={2} suffix="SAR" /></Col>
        <Col xs={12} md={4}><Kpi title="Open amount" value={s?.openDeniedAmount} precision={2} suffix="SAR" /></Col>
        <Col xs={12} md={4}><Kpi title="Recovered" value={s?.recoveredAmount} precision={2} suffix="SAR" color="#389e0d" /></Col>
        <Col xs={12} md={4}><Kpi title="Deadline ≤ 7 days" value={s?.expiringIn7Days} color={s?.expiringIn7Days ? '#cf1322' : undefined} /></Col>
      </Row>
      <Row gutter={[12, 12]} style={{ marginBottom: 12 }}>
        <Col xs={24} md={12} xl={6}><BreakdownTable title="Category" data={s?.byCategory} /></Col>
        <Col xs={24} md={12} xl={6}><BreakdownTable title="Reason" data={s?.byReason} /></Col>
        <Col xs={24} md={12} xl={6}><BreakdownTable title="Payer" data={s?.byPayer} /></Col>
        <Col xs={24} md={12} xl={6}><BreakdownTable title="Status" data={s?.byStatus} /></Col>
      </Row>
      <Card>
        <Tabs
          items={[
            {
              key: 'denials',
              label: `Denials (${denials.data?.total ?? 0})`,
              children: (
                <>
                  <Space wrap style={{ marginBottom: 12 }}>
                    <Select allowClear placeholder="Category" style={{ width: 170 }} value={category} onChange={setCategory} options={CATEGORIES.map((v) => ({ value: v, label: v }))} />
                    <Select allowClear placeholder="Status" style={{ width: 160 }} value={status} onChange={setStatus} options={DENIAL_STATUSES.map((v) => ({ value: v, label: v }))} />
                    <Input allowClear placeholder="Reason code" style={{ width: 140 }} value={reasonCode} onChange={(e) => setReasonCode(e.target.value)} />
                    <InsurerSelect value={insurerId} onChange={setInsurerId} />
                    <Button
                      type="primary"
                      disabled={!canManage || !sameClaim}
                      title={selected.length && !sameClaim ? 'Select denials of a single claim' : undefined}
                      onClick={() => { appealForm.setFieldsValue({ resubmit: false }); setAppealing(true); }}
                    >
                      Appeal selected ({selected.length})
                    </Button>
                  </Space>
                  <Table<Denial>
                    rowKey="id"
                    size="middle"
                    loading={denials.isFetching}
                    dataSource={denials.data?.items ?? []}
                    columns={columns}
                    scroll={{ x: 'max-content' }}
                    rowSelection={{ selectedRowKeys: selected.map((d) => d.id), onChange: (_, rows) => setSelected(rows), preserveSelectedRowKeys: true }}
                    pagination={{ current: paging.page, pageSize: paging.pageSize, total: denials.data?.total ?? 0, showSizeChanger: true, onChange: (page, pageSize) => setPaging({ page, pageSize }) }}
                  />
                </>
              ),
            },
            {
              key: 'appeals',
              label: `Appeals (${appeals.data?.total ?? 0})`,
              children: (
                <Table<Appeal>
                  rowKey="id"
                  size="middle"
                  loading={appeals.isFetching}
                  dataSource={appeals.data?.items ?? []}
                  columns={appealColumns}
                  scroll={{ x: 'max-content' }}
                  pagination={{ current: appealPaging.page, pageSize: appealPaging.pageSize, total: appeals.data?.total ?? 0, showSizeChanger: true, onChange: (page, pageSize) => setAppealPaging({ page, pageSize }) }}
                />
              ),
            },
          ]}
        />
      </Card>

      <Modal
        open={!!editing}
        title={`Denial ${editing?.reasonCode ?? ''} on ${editing?.claimNumber ?? ''}`}
        okButtonProps={{ loading: updateStatus.isPending }}
        onCancel={() => setEditing(undefined)}
        onOk={() => statusForm.validateFields().then((v) => updateStatus.mutate(v))}
        destroyOnClose
      >
        <Form form={statusForm} layout="vertical">
          <Form.Item name="status" label="Status" rules={[{ required: true }]}>
            <Select options={MANUAL_STATUSES.map((v) => ({ value: v, label: v }))} />
          </Form.Item>
          <Form.Item name="notes" label="Notes">
            <Input.TextArea rows={3} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        open={appealing}
        width={640}
        title={`Appeal ${selected.length} denial(s) on ${selected[0]?.claimNumber ?? ''}`}
        okText="Create appeal"
        okButtonProps={{ loading: createAppeal.isPending }}
        onCancel={() => setAppealing(false)}
        onOk={() => appealForm.validateFields().then((v) => createAppeal.mutate(v))}
        destroyOnClose
      >
        <Form form={appealForm} layout="vertical">
          <Form.Item name="reason" label="Appeal reason" rules={[{ required: true }]}>
            <Input.TextArea rows={3} />
          </Form.Item>
          <Form.Item name="text" label="Message to insurer (sent as NPHIES communication)">
            <Input.TextArea rows={3} />
          </Form.Item>
          <Form.Item name="files" label="Supporting documents" valuePropName="value">
            <AttachmentPicker />
          </Form.Item>
          <Form.Item name="notes" label="Internal notes">
            <Input.TextArea rows={2} />
          </Form.Item>
          <Form.Item name="resubmit" valuePropName="checked">
            <Checkbox>Also resubmit the claim (related claim)</Checkbox>
          </Form.Item>
        </Form>
      </Modal>

      {claimId && <ClaimTimelineDrawer claimId={claimId} onClose={() => setClaimId(undefined)} />}
    </>
  );
}
