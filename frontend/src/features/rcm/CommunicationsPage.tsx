import { useState } from 'react';
import { Button, Card, Form, Input, Modal, Select, Space, Switch, Table, Tabs, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { MessageOutlined, ReloadOutlined, SendOutlined } from '@ant-design/icons';
import type { UploadFile } from 'antd/es/upload/interface';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { rcmApi } from '@/api/endpoints/rcm';
import { AttachmentPicker, toAttachments } from '@/components/AttachmentPicker/AttachmentPicker';
import { useGlobalFilters } from '@/hooks/useGlobalFilters';
import { PERMISSIONS, usePermission } from '@/hooks/usePermission';
import type { Communication, CommunicationRequest } from '@/types/rcm';
import { fmtDate, statusTag } from '@/utils/display';
import { ClaimTimelineDrawer } from './ClaimTimelineDrawer';
import { PageHeader, showError } from './shared';

interface ComposeValues {
  claimId?: string;
  text?: string;
  files?: UploadFile[];
  category?: string;
  priority?: string;
  reasonCode?: string;
}

const CATEGORIES = ['instruction', 'notification', 'alert', 'reminder'];
const PRIORITIES = ['routine', 'urgent', 'asap', 'stat'];

function splitTexts(text?: string): string[] {
  return (text ?? '')
    .split(/\n\s*\n/)
    .map((t) => t.trim())
    .filter(Boolean);
}

function ComposeForm({ form, withClaim }: { form: ReturnType<typeof Form.useForm<ComposeValues>>[0]; withClaim: boolean }) {
  return (
    <Form form={form} layout="vertical">
      {withClaim && (
        <Form.Item name="claimId" label="Claim (internal id)" rules={[{ required: true }]}>
          <Input placeholder="Claim the communication relates to" />
        </Form.Item>
      )}
      <Form.Item name="text" label="Message" extra="Separate multiple payload texts with a blank line.">
        <Input.TextArea rows={5} />
      </Form.Item>
      <Form.Item name="files" label="Attachments" valuePropName="value">
        <AttachmentPicker />
      </Form.Item>
      {withClaim && (
        <Space style={{ width: '100%' }} styles={{ item: { flex: 1 } }}>
          <Form.Item name="category" label="Category">
            <Select allowClear options={CATEGORIES.map((v) => ({ value: v, label: v }))} />
          </Form.Item>
          <Form.Item name="priority" label="Priority">
            <Select allowClear options={PRIORITIES.map((v) => ({ value: v, label: v }))} />
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
  const [openOnly, setOpenOnly] = useState(true);
  const [reqPage, setReqPage] = useState({ page: 1, pageSize: 25 });
  const [comPage, setComPage] = useState({ page: 1, pageSize: 25 });
  const [respondTo, setRespondTo] = useState<CommunicationRequest>();
  const [composing, setComposing] = useState(false);
  const [claimId, setClaimId] = useState<string>();
  const [form] = Form.useForm<ComposeValues>();

  const rq = { ...global.rcm, ...reqPage, openOnly };
  const requests = useQuery({ queryKey: ['rcm', 'comm-requests', rq], queryFn: () => rcmApi.communications.requests(rq), placeholderData: keepPreviousData });
  const cq = { ...global.rcm, ...comPage };
  const comms = useQuery({ queryKey: ['rcm', 'communications', cq], queryFn: () => rcmApi.communications.list(cq), placeholderData: keepPreviousData });

  const send = useMutation({
    mutationFn: async (v: ComposeValues) => {
      const body = {
        claimId: v.claimId,
        texts: splitTexts(v.text),
        attachments: await toAttachments(v.files),
        category: v.category,
        priority: v.priority,
        reasonCode: v.reasonCode,
      };
      if (body.texts.length === 0 && body.attachments.length === 0) throw new Error('Enter a message or attach a file.');
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

  const reqColumns: ColumnsType<CommunicationRequest> = [
    { title: 'Received', dataIndex: 'receivedAt', render: (v) => fmtDate(v, true) },
    { title: 'Identifier', dataIndex: 'identifier' },
    { title: 'Claim', render: (_, r) => (r.claimId ? <a onClick={() => setClaimId(r.claimId!)}>{r.claimIdentifier ?? r.claimId}</a> : r.claimIdentifier) },
    { title: 'Category', dataIndex: 'category' },
    { title: 'Priority', dataIndex: 'priority' },
    { title: 'Request', dataIndex: 'payloadText', render: (v) => <Typography.Paragraph ellipsis={{ rows: 2, expandable: true }} style={{ margin: 0, maxWidth: 420 }}>{v}</Typography.Paragraph> },
    { title: 'Status', render: (_, r) => statusTag(r.isOpen ? 'open' : r.status) },
    { title: 'Responded', dataIndex: 'respondedAt', render: (v) => fmtDate(v, true) },
    {
      title: '',
      render: (_, r) =>
        r.isOpen && (
          <Button size="small" type="primary" icon={<MessageOutlined />} disabled={!canSend} onClick={() => { form.resetFields(); setRespondTo(r); }}>
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
    { title: 'Message', dataIndex: 'payloadText', render: (v) => <Typography.Paragraph ellipsis={{ rows: 2, expandable: true }} style={{ margin: 0, maxWidth: 420 }}>{v}</Typography.Paragraph> },
    { title: 'Status', dataIndex: 'status', render: statusTag },
    { title: 'Processing', dataIndex: 'processingStatus', render: statusTag },
    { title: 'Sent', dataIndex: 'sentAt', render: (v) => fmtDate(v, true) },
  ];

  const refresh = () => {
    void requests.refetch();
    void comms.refetch();
  };

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
                  <Space style={{ marginBottom: 12 }}>
                    <Switch checked={openOnly} onChange={(v) => { setOpenOnly(v); setReqPage((p) => ({ ...p, page: 1 })); }} />
                    Open only
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
        width={640}
        title={respondTo ? `Respond to ${respondTo.identifier ?? 'request'}` : 'New unsolicited communication'}
        okText="Send to NPHIES"
        okButtonProps={{ loading: send.isPending }}
        onCancel={() => { setRespondTo(undefined); setComposing(false); }}
        onOk={() => form.validateFields().then((v) => send.mutate(v))}
        destroyOnClose
      >
        {respondTo?.payloadText && (
          <Card size="small" style={{ marginBottom: 12 }} title="Insurer request">
            <Typography.Paragraph style={{ margin: 0, whiteSpace: 'pre-wrap' }}>{respondTo.payloadText}</Typography.Paragraph>
          </Card>
        )}
        <ComposeForm form={form} withClaim={!respondTo} />
      </Modal>

      {claimId && <ClaimTimelineDrawer claimId={claimId} onClose={() => setClaimId(undefined)} />}
    </>
  );
}
