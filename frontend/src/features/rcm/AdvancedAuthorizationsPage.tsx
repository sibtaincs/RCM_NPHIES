import { useState } from 'react';
import { Button, Card, Input, Modal, Popconfirm, Select, Space, Table, Tag, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { ReloadOutlined } from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { rcmApi } from '@/api/endpoints/rcm';
import { useGlobalFilters } from '@/hooks/useGlobalFilters';
import { PERMISSIONS, usePermission } from '@/hooks/usePermission';
import type { AdvancedAuthorization } from '@/types/rcm';
import { fmtDate, fmtMoney } from '@/utils/display';
import { ClaimEditorDrawer } from './ClaimEditorDrawer';
import { PageHeader, showError } from './shared';

const STATUSES = ['Active', 'Used', 'Expired', 'Cancelled', 'Superseded'];
const STATUS_COLOR: Record<string, string> = { Active: 'green', Used: 'blue', Expired: 'orange', Cancelled: 'red', Superseded: 'default' };

/** Authorizations the payer issued on its own (received by poll): bill them, change them or ask to cancel them. */
export function AdvancedAuthorizationsPage() {
  const global = useGlobalFilters();
  const { can } = usePermission();
  const qc = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [status, setStatus] = useState<string>();
  const [draftId, setDraftId] = useState<string>();
  const [messageFor, setMessageFor] = useState<AdvancedAuthorization>();
  const [messageText, setMessageText] = useState('');

  const q = { ...global.rcm, status, page, pageSize };
  const list = useQuery({ queryKey: ['rcm', 'advanced-authorizations', q], queryFn: () => rcmApi.advancedAuthorizations.list(q), placeholderData: keepPreviousData });
  const refresh = () => void qc.invalidateQueries({ queryKey: ['rcm', 'advanced-authorizations'] });

  const cancel = useMutation({
    mutationFn: (id: string) => rcmApi.advancedAuthorizations.cancel(id, 'NP'),
    onSuccess: (r) => {
      if (r.claimStatus === 'Cancelled') void message.success('The payer cancelled the authorization');
      else void message.warning(`Cancel request sent, but not confirmed (${r.transactionStatus}${r.errors?.length ? `: ${r.errors.join('; ')}` : ''})`);
      refresh();
    },
    onError: (e) => showError('Could not cancel the authorization', e),
  });

  const amend = useMutation({
    mutationFn: (id: string) => rcmApi.advancedAuthorizations.amend(id),
    onSuccess: (r) => {
      void message.success(`Draft prior authorization ${r.claimNumber} created with all items. Complete it and send it to change the authorization.`);
      setDraftId(r.claimId);
    },
    onError: (e) => showError('Could not start the change', e),
  });

  const sendMessage = useMutation({
    mutationFn: () => rcmApi.communications.send({ advancedAuthorizationId: messageFor!.id, texts: [messageText.trim()], attachments: [] }),
    onSuccess: () => {
      void message.success('Message sent to the payer');
      setMessageFor(undefined);
      setMessageText('');
    },
    onError: (e) => showError('Could not send the message', e),
  });

  const columns: ColumnsType<AdvancedAuthorization> = [
    { title: 'Received', dataIndex: 'receivedAt', render: (v) => fmtDate(v, true) },
    { title: 'Pre-auth ref', dataIndex: 'preAuthRef' },
    { title: 'Patient', render: (_, r) => r.patientName ?? r.patientIdentifier ?? '—' },
    { title: 'Reason', dataIndex: 'reason', render: (v) => v ?? '—' },
    { title: 'Type', render: (_, r) => [r.claimType, r.subType].filter(Boolean).join(' / ') || '—' },
    { title: 'Valid', render: (_, r) => `${fmtDate(r.periodStart)} → ${fmtDate(r.periodEnd)}` },
    { title: 'Approved', dataIndex: 'totalBenefit', align: 'right', render: (v) => fmtMoney(v) },
    {
      title: 'Status',
      dataIndex: 'status',
      render: (v: string, r) => (
        <Space size={4}>
          <Tag color={STATUS_COLOR[v] ?? 'default'}>{v}</Tag>
          {r.reissueReason && <Tag>Reissued: {r.reissueReason}</Tag>}
        </Space>
      ),
    },
    {
      title: '',
      render: (_, r) =>
        r.status === 'Active' ? (
          <Space>
            <Button size="small" disabled={!can(PERMISSIONS.PreAuthCreate)} loading={amend.isPending && amend.variables === r.id} onClick={() => amend.mutate(r.id)}>
              Change
            </Button>
            <Button size="small" disabled={!can(PERMISSIONS.ClaimsUpdate)} onClick={() => setMessageFor(r)}>
              Message payer
            </Button>
            <Popconfirm title="Ask the payer to cancel this authorization?" okText="Send cancel request" onConfirm={() => cancel.mutate(r.id)}>
              <Button size="small" danger disabled={!can(PERMISSIONS.PreAuthSubmit)} loading={cancel.isPending && cancel.variables === r.id}>
                Cancel
              </Button>
            </Popconfirm>
          </Space>
        ) : null,
    },
  ];

  return (
    <>
      <PageHeader title="Advanced authorizations" extra={<Button icon={<ReloadOutlined />} onClick={() => void list.refetch()} />} />
      <Card>
        <Space wrap style={{ marginBottom: 12 }}>
          <Select allowClear placeholder="Status" style={{ width: 180 }} value={status} onChange={(v) => { setStatus(v); setPage(1); }} options={STATUSES.map((v) => ({ value: v, label: v }))} />
        </Space>
        <Table<AdvancedAuthorization>
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
            showTotal: (t) => `${t} authorizations`,
            onChange: (p, s) => {
              setPage(p);
              setPageSize(s);
            },
          }}
        />
      </Card>
      <Modal
        open={!!messageFor}
        title={`Message the payer about authorization ${messageFor?.preAuthRef ?? ''}`}
        okText="Send"
        okButtonProps={{ loading: sendMessage.isPending, disabled: !messageText.trim() }}
        onCancel={() => setMessageFor(undefined)}
        onOk={() => sendMessage.mutate()}
        destroyOnClose
      >
        <Input.TextArea rows={4} value={messageText} onChange={(e) => setMessageText(e.target.value)} placeholder="For example: please extend the authorization period, or add a service." />
      </Modal>
      {draftId && <ClaimEditorDrawer claimId={draftId} use="preauthorization" onClose={() => setDraftId(undefined)} />}
    </>
  );
}
