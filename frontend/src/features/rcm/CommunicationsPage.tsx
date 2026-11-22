import { useState } from 'react';
import { Alert, Button, Card, Col, Descriptions, Form, Input, InputNumber, Modal, Radio, Row, Select, Space, Table, Tabs, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { MessageOutlined, ReloadOutlined, SendOutlined } from '@ant-design/icons';
import type { UploadFile } from 'antd/es/upload/interface';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { rcmApi } from '@/api/endpoints/rcm';
import { AttachmentPicker, toAttachments } from '@/components/AttachmentPicker/AttachmentPicker';
import { useGlobalFilters } from '@/hooks/useGlobalFilters';
import { PERMISSIONS, usePermission } from '@/hooks/usePermission';
import { COMMUNICATION_REASONS } from '@/types/rcm';
import type { Communication, CommunicationRequest } from '@/types/rcm';
import { fmtDate, statusTag } from '@/utils/display';
import { ClaimDocumentPicker } from './ClaimDocumentPicker';
import { ClaimTimelineDrawer } from './ClaimTimelineDrawer';
import { Kpi, PageHeader, showError } from './shared';

interface ComposeValues {
  claimId?: string;
  text?: string;
  files?: UploadFile[];
  documentIds?: string[];
  category?: string;
  priority?: string;
  reasonCode?: string;
  /** The claim service the message is about (extension-ClaimItemSequence). */
  itemSequence?: number;
}

type View = 'open' | 'overdue' | 'all';

const CATEGORIES = ['instruction', 'notification', 'alert', 'reminder'];
const PRIORITIES = ['routine', 'urgent', 'asap', 'stat'];
// Re-adjudication is sent from Denials & appeals, which tracks the appeal and its outcome.
const REASONS = COMMUNICATION_REASONS.filter((r) => r !== 're-adjudication');
const KINDS = [
  { value: 'Claim', label: 'Claim' },
  { value: 'PriorAuthorization', label: 'Prior authorization' },
  { value: 'AdvancedAuthorization', label: 'Advanced authorization' },
];
const KIND_LABEL: Record<string, string> = { Claim: 'Claim', PriorAuthorization: 'Prior auth', AdvancedAuthorization: 'Advanced auth' };

const items = (sequences?: number[]) => (sequences?.length ? sequences.map((n) => <Tag key={n}>#{n}</Tag>) : null);

function hours(h: number): string {
  const a = Math.abs(h);
  return a >= 48 ? `${Math.round(a / 24)}d` : a >= 1 ? `${Math.round(a)}h` : `${Math.max(1, Math.round(a * 60))}m`;
}

/** The due date with the time left, red when overdue and orange when due soon. */
function Due({ r }: { r: CommunicationRequest }) {
  if (!r.dueAt) return <Typography.Text type="secondary">{r.requiresResponse ? '—' : 'no reply needed'}</Typography.Text>;
  return (
    <Space size={4} direction="vertical">
      <span>{fmtDate(r.dueAt, true)}</span>
      {r.isOpen && r.hoursLeft != null && (
        <Tag color={r.isOverdue ? 'error' : r.isDueSoon ? 'warning' : 'default'}>
          {r.isOverdue ? `${hours(r.hoursLeft)} overdue` : `in ${hours(r.hoursLeft)}`}
        </Tag>
      )}
      {r.answeredLate && <Tag color="error">answered late</Tag>}
    </Space>
  );
}

function splitTexts(text?: string): string[] {
  return (text ?? '')
    .split(/\n\s*\n/)
    .map((t) => t.trim())
    .filter(Boolean);
}

function ComposeForm({ form, request }: { form: ReturnType<typeof Form.useForm<ComposeValues>>[0]; request?: CommunicationRequest }) {
  const typedClaimId = Form.useWatch('claimId', form);
  // A reply goes to the latest version of a prior authorization chain; the documents are that claim's.
  const documentsClaim = request ? request.replyClaimId ?? request.claimId : typedClaimId?.trim();
  return (
    <Form form={form} layout="vertical">
      {!request && (
        <Form.Item name="claimId" label="Claim (internal id)" rules={[{ required: true }]}>
          <Input placeholder="Claim the communication relates to" />
        </Form.Item>
      )}
      <Form.Item name="text" label="Message" extra="Separate multiple payload texts with a blank line.">
        <Input.TextArea rows={5} />
      </Form.Item>
      {!request?.advancedAuthorizationId && (
        <Form.Item name="documentIds" label="Stored claim documents" extra="Sent from the document store; no need to upload them again.">
          <ClaimDocumentPicker claimId={documentsClaim} />
        </Form.Item>
      )}
      <Form.Item name="files" label="Upload attachments" valuePropName="value">
        <AttachmentPicker />
      </Form.Item>
      <Form.Item name="itemSequence" label="About service (item sequence)" extra="Optional: links the message and attachments to one service of the claim.">
        <InputNumber min={1} precision={0} style={{ width: 160 }} />
      </Form.Item>
      {!request && (
        <Space style={{ width: '100%' }} styles={{ item: { flex: 1 } }}>
          <Form.Item name="category" label="Category">
            <Select allowClear options={CATEGORIES.map((v) => ({ value: v, label: v }))} />
          </Form.Item>
          <Form.Item name="priority" label="Priority">
            <Select allowClear options={PRIORITIES.map((v) => ({ value: v, label: v }))} />
          </Form.Item>
          <Form.Item name="reasonCode" label="Reason">
            <Select allowClear options={REASONS.map((v) => ({ value: v, label: v }))} />
          </Form.Item>
        </Space>
      )}
    </Form>
  );
}

export function CommunicationsPage() {
  const global = useGlobalFilters();
  const { can } = usePermission();
  const canSend = can(PERMISSIONS.ClaimsSubmit);
  const qc = useQueryClient();
  const [tab, setTab] = useState('requests');
  const [view, setView] = useState<View>('open');
  const [kind, setKind] = useState<string>();
  const [reqPage, setReqPage] = useState({ page: 1, pageSize: 25 });
  const [comPage, setComPage] = useState({ page: 1, pageSize: 25 });
  const [respondTo, setRespondTo] = useState<CommunicationRequest>();
  const [composing, setComposing] = useState(false);
  const [claimId, setClaimId] = useState<string>();
  const [form] = Form.useForm<ComposeValues>();

  const rq = { ...global.rcm, ...reqPage, openOnly: view === 'open', overdueOnly: view === 'overdue', kind };
  const requests = useQuery({ queryKey: ['rcm', 'comm-requests', rq], queryFn: () => rcmApi.communications.requests(rq), placeholderData: keepPreviousData });
  const summary = useQuery({ queryKey: ['rcm', 'comm-request-summary', global.rcm], queryFn: () => rcmApi.communications.requestSummary(global.rcm) });
  const cq = { ...global.rcm, ...comPage };
  const comms = useQuery({ queryKey: ['rcm', 'communications', cq], queryFn: () => rcmApi.communications.list(cq), placeholderData: keepPreviousData });

  const send = useMutation({
    mutationFn: async (v: ComposeValues) => {
      const texts = splitTexts(v.text);
      const seq = v.itemSequence ?? undefined;
      const documents = (v.documentIds ?? []).map((documentId) => ({ documentId, itemSequence: seq }));
      const body = {
        claimId: v.claimId?.trim(),
        texts: seq ? [] : texts,
        items: seq ? texts.map((text) => ({ sequence: seq, text })) : [],
        attachments: (await toAttachments(v.files)).map((a) => ({ ...a, itemSequence: seq })),
        documents,
        category: v.category,
        priority: v.priority,
        reasonCode: v.reasonCode,
      };
      if (texts.length === 0 && body.attachments.length === 0 && documents.length === 0) throw new Error('Enter a message or attach a file.');
      return respondTo ? rcmApi.communications.respond(respondTo.id, body) : rcmApi.communications.send(body);
    },
    onSuccess: (c) => {
      void qc.invalidateQueries({ queryKey: ['rcm'] });
      setRespondTo(undefined);
      setComposing(false);
      form.resetFields();
      (c.errors.length ? Modal.warning : Modal.success)({
        title: `Communication ${c.identifier ?? ''}`,
        content: (
          <>
            <p>Transaction: {statusTag(c.transactionStatus)}</p>
            {c.errors.length > 0 && <ul>{c.errors.map((e) => <li key={e}>{e}</li>)}</ul>}
          </>
        ),
      });
    },
    onError: (e) => showError('Sending failed', e),
  });

  const respond = (r: CommunicationRequest) => {
    form.resetFields();
    form.setFieldsValue({ itemSequence: r.itemSequences.length === 1 ? r.itemSequences[0] : undefined });
    setRespondTo(r);
  };

  const about = (r: CommunicationRequest) => (
    <Space size={4}>
      {r.kind && <Tag>{KIND_LABEL[r.kind] ?? r.kind}</Tag>}
      {r.claimId ? <a onClick={() => setClaimId(r.claimId!)}>{r.claimNumber ?? r.claimIdentifier ?? r.claimId}</a> : r.claimIdentifier}
    </Space>
  );

  const reqColumns: ColumnsType<CommunicationRequest> = [
    { title: 'Due', render: (_, r) => <Due r={r} /> },
    { title: 'Received', dataIndex: 'receivedAt', render: (v) => fmtDate(v, true) },
    { title: 'Payer', dataIndex: 'insurerName' },
    { title: 'Patient', dataIndex: 'patientName' },
    { title: 'About', render: (_, r) => about(r) },
    { title: 'Services', dataIndex: 'itemSequences', render: items },
    { title: 'Request', dataIndex: 'payloadText', render: (v) => <Typography.Paragraph ellipsis={{ rows: 2, expandable: true }} style={{ margin: 0, maxWidth: 380 }}>{v}</Typography.Paragraph> },
    { title: 'Category', render: (_, r) => (r.requiresResponse ? r.category : <Tag color="blue" title="Informs only; no reply needed">{r.category}</Tag>) },
    { title: 'Reason', dataIndex: 'reason' },
    { title: 'Priority', dataIndex: 'priority' },
    { title: 'Identifier', dataIndex: 'identifier' },
    { title: 'Status', render: (_, r) => statusTag(r.isOpen ? 'open' : r.requiresResponse ? r.status : 'informational') },
    { title: 'Responded', dataIndex: 'respondedAt', render: (v) => fmtDate(v, true) },
    {
      title: '',
      fixed: 'right',
      render: (_, r) =>
        r.isOpen && (
          <Button size="small" type="primary" icon={<MessageOutlined />} disabled={!canSend} onClick={() => respond(r)}>
            Respond
          </Button>
        ),
    },
  ];

  const comColumns: ColumnsType<Communication> = [
    { title: 'Created', dataIndex: 'createdAt', render: (v) => fmtDate(v, true) },
    { title: 'Identifier', dataIndex: 'identifier' },
    { title: 'Direction', dataIndex: 'direction' },
    { title: 'Claim', dataIndex: 'claimId', render: (v) => v && <a onClick={() => setClaimId(v)}>open</a> },
    { title: 'Solicited', render: (_, r) => (r.communicationRequestId ? 'Yes' : 'No') },
    { title: 'Reason', dataIndex: 'reasonCode' },
    { title: 'Services', dataIndex: 'itemSequences', render: items },
    { title: 'Message', dataIndex: 'payloadText', render: (v) => <Typography.Paragraph ellipsis={{ rows: 2, expandable: true }} style={{ margin: 0, maxWidth: 420 }}>{v}</Typography.Paragraph> },
    { title: 'Status', dataIndex: 'status', render: statusTag },
    { title: 'Processing', dataIndex: 'processingStatus', render: statusTag },
    { title: 'Sent', dataIndex: 'sentAt', render: (v) => fmtDate(v, true) },
  ];

  const refresh = () => void qc.invalidateQueries({ queryKey: ['rcm'] });
  const s = summary.data;
  const otherVersion = respondTo?.replyClaimId && respondTo.replyClaimId !== respondTo.claimId;

  return (
    <>
      <PageHeader
        title="Communications"
        extra={
          <Space>
            <Button type="primary" icon={<SendOutlined />} disabled={!canSend} onClick={() => { form.resetFields(); setComposing(true); }}>
              New communication
            </Button>
            <Button icon={<ReloadOutlined />} onClick={refresh} />
          </Space>
        }
      />
      <Row gutter={[12, 12]} style={{ marginBottom: 12 }}>
        <Col xs={12} md={4}><Kpi title="Waiting for a reply" value={s?.open} /></Col>
        <Col xs={12} md={4}><Kpi title="Overdue" value={s?.overdue} color={s?.overdue ? '#cf1322' : undefined} /></Col>
        <Col xs={12} md={4}><Kpi title={`Due within ${s?.dueSoonHours ?? 8}h`} value={s?.dueSoon} color={s?.dueSoon ? '#d46b08' : undefined} /></Col>
        <Col xs={12} md={4}><Kpi title="Answered" value={s?.answered} /></Col>
        <Col xs={12} md={4}><Kpi title="Answered late" value={s?.answeredLate} color={s?.answeredLate ? '#cf1322' : undefined} /></Col>
        <Col xs={12} md={4}><Kpi title="Average reply (hours)" value={s?.averageResponseHours ?? undefined} precision={1} /></Col>
      </Row>
      <Card>
        <Tabs
          activeKey={tab}
          onChange={setTab}
          items={[
            {
              key: 'requests',
              label: `Insurer requests (${requests.data?.total ?? 0})`,
              children: (
                <>
                  <Space wrap style={{ marginBottom: 12 }}>
                    <Radio.Group
                      optionType="button"
                      value={view}
                      onChange={(e) => { setView(e.target.value as View); setReqPage((p) => ({ ...p, page: 1 })); }}
                      options={[
                        { value: 'open', label: 'Waiting for a reply' },
                        { value: 'overdue', label: 'Overdue' },
                        { value: 'all', label: 'All' },
                      ]}
                    />
                    <Select allowClear placeholder="About" style={{ width: 200 }} value={kind} onChange={(v) => { setKind(v); setReqPage((p) => ({ ...p, page: 1 })); }} options={KINDS} />
                  </Space>
                  <Table<CommunicationRequest>
                    rowKey="id"
                    size="middle"
                    loading={requests.isFetching}
                    dataSource={requests.data?.items ?? []}
                    columns={reqColumns}
                    scroll={{ x: 'max-content' }}
                    pagination={{ current: reqPage.page, pageSize: reqPage.pageSize, total: requests.data?.total ?? 0, showSizeChanger: true, onChange: (page, pageSize) => setReqPage({ page, pageSize }) }}
                  />
                </>
              ),
            },
            {
              key: 'communications',
              label: `Communications (${comms.data?.total ?? 0})`,
              children: (
                <Table<Communication>
                  rowKey="id"
                  size="middle"
                  loading={comms.isFetching}
                  dataSource={comms.data?.items ?? []}
                  columns={comColumns}
                  scroll={{ x: 'max-content' }}
                  pagination={{ current: comPage.page, pageSize: comPage.pageSize, total: comms.data?.total ?? 0, showSizeChanger: true, onChange: (page, pageSize) => setComPage({ page, pageSize }) }}
                />
              ),
            },
          ]}
        />
      </Card>

      <Modal
        open={!!respondTo || composing}
        width={680}
        title={respondTo ? `Respond to ${respondTo.identifier ?? 'request'}` : 'New unsolicited communication'}
        okText="Send to NPHIES"
        okButtonProps={{ loading: send.isPending }}
        onCancel={() => { setRespondTo(undefined); setComposing(false); }}
        onOk={() => form.validateFields().then((v) => send.mutate(v))}
        destroyOnClose
      >
        {respondTo && (
          <Card size="small" style={{ marginBottom: 12 }} title="Insurer request">
            <Descriptions size="small" column={2} style={{ marginBottom: 8 }}>
              <Descriptions.Item label="About">{about(respondTo)}</Descriptions.Item>
              <Descriptions.Item label="Due"><Due r={respondTo} /></Descriptions.Item>
              {respondTo.itemSequences.length > 0 && <Descriptions.Item label="Services">{items(respondTo.itemSequences)}</Descriptions.Item>}
              {respondTo.patientName && <Descriptions.Item label="Patient">{respondTo.patientName}</Descriptions.Item>}
            </Descriptions>
            {respondTo.payloadText && <Typography.Paragraph style={{ margin: 0, whiteSpace: 'pre-wrap' }}>{respondTo.payloadText}</Typography.Paragraph>}
          </Card>
        )}
        {otherVersion && (
          <Alert
            type="info"
            showIcon
            style={{ marginBottom: 12 }}
            message={`The reply is sent about ${respondTo!.replyClaimNumber ?? 'the latest version'}, the latest version of this prior authorization.`}
          />
        )}
        <ComposeForm form={form} request={respondTo} />
      </Modal>

      {claimId && <ClaimTimelineDrawer claimId={claimId} onClose={() => setClaimId(undefined)} />}
    </>
  );
}
