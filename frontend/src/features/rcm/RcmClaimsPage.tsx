import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Button,
  Card,
  Checkbox,
  DatePicker,
  Dropdown,
  Form,
  Input,
  List,
  Modal,
  Select,
  Space,
  Spin,
  Table,
  Tag,
  Tooltip,
  Typography,
  message,
} from 'antd';
import type { MenuProps } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { EditOutlined, MoreOutlined, PaperClipOutlined, PlusOutlined, ReloadOutlined, SendOutlined, StarFilled, SyncOutlined } from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { rcmApi } from '@/api/endpoints/rcm';
import { useGlobalFilters } from '@/hooks/useGlobalFilters';
import { useDebounce } from '@/hooks/useDebounce';
import { PERMISSIONS, usePermission } from '@/hooks/usePermission';
import type { RcmClaimSummary, SubmissionResult, TaskResult } from '@/types/rcm';
import type { ClaimCorrectionOption } from '@/types/claimEditor';
import { fmtDate, fmtMoney, statusTag } from '@/utils/display';
import { ClaimTimelineDrawer } from './ClaimTimelineDrawer';
import { ClaimEditorDrawer } from './ClaimEditorDrawer';
import { ClaimDocumentsTab } from './ClaimDocumentsTab';
import { BundlePreviewModal } from './JsonViewerModal';
import { InsurerSelect, PageHeader, showError, showOperationResult } from './shared';

type Mode = 'claim' | 'preauth';

const CLAIM_TYPES = ['institutional', 'oral', 'pharmacy', 'professional', 'vision'];
/** Mirrors RcmClaimStatus. */
const NPHIES_STATUSES = ['Draft', 'Submitted', 'Queued', 'Pended', 'Approved', 'PartiallyApproved', 'Rejected', 'Error', 'ValidationFailed', 'Timeout', 'CancelRequested', 'Cancelled', 'Nullified', 'Paid', 'PartiallyPaid', 'Superseded'];
const CANCEL_REASONS = [
  { value: 'WI', label: 'WI - Wrong information' },
  { value: 'NP', label: 'NP - Service not performed' },
  { value: 'TAS', label: 'TAS - Transaction already submitted' },
  { value: 'SU', label: 'SU - Service unavailable' },
  { value: 'resubmission', label: 'Resubmission' },
];

type ActionKind = 'submit' | 'resubmit' | 'update' | 'extend' | 'toClaim' | 'link' | 'status' | 'cancel';

/** Claims or prior-authorizations: list + NPHIES actions + batch submit + master-detail timeline. */
export function RcmClaimsPage({ mode }: { mode: Mode }) {
  const isClaim = mode === 'claim';
  const global = useGlobalFilters();
  const { can } = usePermission();
  const qc = useQueryClient();
  const canSubmit = can(isClaim ? PERMISSIONS.ClaimsSubmit : PERMISSIONS.PreAuthSubmit);

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [search, setSearch] = useState('');
  const [claimType, setClaimType] = useState<string>();
  const [nphiesStatus, setNphiesStatus] = useState<string>();
  const [insurerId, setInsurerId] = useState<string>();
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [timelineId, setTimelineId] = useState<string>();
  const [action, setAction] = useState<{ kind: ActionKind; claim: RcmClaimSummary }>();
  const [editor, setEditor] = useState<{ id?: string }>();
  const [correcting, setCorrecting] = useState<RcmClaimSummary>();
  const [previewId, setPreviewId] = useState<string>();
  const [docsFor, setDocsFor] = useState<RcmClaimSummary>();
  const navigate = useNavigate();
  const [form] = Form.useForm();
  const term = useDebounce(search);

  const query = {
    ...global.rcm,
    insurerId,
    claimType,
    nphiesStatus,
    search: term || undefined,
    use: isClaim ? 'claim' : undefined,
    page,
    pageSize,
  };
  const list = useQuery({
    queryKey: ['rcm', mode, query],
    queryFn: () => (isClaim ? rcmApi.claims.list(query) : rcmApi.preauths.list(query)),
    placeholderData: keepPreviousData,
  });

  const invalidate = () => {
    void qc.invalidateQueries({ queryKey: ['rcm'] });
    void qc.invalidateQueries({ queryKey: ['claim-timeline'] });
  };

  const run = useMutation({
    mutationFn: async ({ kind, claim, values }: { kind: ActionKind; claim: RcmClaimSummary; values: Record<string, unknown> }) => {
      const derived = {
        submit: values.submit !== false,
        reason: values.reason as string | undefined,
        newPeriodEnd: values.newPeriodEnd ? (values.newPeriodEnd as { format: (f: string) => string }).format('YYYY-MM-DD') : undefined,
      };
      const api = isClaim ? rcmApi.claims : rcmApi.preauths;
      switch (kind) {
        case 'submit':
          return api.submit(claim.id);
        case 'resubmit':
          return rcmApi.claims.resubmit(claim.id, derived);
        case 'update':
          return rcmApi.preauths.update(claim.id, derived);
        case 'extend':
          return rcmApi.preauths.extend(claim.id, derived);
        case 'toClaim':
          return rcmApi.preauths.toClaim(claim.id, derived);
        case 'status':
          return api.statusCheck(claim.id);
        case 'cancel':
          return api.cancel(claim.id, { reason: String(values.reason ?? 'WI'), nullify: !!values.nullify });
        case 'link':
          await rcmApi.claims.linkPreAuth(claim.id, {
            preAuthClaimId: (values.preAuthClaimId as string) || undefined,
            preAuthRef: (values.preAuthRef as string) || undefined,
          });
          return null;
      }
    },
    onSuccess: (result, { kind, claim, values }) => {
      invalidate();
      setAction(undefined);
      form.resetFields();
      const derivedDraft = values.submit === false && ['resubmit', 'update', 'extend', 'toClaim'].includes(kind);
      if (derivedDraft && result && 'claimId' in result) {
        void message.success(`Editable copy ${(result as SubmissionResult).claimNumber} created — correct it and submit.`);
        setEditor({ id: (result as SubmissionResult).claimId });
        return;
      }
      if (result) showOperationResult(`${claim.claimNumber}: ${kind}`, result as SubmissionResult | TaskResult);
      else void message.success('Pre-authorization linked');
    },
    onError: (e, { kind }) => showError(`${kind} failed`, e),
  });

  const batch = useMutation({
    mutationFn: (batchIdentifier?: string) => rcmApi.batches.submit({ claimIds: selectedIds, batchIdentifier }),
    onSuccess: (r) => {
      invalidate();
      setSelectedIds([]);
      Modal.info({
        title: `Batch ${r.batchIdentifier}`,
        content: (
          <>
            <p>
              {r.claimCount} claims · {statusTag(r.transactionStatus)}
            </p>
            {r.errors.length > 0 && <ul>{r.errors.map((e) => <li key={e}>{e}</li>)}</ul>}
          </>
        ),
      });
    },
    onError: (e) => showError('Batch submission failed', e),
  });

  const startAction = (kind: ActionKind, claim: RcmClaimSummary) => {
    if (kind === 'submit' || kind === 'status') {
      Modal.confirm({
        title: kind === 'submit' ? `Send ${claim.claimNumber} to NPHIES?` : `Check status of ${claim.claimNumber}?`,
        onOk: () => run.mutateAsync({ kind, claim, values: {} }).catch(() => undefined),
      });
      return;
    }
    form.resetFields();
    form.setFieldsValue({ submit: true, reason: kind === 'cancel' ? 'WI' : undefined, nullify: false });
    setAction({ kind, claim });
  };

  const corrections = useQuery({
    queryKey: ['claim-corrections', correcting?.id],
    queryFn: () => rcmApi.claims.corrections(correcting!.id),
    enabled: !!correcting,
    gcTime: 0,
  });

  /** Correction option → existing action. Derived actions default to "don't send" so the copy opens in the editor. */
  const applyCorrection = (o: ClaimCorrectionOption, claim: RcmClaimSummary) => {
    setCorrecting(undefined);
    const derived = (kind: ActionKind) => {
      form.resetFields();
      form.setFieldsValue({ submit: false });
      setAction({ kind, claim });
    };
    switch (o.action) {
      case 'edit':
        return setEditor({ id: claim.id });
      case 'preview':
        return setPreviewId(claim.id);
      case 'status-check':
        return startAction('status', claim);
      case 'cancel':
      case 'nullify':
        startAction('cancel', claim);
        return form.setFieldsValue({ nullify: o.action === 'nullify' });
      case 'resubmit':
        return derived('resubmit');
      case 'update':
        return derived('update');
      case 'extend':
        return derived('extend');
      case 'create-claim':
        return derived('toClaim');
      case 'communicate':
        return navigate('/rcm/communications');
      case 'appeal':
        return navigate('/rcm/denials');
    }
  };

  const menu = (r: RcmClaimSummary): MenuProps['items'] => {
    const editable = ['Draft', 'Error', 'ValidationFailed'].includes(r.nphiesStatus ?? 'Draft');
    const items: MenuProps['items'] = [
      { key: 'timeline', label: 'Open details / timeline' },
      { key: 'edit', label: editable ? 'Edit' : 'View in editor', icon: <EditOutlined /> },
      { key: 'correct', label: 'Correct… (guided options)' },
      { key: 'documents', label: editable ? 'Documents / attach' : 'Documents', icon: <PaperClipOutlined /> },
      { type: 'divider' },
      { key: 'submit', label: 'Submit to NPHIES', disabled: !canSubmit },
    ];
    if (isClaim) {
      items.push(
        { key: 'resubmit', label: 'Resubmit (related)', disabled: !canSubmit },
        { key: 'link', label: 'Link pre-authorization', disabled: !can(PERMISSIONS.ClaimsUpdate) },
      );
    } else {
      items.push(
        { key: 'update', label: 'Update pre-auth', disabled: !canSubmit },
        { key: 'extend', label: 'Extend validity', disabled: !canSubmit },
        { key: 'toClaim', label: 'Create claim from pre-auth', disabled: !can(PERMISSIONS.ClaimsCreate) },
      );
    }
    items.push(
      { key: 'status', label: 'Status check', disabled: !canSubmit },
      { key: 'cancel', label: 'Cancel / nullify', danger: true, disabled: !canSubmit },
    );
    return items;
  };

  const columns: ColumnsType<RcmClaimSummary> = [
    { title: 'Number', dataIndex: 'claimNumber', fixed: 'left', render: (v, r) => <a onClick={() => setTimelineId(r.id)}>{v}</a> },
    { title: 'Created', dataIndex: 'createdDate', render: (v) => fmtDate(v) },
    {
      title: 'Patient',
      render: (_, r) => (
        <>
          {r.patientName ?? r.patientId}
          {r.patientNameArabic && <div dir="rtl" style={{ fontSize: 12, color: '#888' }}>{r.patientNameArabic}</div>}
        </>
      ),
    },
    { title: 'Insurer', dataIndex: 'insurerName' },
    {
      title: 'Type',
      render: (_, r) => (
        <Space size={4}>
          <Tag>{r.claimType}</Tag>
          {r.claimSubType && <Tag>{r.claimSubType}</Tag>}
          {r.isShadowBilling && <Tag color="purple">shadow</Tag>}
        </Space>
      ),
    },
    { title: 'Status', dataIndex: 'status', render: statusTag },
    { title: 'NPHIES', dataIndex: 'nphiesStatus', render: statusTag },
    { title: 'Outcome', render: (_, r) => statusTag(r.adjudicationOutcome ?? r.outcome) },
    { title: 'Total', dataIndex: 'total', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Approved', dataIndex: 'approvedAmount', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'VAT', dataIndex: 'approvedTax', align: 'right', render: (v) => fmtMoney(v) },
    { title: 'Patient share', dataIndex: 'patientShareAmount', align: 'right', render: (v) => fmtMoney(v) },
    ...(isClaim
      ? [
          { title: 'Expected', dataIndex: 'expectedPayment', align: 'right' as const, render: (v: number) => fmtMoney(v) },
          { title: 'Paid', dataIndex: 'paidAmount', align: 'right' as const, render: (v: number) => fmtMoney(v) },
          {
            title: 'Variance',
            dataIndex: 'paymentVariance',
            align: 'right' as const,
            render: (v: number | null) =>
              v == null ? '—' : (
                <Tooltip title={v > 0 ? 'Underpaid vs approved + VAT' : v < 0 ? 'Overpaid' : 'Fully paid'}>
                  <span style={{ color: v > 0.009 ? '#cf1322' : v < -0.009 ? '#d48806' : undefined }}>{fmtMoney(v)}</span>
                </Tooltip>
              ),
          },
          { title: 'PreAuth', dataIndex: 'preAuthRef' },
          { title: 'Batch', dataIndex: 'batchIdentifier' },
        ]
      : [
          { title: 'PreAuth ref', dataIndex: 'preAuthRef' },
          { title: 'Valid until', dataIndex: 'preAuthPeriodEnd', render: (v: string) => fmtDate(v) },
        ]),
    {
      title: 'Docs',
      dataIndex: 'documentCount',
      align: 'center',
      width: 70,
      render: (v: number | null | undefined, r) => {
        const missing = !v && ['Draft', 'Error', 'ValidationFailed'].includes(r.nphiesStatus ?? 'Draft');
        return (
          <Tooltip title={missing ? 'No documents attached - click to attach' : `${v ?? 0} document(s)`}>
            <Button size="small" type="text" danger={missing} icon={<PaperClipOutlined />} onClick={() => setDocsFor(r)}>
              {v ?? 0}
            </Button>
          </Tooltip>
        );
      },
    },
    { title: 'Submitted', dataIndex: 'submittedAt', render: (v) => fmtDate(v, true) },
    {
      title: '',
      fixed: 'right',
      width: 48,
      render: (_, r) => (
        <Dropdown
          trigger={['click']}
          menu={{
            items: menu(r),
            onClick: ({ key }) => {
              if (key === 'timeline') return setTimelineId(r.id);
              if (key === 'edit') return setEditor({ id: r.id });
              if (key === 'correct') return setCorrecting(r);
              if (key === 'documents') return setDocsFor(r);
              startAction(key as ActionKind, r);
            },
          }}
        >
          <Button type="text" icon={<MoreOutlined />} />
        </Dropdown>
      ),
    },
  ];

  const actionTitle: Record<ActionKind, string> = {
    submit: 'Submit',
    status: 'Status check',
    resubmit: 'Resubmit claim',
    update: 'Update pre-authorization',
    extend: 'Extend pre-authorization',
    toClaim: 'Create claim from pre-authorization',
    link: 'Link pre-authorization',
    cancel: 'Cancel / nullify',
  };

  return (
    <>
      <PageHeader
        title={isClaim ? 'Claims' : 'Prior authorizations'}
        extra={
          <Space wrap>
            {can(PERMISSIONS.ClaimsCreate) && (
              <Button icon={<PlusOutlined />} onClick={() => setEditor({})}>
                {isClaim ? 'New claim' : 'New pre-auth'}
              </Button>
            )}
            {isClaim && (
              <Tooltip title="Submit selected claims as one NPHIES batch">
                <Button
                  type="primary"
                  icon={<SendOutlined />}
                  disabled={!canSubmit || selectedIds.length === 0}
                  loading={batch.isPending}
                  onClick={() =>
                    Modal.confirm({
                      title: `Submit ${selectedIds.length} claims as a batch?`,
                      onOk: () => batch.mutateAsync(undefined).catch(() => undefined),
                    })
                  }
                >
                  Batch submit ({selectedIds.length})
                </Button>
              </Tooltip>
            )}
            <Button icon={<ReloadOutlined />} onClick={() => void list.refetch()} />
          </Space>
        }
      />
      <Card>
        <Space wrap style={{ marginBottom: 12 }}>
          <Input.Search
            allowClear
            placeholder="Claim no. / identifier / patient"
            style={{ width: 260 }}
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
          <Select allowClear placeholder="Claim type" style={{ width: 160 }} value={claimType} onChange={setClaimType} options={CLAIM_TYPES.map((v) => ({ value: v, label: v }))} />
          <Select allowClear placeholder="NPHIES status" style={{ width: 160 }} value={nphiesStatus} onChange={setNphiesStatus} options={NPHIES_STATUSES.map((v) => ({ value: v, label: v }))} />
          <InsurerSelect value={insurerId} onChange={setInsurerId} />
        </Space>
        <Table<RcmClaimSummary>
          rowKey="id"
          size="middle"
          loading={list.isFetching}
          dataSource={list.data?.items ?? []}
          columns={columns}
          scroll={{ x: 'max-content' }}
          rowSelection={isClaim ? { selectedRowKeys: selectedIds, onChange: (k) => setSelectedIds(k as string[]), preserveSelectedRowKeys: true } : undefined}
          pagination={{
            current: page,
            pageSize,
            total: list.data?.total ?? 0,
            showSizeChanger: true,
            showTotal: (t) => `${t} records`,
            onChange: (p, s) => {
              setPage(p);
              setPageSize(s);
            },
          }}
        />
      </Card>

      <Modal
        open={!!action}
        title={action ? `${actionTitle[action.kind]} — ${action.claim.claimNumber}` : ''}
        okText={action?.kind === 'link' ? 'Link' : 'Send'}
        okButtonProps={{ icon: <SyncOutlined />, loading: run.isPending }}
        onCancel={() => setAction(undefined)}
        onOk={() => form.validateFields().then((values) => action && run.mutate({ ...action, values }))}
        destroyOnClose
      >
        <Form form={form} layout="vertical">
          {action?.kind === 'cancel' && (
            <>
              <Form.Item name="reason" label="Reason" rules={[{ required: true }]}>
                <Select options={CANCEL_REASONS} />
              </Form.Item>
              <Form.Item name="nullify" valuePropName="checked">
                <Checkbox>Nullify (claim was already paid/adjudicated)</Checkbox>
              </Form.Item>
            </>
          )}
          {action?.kind === 'link' && (
            <>
              <Form.Item name="preAuthClaimId" label="Pre-authorization (internal id)">
                <Input placeholder="Id of an approved pre-auth in this system" />
              </Form.Item>
              <Form.Item name="preAuthRef" label="or NPHIES pre-auth reference">
                <Input placeholder="preAuthRef from the insurer" />
              </Form.Item>
            </>
          )}
          {(action?.kind === 'resubmit' || action?.kind === 'update' || action?.kind === 'extend' || action?.kind === 'toClaim') && (
            <>
              <Form.Item name="reason" label="Reason / note">
                <Input.TextArea rows={2} />
              </Form.Item>
              {action.kind === 'extend' && (
                <Form.Item name="newPeriodEnd" label="New validity end" rules={[{ required: true }]}>
                  <DatePicker style={{ width: '100%' }} />
                </Form.Item>
              )}
              <Form.Item name="submit" valuePropName="checked">
                <Checkbox>Send to NPHIES immediately</Checkbox>
              </Form.Item>
            </>
          )}
        </Form>
      </Modal>

      {timelineId && (
        <ClaimTimelineDrawer
          claimId={timelineId}
          onClose={() => setTimelineId(undefined)}
          onEdit={(id) => {
            setTimelineId(undefined);
            setEditor({ id });
          }}
        />
      )}
      {editor && (
        <ClaimEditorDrawer
          claimId={editor.id}
          use={isClaim ? 'claim' : 'preauthorization'}
          defaultProviderId={global.rcm.providerId}
          onClose={() => setEditor(undefined)}
          onSaved={invalidate}
        />
      )}
      {previewId && <BundlePreviewModal claimId={previewId} onClose={() => setPreviewId(undefined)} />}
      <Modal
        open={!!docsFor}
        width={1000}
        title={docsFor ? `Documents - ${docsFor.claimNumber}${docsFor.patientName ? ` · ${docsFor.patientName}` : ''}` : ''}
        footer={null}
        onCancel={() => {
          setDocsFor(undefined);
          invalidate();
        }}
        destroyOnClose
      >
        {docsFor && <ClaimDocumentsTab claimId={docsFor.id} nphiesStatus={docsFor.nphiesStatus} />}
      </Modal>
      <Modal
        open={!!correcting}
        title={correcting ? `Correct ${correcting.claimNumber}` : ''}
        footer={null}
        onCancel={() => setCorrecting(undefined)}
        destroyOnClose
      >
        {corrections.isLoading && <Spin />}
        {corrections.data && (
          <>
            <Space style={{ marginBottom: 8 }}>
              {statusTag(corrections.data.nphiesStatus)}
              {corrections.data.editable ? <Tag color="green">editable</Tag> : <Tag>locked</Tag>}
            </Space>
            <Typography.Paragraph type="secondary">{corrections.data.guidance}</Typography.Paragraph>
            <List
              size="small"
              bordered
              dataSource={corrections.data.options}
              renderItem={(o) => (
                <List.Item
                  actions={[
                    <Button key="go" size="small" type={o.recommended ? 'primary' : 'default'} onClick={() => correcting && applyCorrection(o, correcting)}>
                      Go
                    </Button>,
                  ]}
                >
                  <List.Item.Meta
                    title={
                      <Space>
                        {o.label}
                        {o.recommended && <StarFilled style={{ color: '#faad14' }} />}
                      </Space>
                    }
                    description={o.description}
                  />
                </List.Item>
              )}
            />
          </>
        )}
      </Modal>
    </>
  );
}
