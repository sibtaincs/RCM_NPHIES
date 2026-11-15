import { useState } from 'react';
import { Alert, Button, Form, Input, Modal, Select, Space, Table, Tag, Typography, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation } from '@tanstack/react-query';
import { rcmApi } from '@/api/endpoints/rcm';
import { PERMISSIONS, usePermission } from '@/hooks/usePermission';
import type { DiscoveredPolicy, InsuranceDiscovery } from '@/types/rcm';
import { fmtDate } from '@/utils/display';
import { PatientSelect, showError } from './shared';

const SYSTEM_TYPES = [
  { value: 1, label: 'Nationals and residents (national id or Iqama)' },
  { value: 2, label: 'Visitors (visa, passport or border number)' },
  { value: 3, label: 'Tourists (visa or passport number)' },
  { value: 4, label: 'Premium residency (visa or passport number)' },
  { value: 5, label: 'Hajj and Umrah visitors (passport number)' },
];

const DEPENDENT_RELATIONSHIPS = [
  { value: 'child', label: 'Child (on the mother’s or father’s policy)' },
  { value: 'spouse', label: 'Spouse' },
  { value: 'parent', label: 'Parent (on a son’s or daughter’s policy)' },
  { value: 'other', label: 'Other relative' },
];

interface FormValues {
  patientId?: string;
  patientKey?: string;
  systemType: number;
}

/** Looks a patient's insurer and policies up at NPHIES by identity number, and can save a policy as a coverage. */
export function InsuranceDiscoveryModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { can } = usePermission();
  const [form] = Form.useForm<FormValues>();
  const [result, setResult] = useState<InsuranceDiscovery>();
  const [asked, setAsked] = useState<FormValues>();

  const discover = useMutation({
    mutationFn: (v: FormValues) => rcmApi.eligibility.discoverInsurance({ patientId: v.patientId, patientKey: v.patientKey?.trim() || undefined, systemType: v.systemType }),
    onSuccess: (r, v) => {
      setResult(r);
      setAsked(v);
    },
    onError: (e) => showError('Insurance discovery failed', e),
  });

  // A dependent's policy needs to know how the patient is related to the policy holder, and who that is.
  const [dependent, setDependent] = useState<DiscoveredPolicy>();
  const [relationship, setRelationship] = useState<string>('child');
  const [holderId, setHolderId] = useState<string>();

  const addCoverage = useMutation({
    mutationFn: (v: { policy: DiscoveredPolicy; relationship?: string; subscriberPatientId?: string }) =>
      rcmApi.eligibility.addDiscoveredCoverage({
        patientId: asked!.patientId!,
        patientKey: result!.patientKey,
        systemType: result!.systemType,
        policyNumber: v.policy.policyNumber!,
        relationship: v.relationship,
        subscriberPatientId: v.subscriberPatientId,
      }),
    onSuccess: (r) => {
      void message.success(r.created ? `Coverage for policy ${r.policyNumber} added` : `Policy ${r.policyNumber} is already on file`);
      setDependent(undefined);
      setHolderId(undefined);
      if (asked) discover.mutate(asked);
    },
    onError: (e) => showError('Could not add the coverage', e),
  });

  const columns: ColumnsType<DiscoveredPolicy> = [
    { title: 'Policy', dataIndex: 'policyNumber' },
    {
      title: 'Insurer',
      render: (_, p) => (
        <div>
          <div>{p.insuranceCompanyEn ?? p.insuranceCompanyAr ?? '—'}</div>
          <Typography.Text type="secondary">
            {p.matchedInsurerName ? `On file as ${p.matchedInsurerName}` : `Id ${p.insuranceCompanyId ?? '—'}: no matching payer on file`}
          </Typography.Text>
        </div>
      ),
    },
    { title: 'Class', dataIndex: 'className', render: (v) => v ?? '—' },
    { title: 'Network', dataIndex: 'networkId', render: (v) => v ?? '—' },
    { title: 'Member', dataIndex: 'beneficiaryNumber', render: (v, p) => [v, p.beneficiaryType].filter(Boolean).join(' · ') || '—' },
    { title: 'Deductible %', dataIndex: 'deductibleRate', render: (v) => v ?? '—' },
    { title: 'Max limit', dataIndex: 'maxLimit', render: (v) => v ?? '—' },
    { title: 'Expires', dataIndex: 'expiryDate', render: (v, p) => (p.expired ? <Tag color="red">Expired {fmtDate(v)}</Tag> : fmtDate(v)) },
    {
      title: '',
      render: (_, p) =>
        p.existingCoverageId ? (
          <Tag color="green">On file</Tag>
        ) : (
          <Button
            size="small"
            disabled={!can(PERMISSIONS.PatientsUpdate) || !asked?.patientId || !p.matchedInsurerId || !p.policyNumber}
            title={
              !asked?.patientId
                ? 'Select a patient to save the coverage to'
                : !p.matchedInsurerId
                  ? 'Add this insurer to the payers first'
                  : undefined
            }
            loading={addCoverage.isPending && addCoverage.variables?.policy.policyNumber === p.policyNumber}
            onClick={() => (p.beneficiaryType === 'dependent' ? setDependent(p) : addCoverage.mutate({ policy: p }))}
          >
            Add coverage
          </Button>
        ),
    },
  ];

  return (
    <Modal
      open={open}
      width={980}
      title="Find insurance"
      footer={null}
      destroyOnClose
      onCancel={() => {
        setResult(undefined);
        setAsked(undefined);
        form.resetFields();
        onClose();
      }}
    >
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 12 }}
        message="Looks the patient's insurer and policy up at NPHIES by identity number, so you do not have to ask the patient for policy details."
      />
      <Form form={form} layout="vertical" initialValues={{ systemType: 1 }} onFinish={(v) => discover.mutate(v)}>
        <Space align="end" wrap>
          <Form.Item name="patientId" label="Patient" style={{ width: 260 }}>
            <PatientSelect />
          </Form.Item>
          <Form.Item name="patientKey" label="Identity number (if different or no patient)" style={{ width: 260 }}>
            <Input placeholder="Defaults to the patient's id number" allowClear />
          </Form.Item>
          <Form.Item name="systemType" label="Beneficiary type" style={{ width: 320 }}>
            <Select options={SYSTEM_TYPES} />
          </Form.Item>
          <Form.Item>
            <Button type="primary" htmlType="submit" loading={discover.isPending}>
              Search
            </Button>
          </Form.Item>
        </Space>
      </Form>

      {result && result.simulated && <Alert type="warning" showIcon style={{ marginBottom: 12 }} message="Sandbox answer: this is simulated data, not from NPHIES." />}
      {result && !result.found && (
        <Alert
          type={result.errorCode === '105' ? 'info' : 'error'}
          showIcon
          style={{ marginBottom: 12 }}
          message={result.errorCode === '105' ? `No insurance record found for ${result.patientKey}.` : `${result.errorCode ?? 'Error'}: ${result.errorMessage ?? 'The lookup failed.'}`}
        />
      )}
      {result && result.found && (
        <Table<DiscoveredPolicy> rowKey={(p) => `${p.insuranceCompanyId}-${p.policyNumber}`} size="small" pagination={false} dataSource={result.policies} columns={columns} scroll={{ x: 'max-content' }} />
      )}
      <Modal
        open={!!dependent}
        title={`Dependent on policy ${dependent?.policyNumber ?? ''}`}
        okText="Add coverage"
        okButtonProps={{ loading: addCoverage.isPending }}
        onCancel={() => setDependent(undefined)}
        onOk={() => dependent && addCoverage.mutate({ policy: dependent, relationship, subscriberPatientId: holderId })}
        destroyOnClose
      >
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="NPHIES lists this patient as a dependent. Say how the patient is related to the policy holder: a child on the mother's or father's policy is “Child”."
        />
        <Form layout="vertical">
          <Form.Item label="The patient is the policy holder's" required>
            <Select value={relationship} onChange={setRelationship} options={DEPENDENT_RELATIONSHIPS} />
          </Form.Item>
          <Form.Item label="Policy holder (mother, father, spouse…), if on file">
            <PatientSelect value={holderId} onChange={setHolderId} />
          </Form.Item>
        </Form>
      </Modal>
    </Modal>
  );
}
