import { useState } from 'react';
import { Alert, Button, Card, Checkbox, DatePicker, Descriptions, Drawer, Form, Modal, Select, Space, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { PlusOutlined, ReloadOutlined } from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { Dayjs } from 'dayjs';
import { rcmApi } from '@/api/endpoints/rcm';
import { useGlobalFilters } from '@/hooks/useGlobalFilters';
import { PERMISSIONS, usePermission } from '@/hooks/usePermission';
import type { EligibilityCheck } from '@/types/rcm';
import { fmtDate, statusTag } from '@/utils/display';
import { CoverageSelect, InsurerSelect, PageHeader, PatientSelect, showError } from './shared';

const PURPOSES = [
  { value: 'benefits', label: 'Benefits' },
  { value: 'validation', label: 'Validation' },
  { value: 'discovery', label: 'Discovery' },
];

function Json({ value }: { value: unknown }) {
  if (value === null || value === undefined || (Array.isArray(value) && value.length === 0)) return <Typography.Text type="secondary">—</Typography.Text>;
  return <pre style={{ maxHeight: 360, overflow: 'auto', fontSize: 12, margin: 0 }}>{JSON.stringify(value, null, 2)}</pre>;
}

export function EligibilityPage() {
  const global = useGlobalFilters();
  const { can } = usePermission();
  const qc = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [status, setStatus] = useState<string>();
  const [insurerId, setInsurerId] = useState<string>();
  const [open, setOpen] = useState(false);
  const [detail, setDetail] = useState<EligibilityCheck>();
  const [form] = Form.useForm();
  const patientId = Form.useWatch('patientId', form) as string | undefined;
  const isNewborn = Form.useWatch('isNewborn', form) as boolean | undefined;
  const motherId = Form.useWatch('motherId', form) as string | undefined;

  const q = { ...global.rcm, status, insurerId, page, pageSize };
  const list = useQuery({ queryKey: ['rcm', 'eligibility', q], queryFn: () => rcmApi.eligibility.list(q), placeholderData: keepPreviousData });

  const check = useMutation({
    mutationFn: (v: {
      patientId: string;
      coverageId?: string;
      insurerId?: string;
      purposes: string[];
      servicedDate?: Dayjs;
      isNewborn?: boolean;
      isTransfer?: boolean;
    }) =>
      rcmApi.eligibility.check({
        patientId: v.patientId,
        coverageId: v.coverageId,
        insurerId: v.insurerId,
        providerId: global.hospitalId!,
        purposes: v.purposes,
        servicedDate: v.servicedDate?.format('YYYY-MM-DD'),
        isNewborn: !!v.isNewborn,
        isTransfer: !!v.isTransfer,
      }),
    onSuccess: (r) => {
      setOpen(false);
      form.resetFields();
      void qc.invalidateQueries({ queryKey: ['rcm', 'eligibility'] });
      setDetail(r);
    },
    onError: (e) => showError('Eligibility check failed', e),
  });

  const columns: ColumnsType<EligibilityCheck> = [
    { title: 'Requested', dataIndex: 'createdAt', render: (v) => fmtDate(v, true) },
    { title: 'Identifier', dataIndex: 'requestIdentifier', render: (v, r) => <a onClick={() => setDetail(r)}>{v}</a> },
    { title: 'Patient', dataIndex: 'patientId' },
    { title: 'Purposes', dataIndex: 'purposes', render: (v: string[]) => v?.join(', ') },
    { title: 'Service date', dataIndex: 'servicedDate', render: (v) => fmtDate(v) },
    { title: 'Status', dataIndex: 'status', render: statusTag },
    { title: 'Outcome', dataIndex: 'outcome', render: statusTag },
    { title: 'Eligible', dataIndex: 'isEligible', render: (v) => (v === null || v === undefined ? '—' : statusTag(v ? 'eligible' : 'not-eligible')) },
    { title: 'In force', dataIndex: 'inForce', render: (v) => (v === null || v === undefined ? '—' : v ? 'Yes' : 'No') },
    { title: 'Site', dataIndex: 'siteEligibility' },
    { title: 'Responded', dataIndex: 'respondedAt', render: (v) => fmtDate(v, true) },
  ];

  return (
    <>
      <PageHeader
        title="Eligibility"
        extra={
          <Space>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              disabled={!can(PERMISSIONS.EligibilityCheck)}
              onClick={() => {
                form.setFieldsValue({ purposes: ['benefits', 'validation'] });
                setOpen(true);
              }}
            >
              New check
            </Button>
            <Button icon={<ReloadOutlined />} onClick={() => void list.refetch()} />
          </Space>
        }
      />
      <Card>
        <Space wrap style={{ marginBottom: 12 }}>
          <Select allowClear placeholder="Status" style={{ width: 160 }} value={status} onChange={setStatus} options={['pending', 'completed', 'error', 'queued'].map((v) => ({ value: v, label: v }))} />
          <InsurerSelect value={insurerId} onChange={setInsurerId} />
        </Space>
        <Table<EligibilityCheck>
          rowKey="id"
          size="middle"
          loading={list.isFetching}
          dataSource={list.data?.items ?? []}
          columns={columns}
          scroll={{ x: 'max-content' }}
          onRow={(r) => ({ onDoubleClick: () => setDetail(r) })}
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
        open={open}
        title="New eligibility check"
        okText="Send to NPHIES"
        okButtonProps={{ loading: check.isPending, disabled: !global.hospitalId }}
        onCancel={() => setOpen(false)}
        onOk={() => form.validateFields().then((v) => check.mutate(v))}
        destroyOnClose
      >
        {!global.hospitalId && <Alert type="warning" showIcon style={{ marginBottom: 12 }} message="Select a hospital in the header filter first — it is the requesting provider." />}
        <Form form={form} layout="vertical">
          <Form.Item name="patientId" label="Patient" rules={[{ required: true }]}>
            <PatientSelect />
          </Form.Item>
          <Form.Item name="isNewborn" valuePropName="checked" style={{ marginBottom: 8 }}>
            <Checkbox onChange={() => form.setFieldsValue({ coverageId: undefined, motherId: undefined })}>
              Newborn on the mother&apos;s coverage
            </Checkbox>
          </Form.Item>
          {isNewborn && (
            <Form.Item name="motherId" label="Mother (coverage holder)" rules={[{ required: true }]}>
              <PatientSelect onChange={() => form.setFieldsValue({ coverageId: undefined })} />
            </Form.Item>
          )}
          <Form.Item name="coverageId" label="Coverage">
            <CoverageSelect patientId={isNewborn ? motherId : patientId} />
          </Form.Item>
          <Form.Item name="isTransfer" valuePropName="checked">
            <Checkbox>Referral / transfer of care (we are the receiving provider)</Checkbox>
          </Form.Item>
          <Form.Item name="insurerId" label="Insurer (when no coverage / discovery)">
            <InsurerSelect placeholder="Defaults to the coverage payer" />
          </Form.Item>
          <Form.Item name="purposes" label="Purposes" rules={[{ required: true }]}>
            <Checkbox.Group options={PURPOSES} />
          </Form.Item>
          <Form.Item name="servicedDate" label="Service date">
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>
        </Form>
      </Modal>

      <Drawer open={!!detail} width={720} title={detail ? `Eligibility ${detail.requestIdentifier}` : ''} onClose={() => setDetail(undefined)}>
        {detail && (
          <>
            <Descriptions size="small" bordered column={2}>
              <Descriptions.Item label="Status">{statusTag(detail.status)}</Descriptions.Item>
              <Descriptions.Item label="Outcome">{statusTag(detail.outcome)}</Descriptions.Item>
              <Descriptions.Item label="Eligible">{detail.isEligible === null || detail.isEligible === undefined ? '—' : detail.isEligible ? 'Yes' : 'No'}</Descriptions.Item>
              <Descriptions.Item label="In force">{detail.inForce === null || detail.inForce === undefined ? '—' : detail.inForce ? 'Yes' : 'No'}</Descriptions.Item>
              <Descriptions.Item label="Site eligibility">{detail.siteEligibility ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Flags">
                {[detail.isNewborn && 'Newborn', detail.isTransfer && 'Transfer'].filter(Boolean).join(', ') || '—'}
              </Descriptions.Item>
              <Descriptions.Item label="Not in force reason">{detail.notInForceReason ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Benefit period" span={2}>
                {fmtDate(detail.benefitPeriodStart)} → {fmtDate(detail.benefitPeriodEnd)}
              </Descriptions.Item>
              <Descriptions.Item label="Disposition" span={2}>{detail.disposition ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Message id" span={2}>{detail.messageId ?? '—'}</Descriptions.Item>
            </Descriptions>
            <Typography.Title level={5} style={{ marginTop: 16 }}>Benefits</Typography.Title>
            <Json value={detail.benefits} />
            <Typography.Title level={5} style={{ marginTop: 16 }}>Discovered coverages</Typography.Title>
            <Json value={detail.discoveredCoverages} />
            <Typography.Title level={5} style={{ marginTop: 16 }}>Errors</Typography.Title>
            <Json value={detail.errors} />
          </>
        )}
      </Drawer>
    </>
  );
}
