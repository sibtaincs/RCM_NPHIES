import { useMemo, useState } from 'react';
import {
  Alert,
  Button,
  Checkbox,
  Collapse,
  Descriptions,
  Divider,
  Drawer,
  Empty,
  Grid,
  Input,
  message,
  Popconfirm,
  Space,
  Spin,
  Table,
  Tabs,
  Tag,
  Typography,
} from 'antd';
import {
  CheckOutlined,
  CloseOutlined,
  EyeOutlined,
  SendOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { usePermission, PERMISSIONS } from '@/hooks/usePermission';
import {
  useClaimReviewMutations,
  useClaimReviewStatus,
  useClaimWorkflowDetails,
  useClaimsReadyForSubmission,
  useClaimsReviewQueue,
} from '@/hooks/useClaims';
import type { ClaimQueueStage, ClaimRecord, ClaimReviewDecision } from '@/types/claims';
import { getApiErrorMessage } from '@/utils/apiError';

function reviewTag(status?: string) {
  const normalized = status?.toLowerCase();
  const color = normalized === 'approved' ? 'success' : normalized === 'rejected' ? 'error' : 'default';
  return <Tag color={color}>{status || 'Pending'}</Tag>;
}

function formatAmount(amount?: number | null, currency?: string | null) {
  if (amount == null) return '—';
  try {
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency: currency || 'SAR',
      maximumFractionDigits: 2,
    }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency || 'SAR'}`;
  }
}

/** Review queue for technical and medical claim approval. */
export function ClaimsPage() {
  const screens = Grid.useBreakpoint();
  const isCompact = !screens.md;
  const detailColumns = screens.md ? 2 : 1;
  const { can } = usePermission();
  const [stage, setStage] = useState<ClaimQueueStage>('technical');
  const [selectedClaim, setSelectedClaim] = useState<ClaimRecord | null>(null);
  const [search, setSearch] = useState('');
  const [reviewNotes, setReviewNotes] = useState('');
  const { review, submit } = useClaimReviewMutations();
  const canRead = can(PERMISSIONS.ClaimsRead);
  const reviewStage = stage === 'ready' ? 'technical' : stage;
  const canReview = stage !== 'ready' && can(
    stage === 'technical' ? PERMISSIONS.ClaimsTechnicalReview : PERMISSIONS.ClaimsMedicalReview,
  );
  const reviewQueue = useClaimsReviewQueue(reviewStage, canRead && stage !== 'ready');
  const submissionQueue = useClaimsReadyForSubmission(canRead && stage === 'ready');
  const reviewStatus = useClaimReviewStatus(selectedClaim?.id ?? null, Boolean(selectedClaim));
  const workflowDetails = useClaimWorkflowDetails(selectedClaim?.id ?? null, Boolean(selectedClaim));
  const queueCount = stage === 'ready' ? submissionQueue.data?.count : reviewQueue.data?.count;
  const queueError = stage === 'ready' ? submissionQueue.error : reviewQueue.error;
  const queueLoading = stage === 'ready'
    ? submissionQueue.isLoading || submissionQueue.isFetching
    : reviewQueue.isLoading || reviewQueue.isFetching;

  const rows = useMemo(() => {
    const term = search.trim().toLowerCase();
    const claims = stage === 'ready'
      ? submissionQueue.data?.claims ?? []
      : reviewQueue.data?.claims ?? [];
    return term
      ? claims.filter(
          (claim) =>
            claim.claimNumber.toLowerCase().includes(term) ||
            claim.patientId.toLowerCase().includes(term),
        )
        : claims;
    }, [reviewQueue.data?.claims, search, stage, submissionQueue.data?.claims]);

  const columns: ColumnsType<ClaimRecord> = [
    {
      title: 'Claim',
      dataIndex: 'claimNumber',
      render: (value: string, claim) => (
        <Space direction="vertical" size={0}>
          <Typography.Text strong>{value}</Typography.Text>
          <Typography.Text type="secondary">Patient {claim.patientId}</Typography.Text>
        </Space>
      ),
    },
    { title: 'Type', dataIndex: 'claimType', width: 140, render: (value: string) => value || '—' },
    {
      title: 'Priority',
      dataIndex: 'priority',
      width: 120,
      render: (value?: string) => {
        const color = value?.toLowerCase() === 'stat' || value?.toLowerCase() === 'urgent' ? 'red' : 'default';
        return <Tag color={color}>{value || 'Normal'}</Tag>;
      },
    },
    {
      title: 'Amount',
      width: 150,
      align: 'right',
      render: (_, claim) => formatAmount(claim.total, claim.totalCurrency),
    },
    {
      title: 'Claim status',
      dataIndex: 'status',
      width: 130,
      render: (value: string) => <Tag>{value || 'Unknown'}</Tag>,
    },
    {
      title: '',
      key: 'action',
      width: 100,
      align: 'right',
      render: (_, claim) => (
        <Button
          icon={<EyeOutlined />}
          onClick={() => {
            setSelectedClaim(claim);
          }}
        >
            {stage === 'ready' ? 'Open' : 'Review'}
        </Button>
      ),
    },
  ];

  const closeDrawer = () => setSelectedClaim(null);
  const submitReview = (decision: ClaimReviewDecision) => {
    if (!selectedClaim || stage === 'ready') return;
    if (decision === 'Rejected' && !reviewNotes.trim()) {
      message.error('Add a reason before rejecting this claim.');
      return;
    }
    review.mutate(
      { id: selectedClaim.id, stage, payload: { decision, notes: reviewNotes.trim() || undefined } },
      { onSuccess: closeDrawer },
    );
  };

  const activeStageStatus = stage === 'technical'
    ? reviewStatus.data?.technicalReviewStatus
    : stage === 'medical'
      ? reviewStatus.data?.medicalReviewStatus
      : undefined;

  return (
    <Space direction="vertical" style={{ width: '100%' }} size="middle">
      <Typography.Title level={3} style={{ margin: 0 }}>Claims</Typography.Title>

      {!canRead ? (
        <Alert type="warning" showIcon message="You do not have permission to view claims." />
      ) : (
        <>
          <Tabs
            activeKey={stage}
            onChange={(key) => {
              setStage(key as ClaimQueueStage);
              setSearch('');
              setSelectedClaim(null);
            }}
            items={[
              { key: 'technical', label: 'Technical review' },
              { key: 'medical', label: 'Medical review' },
              { key: 'ready', label: 'Ready to submit' },
            ]}
          />

          <Space style={{ justifyContent: 'space-between', width: '100%' }} wrap>
            <Input.Search
              allowClear
              placeholder="Search claim or patient ID"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              style={{ width: 320, maxWidth: '100%' }}
            />
            <Typography.Text type="secondary">
              {queueCount ?? 0} {stage === 'ready' ? 'ready to submit' : 'awaiting review'}
            </Typography.Text>
          </Space>

          {queueError && (
            <Alert type="error" showIcon message="Could not load claims" description={getApiErrorMessage(queueError)} />
          )}

          <Table<ClaimRecord>
            rowKey={(claim) => String(claim.id)}
            columns={columns}
            dataSource={rows}
            loading={queueLoading}
            locale={{
              emptyText: (
                <Empty description={stage === 'ready' ? 'No claims ready to submit' : 'No claims awaiting review'} />
              ),
            }}
            pagination={{ pageSize: 10, showSizeChanger: true, pageSizeOptions: ['10', '20', '50'] }}
            scroll={{ x: 'max-content' }}
          />
        </>
      )}

      <Drawer
        title={selectedClaim?.claimNumber}
        open={Boolean(selectedClaim)}
        onClose={closeDrawer}
        width={screens.md ? 'min(920px, 94vw)' : '100vw'}
        destroyOnClose
        styles={{
          header: { paddingInline: isCompact ? 16 : 24 },
          body: { padding: isCompact ? 12 : 24, overflowX: 'hidden', minWidth: 0 },
          footer: { padding: isCompact ? '12px 16px' : '16px 24px' },
        }}
        footer={selectedClaim ? (
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, flexWrap: 'wrap' }}>
            {canReview && activeStageStatus?.toLowerCase() !== 'approved' && (
              <>
                <Button
                  danger
                  icon={<CloseOutlined />}
                  loading={review.isPending}
                  disabled={!reviewStatus.data}
                  onClick={() => submitReview('Rejected')}
                >
                  Reject
                </Button>
                <Button
                  type="primary"
                  icon={<CheckOutlined />}
                  loading={review.isPending}
                  disabled={!reviewStatus.data}
                  onClick={() => submitReview('Approved')}
                >
                  Approve
                </Button>
              </>
            )}
            {can(PERMISSIONS.ClaimsSubmit) && reviewStatus.data?.isApprovedForSubmission && (
              <Popconfirm
                title="Submit this claim to NPHIES?"
                description="This action sends the claim for processing."
                okText="Submit claim"
                onConfirm={() => submit.mutate(selectedClaim.id, { onSuccess: closeDrawer })}
              >
                <Button type="primary" icon={<SendOutlined />} loading={submit.isPending}>
                  Submit to NPHIES
                </Button>
              </Popconfirm>
            )}
          </div>
        ) : null}
      >
        {selectedClaim && (
          <Space direction="vertical" style={{ width: '100%', minWidth: 0 }} size="middle">
            {workflowDetails.isLoading ? <Spin /> : workflowDetails.error ? (
              <Alert type="error" showIcon message={getApiErrorMessage(workflowDetails.error)} />
            ) : workflowDetails.data ? (
              <Collapse
                defaultActiveKey={isCompact
                  ? ['patient-clinic']
                  : ['patient-clinic', 'encounter-insurance', 'medical', 'mds', 'diagnoses', 'services']}
                style={{ width: '100%', minWidth: 0 }}
                items={[
                  {
                    key: 'patient-clinic',
                    label: 'Patient / Clinic Details',
                    children: (
                      <Descriptions column={detailColumns} size="small" bordered>
                        <Descriptions.Item label="Patient #">{workflowDetails.data.patientId}</Descriptions.Item>
                        <Descriptions.Item label="Name">{workflowDetails.data.patientName || '—'}</Descriptions.Item>
                        <Descriptions.Item label="DOB / Age">
                          {workflowDetails.data.patientDateOfBirth
                            ? `${new Date(workflowDetails.data.patientDateOfBirth).toLocaleDateString()}${workflowDetails.data.patientAgeYears == null ? '' : ` (Age ${workflowDetails.data.patientAgeYears} Years)`}`
                            : '—'}
                        </Descriptions.Item>
                        <Descriptions.Item label="Gender">
                          {{ M: 'Male', F: 'Female', O: 'Other', U: 'Unknown' }[workflowDetails.data.patientGender?.toUpperCase() ?? '']
                            ?? workflowDetails.data.patientGender
                            ?? '—'}
                        </Descriptions.Item>
                        <Descriptions.Item label="MRN">{workflowDetails.data.patientMrn || '—'}</Descriptions.Item>
                        <Descriptions.Item label="National ID">{workflowDetails.data.patientNationalId || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Patient Name Arabic">{workflowDetails.data.patientNameArabic || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Family Name Arabic">{workflowDetails.data.familyNameArabic || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Doctor Ref">{workflowDetails.data.doctorReference || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Doctor Name">{workflowDetails.data.doctorName || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Doctor License">{workflowDetails.data.doctorLicenseNumber || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Clinic">{workflowDetails.data.clinicName || '—'}</Descriptions.Item>
                      </Descriptions>
                    ),
                  },
                  {
                    key: 'encounter-insurance',
                    label: 'Encounter / Insurance Details',
                    children: (
                      <Descriptions column={detailColumns} size="small" bordered>
                        <Descriptions.Item label="Encounter #">{workflowDetails.data.encounterIdentifierValue || workflowDetails.data.encounterId || '—'}</Descriptions.Item>
                        <Descriptions.Item label="New Born">{workflowDetails.data.isNewborn == null ? '—' : workflowDetails.data.isNewborn ? 'Yes' : 'No'}</Descriptions.Item>
                        <Descriptions.Item label="Encounter Type">
                          {{ AMB: 'Out Patient', IMP: 'In Patient', EMER: 'Emergency', REF: 'Referral' }[workflowDetails.data.encounterClass?.toUpperCase() ?? '']
                            ?? workflowDetails.data.encounterClass
                            ?? '—'}
                        </Descriptions.Item>
                        <Descriptions.Item label="Encounter Status">{workflowDetails.data.encounterStatus || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Start">
                          {workflowDetails.data.encounterPeriodStart
                            ? new Date(workflowDetails.data.encounterPeriodStart).toLocaleString()
                            : '—'}
                        </Descriptions.Item>
                        <Descriptions.Item label="End">
                          {workflowDetails.data.encounterPeriodEnd
                            ? new Date(workflowDetails.data.encounterPeriodEnd).toLocaleString()
                            : '—'}
                        </Descriptions.Item>
                        <Descriptions.Item label="Invoice Date">
                          {workflowDetails.data.encounterInvoiceDate
                            ? new Date(workflowDetails.data.encounterInvoiceDate).toLocaleDateString()
                            : '—'}
                        </Descriptions.Item>
                        <Descriptions.Item label="Policy Holder No">{workflowDetails.data.policyHolderNumber || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Policy Holder Name">{workflowDetails.data.policyHolderName || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Admit Source">{workflowDetails.data.encounterAdmitSource || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Payer">{workflowDetails.data.payerName || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Identification #">{workflowDetails.data.identificationNumber || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Nationality ID">{workflowDetails.data.nationalityId || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Member ID">{workflowDetails.data.coverageMemberId || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Payer Policy Name">{workflowDetails.data.payerPolicyName || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Policy #">{workflowDetails.data.coveragePolicyNumber || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Plan Code">{workflowDetails.data.coveragePlanCode || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Coverage Type">{workflowDetails.data.coverageType || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Coverage Status">{workflowDetails.data.coverageStatus || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Eligibility">
                          <Space>
                            <Tag>{workflowDetails.data.eligibilityOfflineReference ? 'Offline details recorded' : 'Not checked'}</Tag>
                            <Button size="small" disabled>Check Eligibility</Button>
                          </Space>
                        </Descriptions.Item>
                        <Descriptions.Item label="NPHIES Approval AuthRef">{workflowDetails.data.preAuthorizationReference || '—'}</Descriptions.Item>
                        <Descriptions.Item label="NPHIES Approval Identifier">{workflowDetails.data.nphiesApprovalIdentifier || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Approval URL">
                          {workflowDetails.data.approvalUrl ? (
                            <a href={workflowDetails.data.approvalUrl} target="_blank" rel="noreferrer">Open approval</a>
                          ) : '—'}
                        </Descriptions.Item>
                        <Descriptions.Item label="Offline Approval Date">
                          {workflowDetails.data.offlineApprovalDate
                            ? new Date(workflowDetails.data.offlineApprovalDate).toLocaleDateString()
                            : '—'}
                        </Descriptions.Item>
                        <Descriptions.Item label="Offline Eligibility Reference">{workflowDetails.data.eligibilityOfflineReference || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Coverage Period">
                          {workflowDetails.data.coverageStartDate && workflowDetails.data.coverageEndDate
                            ? `${new Date(workflowDetails.data.coverageStartDate).toLocaleDateString()} - ${new Date(workflowDetails.data.coverageEndDate).toLocaleDateString()}`
                            : '—'}
                        </Descriptions.Item>
                      </Descriptions>
                    ),
                  },
                  {
                    key: 'medical',
                    label: 'Medical Details',
                    children: (
                      <Space direction="vertical" style={{ width: '100%' }}>
                        <Descriptions column={1} size="small" bordered>
                          <Descriptions.Item label="Chief Complaint">{workflowDetails.data.chiefComplaint || '—'}</Descriptions.Item>
                          <Descriptions.Item label="Significant Signs">{workflowDetails.data.significantSigns || '—'}</Descriptions.Item>
                          <Descriptions.Item label="History of Present Illness">{workflowDetails.data.historyOfPresentIllness || '—'}</Descriptions.Item>
                          <Descriptions.Item label="Patient History">{workflowDetails.data.patientHistory || '—'}</Descriptions.Item>
                          <Descriptions.Item label="Physical Examination">{workflowDetails.data.physicalExamination || '—'}</Descriptions.Item>
                          <Descriptions.Item label="Investigation Result">{workflowDetails.data.investigationResult || '—'}</Descriptions.Item>
                          <Descriptions.Item label="Treatment Plan">{workflowDetails.data.treatmentPlan || '—'}</Descriptions.Item>
                        </Descriptions>
                        <Typography.Text strong>Vital Signs</Typography.Text>
                        <Table
                          style={{ width: '100%', minWidth: 0 }}
                          size="small"
                          rowKey="name"
                          pagination={false}
                          dataSource={workflowDetails.data.vitalSigns}
                          columns={[
                            { title: 'Vital', dataIndex: 'name' },
                            { title: 'Value', dataIndex: 'value' },
                            { title: 'Unit', dataIndex: 'unit', render: (value?: string) => value || '—' },
                          ]}
                        />
                      </Space>
                    ),
                  },
                  {
                    key: 'mds',
                    label: 'Additional MDS Information',
                    children: (
                      <Descriptions column={detailColumns} size="small" bordered>
                        <Descriptions.Item label="Emergency Arrival Code">{workflowDetails.data.emergencyArrivalCode || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Triage Category">{workflowDetails.data.triageCategory || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Discharge Disposition">{workflowDetails.data.dischargeDisposition || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Encounter Admit Source">{workflowDetails.data.encounterAdmitSource || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Emergency Department Disposition">{workflowDetails.data.emergencyDepartmentDisposition || '—'}</Descriptions.Item>
                        <Descriptions.Item label="Triage Date">
                          {workflowDetails.data.triageDate ? new Date(workflowDetails.data.triageDate).toLocaleDateString() : '—'}
                        </Descriptions.Item>
                        <Descriptions.Item label="Discharge Specialty">{workflowDetails.data.dischargeSpecialty || '—'}</Descriptions.Item>
                      </Descriptions>
                    ),
                  },
                  {
                    key: 'diagnoses',
                    label: `Diagnosis Details (${workflowDetails.data.diagnoses.length})`,
                    children: (
                      <Table
                        style={{ width: '100%', minWidth: 0 }}
                        size="small"
                        rowKey="sequence"
                        pagination={false}
                        dataSource={workflowDetails.data.diagnoses}
                        columns={[
                          { title: 'Code', dataIndex: 'diagnosisCode' },
                          { title: 'Description', dataIndex: 'diagnosisDisplay' },
                          { title: 'Type', dataIndex: 'diagnosisType' },
                        ]}
                      />
                    ),
                  },
                  {
                    key: 'attachments',
                    label: 'Attachments (0)',
                    children: (
                      <Space direction="vertical" style={{ width: '100%' }}>
                        <Typography.Text type="secondary">No attachments are stored for this claim.</Typography.Text>
                        <Space>
                          <Button disabled>Attach PDF (max 9 MB)</Button>
                          <Checkbox disabled>Include Submission</Checkbox>
                        </Space>
                      </Space>
                    ),
                  },
                  {
                    key: 'services',
                    label: `Services (${workflowDetails.data.services.length}) / Reference Invoices`,
                    children: (
                      <Table
                        style={{ width: '100%', minWidth: 0 }}
                        size="small"
                        rowKey="sequence"
                        pagination={false}
                        scroll={{ x: 760 }}
                        dataSource={workflowDetails.data.services}
                        columns={[
                          { title: 'Service', dataIndex: 'serviceName', render: (value: string, service) => `${service.serviceCode} ${value || ''}` },
                          { title: 'Date', dataIndex: 'serviceDate', render: (value?: string) => value ? new Date(value).toLocaleDateString() : '—' },
                          { title: 'Qty', dataIndex: 'quantity', align: 'right' },
                          { title: 'Gross Amount', dataIndex: 'unitPrice', align: 'right', render: (value?: number) => formatAmount(value, workflowDetails.data?.totalCurrency) },
                          { title: 'Net Amount', dataIndex: 'net', align: 'right', render: (value?: number) => formatAmount(value, workflowDetails.data?.totalCurrency) },
                          { title: 'Reference Invoice', dataIndex: 'referenceInvoice', render: (value?: string) => value || '—' },
                          { title: 'Approval #', dataIndex: 'approvalNumber', render: (value?: string) => value || '—' },
                          { title: 'Approved Amount', dataIndex: 'approvedAmount', align: 'right', render: (value?: number) => formatAmount(value, workflowDetails.data?.totalCurrency) },
                        ]}
                      />
                    ),
                  },
                ]}
              />
            ) : null}

            <Divider orientation="left">Approval status</Divider>
            {reviewStatus.error && (
              <Alert type="error" showIcon message={getApiErrorMessage(reviewStatus.error)} />
            )}
            {reviewStatus.isLoading ? (
              <Spin />
            ) : reviewStatus.data ? (
              <Descriptions column={1} size="small">
                <Descriptions.Item label="Technical">
                  {reviewTag(reviewStatus.data.technicalReviewStatus)}
                  {reviewStatus.data.technicalReviewNotes && (
                    <Typography.Paragraph type="secondary" style={{ margin: '4px 0 0' }}>
                      {reviewStatus.data.technicalReviewNotes}
                    </Typography.Paragraph>
                  )}
                </Descriptions.Item>
                <Descriptions.Item label="Medical">
                  {reviewTag(reviewStatus.data.medicalReviewStatus)}
                  {reviewStatus.data.medicalReviewNotes && (
                    <Typography.Paragraph type="secondary" style={{ margin: '4px 0 0' }}>
                      {reviewStatus.data.medicalReviewNotes}
                    </Typography.Paragraph>
                  )}
                </Descriptions.Item>
              </Descriptions>
            ) : null}

            {canReview && activeStageStatus?.toLowerCase() !== 'approved' && (
              <>
                <Input.TextArea
                  rows={3}
                  value={reviewNotes}
                  onChange={(event) => setReviewNotes(event.target.value)}
                  placeholder="Review notes or rejection reason"
                  maxLength={1000}
                />
              </>
            )}
          </Space>
        )}
      </Drawer>
    </Space>
  );
}
