import { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Badge,
  Button,
  Checkbox,
  Col,
  DatePicker,
  Descriptions,
  Drawer,
  Empty,
  Input,
  InputNumber,
  List,
  Row,
  Select,
  Space,
  Spin,
  Switch,
  Table,
  Tabs,
  Tag,
  Typography,
  Upload,
  message,
} from 'antd';
import {
  CodeOutlined,
  DeleteOutlined,
  PaperClipOutlined,
  PlusOutlined,
  SafetyCertificateOutlined,
  SaveOutlined,
} from '@ant-design/icons';
import dayjs from 'dayjs';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { rcmApi } from '@/api/endpoints/rcm';
import type {
  ClaimEdit,
  ClaimEditCareTeam,
  ClaimEditDiagnosis,
  ClaimEditItem,
  ClaimEditLens,
  ClaimEditSupportingInfo,
  ClaimHealth,
} from '@/types/claimEditor';
import { getApiErrorMessage } from '@/utils/apiError';
import { fmtMoney, statusTag } from '@/utils/display';
import { PERMISSIONS, usePermission } from '@/hooks/usePermission';
import { BundlePreviewModal } from './JsonViewerModal';
import { ClaimDocumentsTab } from './ClaimDocumentsTab';
import {
  CoverageSelect,
  DiagnosisCodeInput,
  EncounterSelect,
  InsurerSelect,
  LocationSelect,
  PatientSelect,
  PractitionerSelect,
  ProviderSelect,
  showError,
} from './shared';

const CLAIM_TYPES = ['institutional', 'oral', 'pharmacy', 'professional', 'vision'];
const SUB_TYPES = [
  { value: 'op', label: 'OP - Outpatient' },
  { value: 'ip', label: 'IP - Inpatient' },
  { value: 'emr', label: 'EMR - Emergency' },
];
const PRIORITIES = ['normal', 'stat', 'deferred'];
const DIAGNOSIS_TYPES = ['principal', 'secondary', 'admitting', 'discharge', 'differential'];
const CARE_ROLES = ['primary', 'assist', 'supervisor', 'other'];
const INFO_CATEGORIES = [
  'info',
  'onset',
  'attachment',
  'chief-complaint',
  'reason-for-visit',
  'missingtooth',
  'hospitalized',
  'employmentImpacted',
  'lab-test',
  'days-supply',
  'vital-sign-weight',
  'vital-sign-height',
  'vital-sign-systolic',
  'vital-sign-diastolic',
  'temperature',
  'pulse',
  'respiratory-rate',
  'oxygen-saturation',
  'icu-hours',
  'ventilation-hours',
  'estimated-Length-of-Stay',
  'admission-weight',
  'birth-weight',
  'patient-history',
  'history-of-present-illness',
  'physical-examination',
  'treatment-plan',
  'investigation-result',
  'morphology',
];
const CODE_SYSTEMS = [
  { value: 'http://nphies.sa/terminology/CodeSystem/services', label: 'NPHIES services' },
  { value: 'http://nphies.sa/terminology/CodeSystem/procedures', label: 'Procedures (ACHI)' },
  { value: 'http://nphies.sa/terminology/CodeSystem/oral-health-op', label: 'Oral health OP' },
  { value: 'http://nphies.sa/terminology/CodeSystem/oral-health-ip', label: 'Oral health IP' },
  { value: 'http://nphies.sa/terminology/CodeSystem/medication-codes', label: 'Medications (GTIN)' },
  { value: 'http://nphies.sa/terminology/CodeSystem/medical-devices', label: 'Medical devices' },
  { value: 'http://nphies.sa/terminology/CodeSystem/imaging', label: 'Imaging' },
  { value: 'http://nphies.sa/terminology/CodeSystem/laboratory', label: 'Laboratory' },
  { value: 'http://nphies.sa/terminology/CodeSystem/moh-category', label: 'MOH category' },
];

const round2 = (n: number) => Math.round(n * 100) / 100;
const nextSeq = (rows: { sequence: number }[]) => rows.reduce((m, r) => Math.max(m, r.sequence), 0) + 1;

/** Mirrors backend recalculation: gross = qty × price × factor; tax = gross × rate%; net = gross + tax. */
function calcItem(i: ClaimEditItem) {
  const gross = (i.quantity || 0) * (i.unitPrice || 0) * (i.factor ?? 1);
  const tax = round2((gross * (i.taxRate ?? 0)) / 100);
  return { gross: round2(gross), tax, net: round2(gross + tax) };
}

function DateField({
  value,
  onChange,
  disabled,
  placeholder,
}: {
  value?: string | null;
  onChange: (v: string | null) => void;
  disabled?: boolean;
  placeholder?: string;
}) {
  return (
    <DatePicker
      style={{ width: '100%' }}
      disabled={disabled}
      placeholder={placeholder}
      value={value ? dayjs(value) : null}
      onChange={(d) => onChange(d ? d.format('YYYY-MM-DD') : null)}
    />
  );
}

function Field({ label, required, children, span = 8 }: { label: string; required?: boolean; children: React.ReactNode; span?: number }) {
  return (
    <Col xs={24} md={span}>
      <div style={{ marginBottom: 12 }}>
        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
          {label}
          {required && <span style={{ color: '#ff4d4f' }}> *</span>}
        </Typography.Text>
        <div>{children}</div>
      </div>
    </Col>
  );
}

function newDraft(use: string, providerId?: string): ClaimEdit {
  const today = dayjs().format('YYYY-MM-DD');
  return {
    use,
    claimType: 'professional',
    claimSubType: 'op',
    priority: 'normal',
    payeeType: 'provider',
    patientId: '',
    coverageId: '',
    providerId: providerId ?? '',
    insurerId: '',
    billablePeriodStart: today,
    billablePeriodEnd: today,
    isShadowBilling: false,
    items: [],
    diagnoses: [],
    careTeam: [],
    supportingInfo: [],
  };
}

function HealthPanel({ health, loading }: { health?: ClaimHealth; loading?: boolean }) {
  if (loading) return <Spin size="small" />;
  if (!health) return <Typography.Text type="secondary">Save the claim to run the health check.</Typography.Text>;
  if (health.issues.length === 0)
    return <Alert type="success" showIcon message="Ready to send — no problems found." />;
  return (
    <List
      size="small"
      dataSource={health.issues}
      renderItem={(i) => (
        <List.Item>
          <Space align="start">
            <Tag color={i.severity === 'Error' ? 'red' : i.severity === 'Warning' ? 'orange' : 'blue'}>{i.severity}</Tag>
            <div>
              <div>{i.message}</div>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                {i.code}
                {i.field ? ` · ${i.field}` : ''}
              </Typography.Text>
            </div>
          </Space>
        </List.Item>
      )}
    />
  );
}

/**
 * Create or edit a claim / pre-authorization. Records that were sent to NPHIES are shown read-only;
 * use the "Correct" actions (resubmit / update / extend) to get an editable copy.
 */
export function ClaimEditorDrawer({
  claimId,
  use = 'claim',
  defaultProviderId,
  onClose,
  onSaved,
}: {
  claimId?: string;
  use?: 'claim' | 'preauthorization';
  defaultProviderId?: string;
  onClose: () => void;
  onSaved?: (id: string) => void;
}) {
  const qc = useQueryClient();
  const { can } = usePermission();
  const [id, setId] = useState(claimId);
  const [draft, setDraft] = useState<ClaimEdit>();
  const [dirty, setDirty] = useState(false);
  const [preview, setPreview] = useState(false);
  const [serverErrors, setServerErrors] = useState<string>();

  const loaded = useQuery({
    queryKey: ['claim-edit', id],
    queryFn: () => rcmApi.claims.getForEdit(id!),
    enabled: !!id,
    gcTime: 0,
  });
  const health = useQuery({
    queryKey: ['claim-health', id],
    queryFn: () => rcmApi.claims.health(id!),
    enabled: !!id,
  });

  useEffect(() => {
    if (!id && !draft) setDraft(newDraft(use, defaultProviderId));
  }, [id, draft, use, defaultProviderId]);
  useEffect(() => {
    if (loaded.data && !dirty) setDraft(loaded.data);
  }, [loaded.data, dirty]);

  const isNew = !id;
  const readOnly = !isNew && (loaded.data?.editable === false || !can(PERMISSIONS.ClaimsUpdate));
  const isPreauth = (draft?.use ?? use) === 'preauthorization';

  const patch = (p: Partial<ClaimEdit>) => {
    setDraft((d) => (d ? { ...d, ...p } : d));
    setDirty(true);
  };
  const patchRow = <K extends 'items' | 'diagnoses' | 'careTeam' | 'supportingInfo'>(
    key: K,
    index: number,
    p: Partial<ClaimEdit[K][number]>,
  ) => {
    setDraft((d) => {
      if (!d) return d;
      const rows = [...d[key]] as ClaimEdit[K][number][];
      rows[index] = { ...rows[index], ...p };
      return { ...d, [key]: rows };
    });
    setDirty(true);
  };
  const removeRow = (key: 'items' | 'diagnoses' | 'careTeam' | 'supportingInfo', index: number) => {
    setDraft((d) => (d ? { ...d, [key]: (d[key] as unknown[]).filter((_, i) => i !== index) } : d));
    setDirty(true);
  };

  const save = useMutation({
    mutationFn: (body: ClaimEdit) => (id ? rcmApi.claims.saveDraft(id, body) : rcmApi.claims.createDraft(body)),
    onSuccess: (saved) => {
      setServerErrors(undefined);
      setDirty(false);
      setDraft(saved);
      if (saved.id) {
        setId(saved.id);
        qc.setQueryData(['claim-edit', saved.id], saved);
        onSaved?.(saved.id);
      }
      void qc.invalidateQueries({ queryKey: ['rcm'] });
      void qc.invalidateQueries({ queryKey: ['claim-timeline'] });
      void qc.invalidateQueries({ queryKey: ['claim-health'] });
      void qc.invalidateQueries({ queryKey: ['claim-audit'] });
      void message.success(`${saved.claimNumber ?? 'Claim'} saved`);
    },
    onError: (e) => {
      setServerErrors(getApiErrorMessage(e));
      showError('Save failed', e);
    },
  });

  const totals = useMemo(() => {
    const rows = draft?.items ?? [];
    return rows.reduce(
      (acc, i) => {
        const c = calcItem(i);
        acc.gross += c.gross;
        acc.tax += c.tax;
        acc.net += c.net;
        acc.share += i.patientShare ?? 0;
        return acc;
      },
      { gross: 0, tax: 0, net: 0, share: 0 },
    );
  }, [draft?.items]);

  if (!draft || (id && loaded.isLoading)) {
    return (
      <Drawer open width="92%" onClose={onClose} title="Claim editor">
        {loaded.error ? <Alert type="error" message={getApiErrorMessage(loaded.error)} /> : <Spin />}
      </Drawer>
    );
  }

  const d = draft;
  const diagOptions = d.diagnoses.map((x) => ({ value: x.sequence, label: `${x.sequence} · ${x.diagnosisCode}` }));
  const careOptions = d.careTeam.map((x) => ({ value: x.sequence, label: `${x.sequence} · ${x.practitionerName ?? x.role ?? ''}` }));
  const infoOptions = d.supportingInfo.map((x) => ({ value: x.sequence, label: `${x.sequence} · ${x.category}` }));

  const header = (
    <Row gutter={16}>
      <Field label="Use">
        <Select
          style={{ width: '100%' }}
          disabled={readOnly || !isNew}
          value={d.use}
          onChange={(v) => patch({ use: v })}
          options={[
            { value: 'claim', label: 'Claim' },
            { value: 'preauthorization', label: 'Prior authorization' },
          ]}
        />
      </Field>
      <Field label="Claim type" required>
        <Select style={{ width: '100%' }} disabled={readOnly} value={d.claimType} onChange={(v) => patch({ claimType: v })} options={CLAIM_TYPES.map((v) => ({ value: v, label: v }))} />
      </Field>
      <Field label="Sub-type">
        <Select style={{ width: '100%' }} allowClear disabled={readOnly} value={d.claimSubType ?? undefined} onChange={(v) => patch({ claimSubType: v ?? null })} options={SUB_TYPES} />
      </Field>
      <Field label="Patient" required>
        <PatientSelect disabled={readOnly || (!isNew && !!loaded.data?.nphiesStatus && loaded.data.nphiesStatus !== 'Draft')} value={d.patientId || undefined} onChange={(v) => patch({ patientId: v ?? '', coverageId: '', encounterId: null })} />
      </Field>
      <Field label="Coverage / policy" required>
        <CoverageSelect patientId={d.patientId || undefined} disabled={readOnly} value={d.coverageId || undefined} onChange={(v) => patch({ coverageId: v ?? '' })} />
      </Field>
      <Field label="Insurer" required>
        <InsurerSelect disabled={readOnly} value={d.insurerId || undefined} onChange={(v) => patch({ insurerId: v ?? '' })} />
      </Field>
      <Field label="Provider / hospital" required>
        <ProviderSelect disabled={readOnly} value={d.providerId || undefined} onChange={(v) => patch({ providerId: v ?? '', locationId: null })} />
      </Field>
      <Field label="Location">
        <LocationSelect organizationId={d.providerId || undefined} disabled={readOnly} value={d.locationId ?? undefined} onChange={(v) => patch({ locationId: v ?? null })} />
      </Field>
      <Field label="Encounter">
        <EncounterSelect patientId={d.patientId || undefined} disabled={readOnly} value={d.encounterId ?? undefined} onChange={(v) => patch({ encounterId: v ?? null })} />
      </Field>
      <Field label="Attending practitioner">
        <PractitionerSelect disabled={readOnly} value={d.practitionerId ?? undefined} onChange={(v) => patch({ practitionerId: v ?? null })} />
      </Field>
      <Field label="Priority">
        <Select style={{ width: '100%' }} disabled={readOnly} value={d.priority} onChange={(v) => patch({ priority: v })} options={PRIORITIES.map((v) => ({ value: v, label: v }))} />
      </Field>
      <Field label="Payee">
        <Select style={{ width: '100%' }} disabled={readOnly} value={d.payeeType ?? 'provider'} onChange={(v) => patch({ payeeType: v })} options={[{ value: 'provider', label: 'provider' }, { value: 'subscriber', label: 'subscriber' }]} />
      </Field>
      <Field label="Billable period start">
        <DateField disabled={readOnly} value={d.billablePeriodStart} placeholder={d.servicedPeriodStart ? 'uses service period' : undefined} onChange={(v) => patch({ billablePeriodStart: v })} />
      </Field>
      <Field label="Billable period end">
        <DateField disabled={readOnly} value={d.billablePeriodEnd} placeholder={d.servicedPeriodEnd ? 'uses service period' : undefined} onChange={(v) => patch({ billablePeriodEnd: v })} />
      </Field>
      <Field label="Service period start">
        <DateField disabled={readOnly} value={d.servicedPeriodStart} onChange={(v) => patch({ servicedPeriodStart: v })} />
      </Field>
      <Field label="Service period end">
        <DateField disabled={readOnly} value={d.servicedPeriodEnd} onChange={(v) => patch({ servicedPeriodEnd: v })} />
      </Field>
      {!isPreauth && (
        <Field label="Pre-authorization ref">
          <Input disabled={readOnly} value={d.preAuthorizationRef ?? ''} onChange={(e) => patch({ preAuthorizationRef: e.target.value || null })} placeholder="preAuthRef from the approved pre-auth" />
        </Field>
      )}
      <Field label="Episode identifier">
        <Input disabled={readOnly} value={d.episodeIdentifierValue ?? ''} onChange={(e) => patch({ episodeIdentifierValue: e.target.value || null })} />
      </Field>
      <Field label="Referral identifier">
        <Input disabled={readOnly} value={d.referralIdentifier ?? ''} onChange={(e) => patch({ referralIdentifier: e.target.value || null })} />
      </Field>
      {(d.claimType === 'pharmacy' || d.claimType === 'vision') && (
        <Field label="Prescription identifier">
          <Input disabled={readOnly} value={d.prescriptionIdentifier ?? ''} onChange={(e) => patch({ prescriptionIdentifier: e.target.value || null })} />
        </Field>
      )}
      <Field label="Accident date">
        <DateField disabled={readOnly} value={d.accidentDate} onChange={(v) => patch({ accidentDate: v })} />
      </Field>
      <Field label="Accident type">
        <Select style={{ width: '100%' }} allowClear disabled={readOnly} value={d.accidentType ?? undefined} onChange={(v) => patch({ accidentType: v ?? null })} options={['MVA', 'SCHOOL', 'SPT', 'WPA'].map((v) => ({ value: v, label: v }))} />
      </Field>
      <Field label="Eligibility offline ref">
        <Input disabled={readOnly} value={d.eligibilityOfflineReference ?? ''} onChange={(e) => patch({ eligibilityOfflineReference: e.target.value || null })} />
      </Field>
      <Field label="Flags">
        <Space>
          <Checkbox disabled={readOnly} checked={d.isShadowBilling} onChange={(e) => patch({ isShadowBilling: e.target.checked })}>
            Shadow billing
          </Checkbox>
          {d.claimSubType === 'ip' && (
            <Checkbox disabled={readOnly} checked={!!d.isTransfer} onChange={(e) => patch({ isTransfer: e.target.checked })}>
              Transfer
            </Checkbox>
          )}
        </Space>
      </Field>
    </Row>
  );

  const lensEditor = (item: ClaimEditItem, idx: number) => {
    const setLens = (li: number, p: Partial<ClaimEditLens>) =>
      patchRow('items', idx, { lenses: item.lenses.map((l, i) => (i === li ? { ...l, ...p } : l)) });
    return (
      <div style={{ padding: 8 }}>
        <Table
          size="small"
          pagination={false}
          rowKey={(_, i) => String(i)}
          dataSource={item.lenses}
          columns={[
            { title: 'Eye', render: (_, l, i) => <Select size="small" disabled={readOnly} style={{ width: 90 }} value={l.eye} onChange={(v) => setLens(i, { eye: v })} options={[{ value: 'right', label: 'right' }, { value: 'left', label: 'left' }]} /> },
            { title: 'Product', render: (_, l, i) => <Select size="small" disabled={readOnly} style={{ width: 100 }} value={l.product} onChange={(v) => setLens(i, { product: v })} options={[{ value: 'lens', label: 'lens' }, { value: 'contact', label: 'contact' }]} /> },
            ...(['sphere', 'cylinder', 'axis', 'prism', 'add', 'power'] as const).map((k) => ({
              title: k,
              render: (_: unknown, l: ClaimEditLens, i: number) => (
                <InputNumber size="small" disabled={readOnly} style={{ width: 80 }} value={l[k] ?? null} onChange={(v) => setLens(i, { [k]: v })} />
              ),
            })),
            { title: 'Prism base', render: (_, l, i) => <Select size="small" allowClear disabled={readOnly} style={{ width: 80 }} value={l.prismBase ?? undefined} onChange={(v) => setLens(i, { prismBase: v ?? null })} options={['up', 'down', 'in', 'out'].map((v) => ({ value: v, label: v }))} /> },
            { title: '', render: (_, __, i) => !readOnly && <Button size="small" type="text" danger icon={<DeleteOutlined />} onClick={() => patchRow('items', idx, { lenses: item.lenses.filter((_, j) => j !== i) })} /> },
          ]}
        />
        {!readOnly && (
          <Button size="small" icon={<PlusOutlined />} style={{ marginTop: 8 }} onClick={() => patchRow('items', idx, { lenses: [...item.lenses, { eye: 'right', product: 'lens' }] })}>
            Add lens
          </Button>
        )}
      </div>
    );
  };

  const items = (
    <>
      <Table<ClaimEditItem>
        size="small"
        rowKey="sequence"
        pagination={false}
        scroll={{ x: 'max-content' }}
        dataSource={d.items}
        expandable={d.claimType === 'vision' ? { expandedRowRender: lensEditor, rowExpandable: () => true } : undefined}
        columns={[
          { title: '#', dataIndex: 'sequence', width: 40 },
          {
            title: 'Code system',
            render: (_, r, i) => (
              <Select size="small" disabled={readOnly} style={{ width: 170 }} value={r.productOrServiceSystem ?? undefined} onChange={(v) => patchRow('items', i, { productOrServiceSystem: v })} options={CODE_SYSTEMS} placeholder="system" />
            ),
          },
          { title: 'Code *', render: (_, r, i) => <Input size="small" disabled={readOnly} style={{ width: 120 }} value={r.productOrServiceCode} onChange={(e) => patchRow('items', i, { productOrServiceCode: e.target.value })} /> },
          { title: 'Description', render: (_, r, i) => <Input size="small" disabled={readOnly} style={{ width: 200 }} value={r.productOrServiceDisplay ?? ''} onChange={(e) => patchRow('items', i, { productOrServiceDisplay: e.target.value })} /> },
          { title: 'Service date', render: (_, r, i) => <div style={{ width: 130 }}><DateField disabled={readOnly} value={r.servicedDate} onChange={(v) => patchRow('items', i, { servicedDate: v })} /></div> },
          { title: 'Qty', render: (_, r, i) => <InputNumber size="small" disabled={readOnly} min={0} style={{ width: 70 }} value={r.quantity} onChange={(v) => patchRow('items', i, { quantity: v ?? 0 })} /> },
          { title: 'Unit price', render: (_, r, i) => <InputNumber size="small" disabled={readOnly} min={0} step={0.01} style={{ width: 100 }} value={r.unitPrice} onChange={(v) => patchRow('items', i, { unitPrice: v ?? 0 })} /> },
          { title: 'Factor', render: (_, r, i) => <InputNumber size="small" disabled={readOnly} min={0} max={1} step={0.05} style={{ width: 70 }} value={r.factor ?? 1} onChange={(v) => patchRow('items', i, { factor: v ?? 1 })} /> },
          { title: 'VAT %', render: (_, r, i) => <InputNumber size="small" disabled={readOnly} min={0} max={100} style={{ width: 70 }} value={r.taxRate ?? 0} onChange={(v) => patchRow('items', i, { taxRate: v ?? 0 })} /> },
          { title: 'VAT', align: 'right', render: (_, r) => fmtMoney(calcItem(r).tax) },
          { title: 'Net', align: 'right', render: (_, r) => <b>{fmtMoney(calcItem(r).net)}</b> },
          { title: 'Patient share', render: (_, r, i) => <InputNumber size="small" disabled={readOnly} min={0} step={0.01} style={{ width: 90 }} value={r.patientShare ?? 0} onChange={(v) => patchRow('items', i, { patientShare: v ?? 0 })} /> },
          { title: 'Care team', render: (_, r, i) => <Select size="small" allowClear disabled={readOnly} style={{ width: 120 }} value={r.careTeamSequence ?? undefined} onChange={(v) => patchRow('items', i, { careTeamSequence: v ?? null })} options={careOptions} /> },
          { title: 'Diagnoses', render: (_, r, i) => <Select size="small" mode="multiple" disabled={readOnly} style={{ width: 160 }} value={r.diagnosisSequences} onChange={(v) => patchRow('items', i, { diagnosisSequences: v })} options={diagOptions} /> },
          { title: 'Supporting info', render: (_, r, i) => <Select size="small" mode="multiple" disabled={readOnly} style={{ width: 160 }} value={r.informationSequences} onChange={(v) => patchRow('items', i, { informationSequences: v })} options={infoOptions} /> },
          ...(d.claimType === 'oral'
            ? [
                { title: 'Tooth', render: (_: unknown, r: ClaimEditItem, i: number) => <Input size="small" disabled={readOnly} style={{ width: 70 }} value={r.bodySiteCode ?? ''} onChange={(e) => patchRow('items', i, { bodySiteCode: e.target.value || null })} /> },
                { title: 'Surface', render: (_: unknown, r: ClaimEditItem, i: number) => <Input size="small" disabled={readOnly} style={{ width: 70 }} value={r.subSiteCode ?? ''} onChange={(e) => patchRow('items', i, { subSiteCode: e.target.value || null })} /> },
              ]
            : [
                { title: 'Body site', render: (_: unknown, r: ClaimEditItem, i: number) => <Input size="small" disabled={readOnly} style={{ width: 80 }} value={r.bodySiteCode ?? ''} onChange={(e) => patchRow('items', i, { bodySiteCode: e.target.value || null })} /> },
              ]),
          { title: 'Package', render: (_, r, i) => <Switch size="small" disabled={readOnly} checked={r.isPackage} onChange={(v) => patchRow('items', i, { isPackage: v })} /> },
          { title: 'Maternity', render: (_, r, i) => <Switch size="small" disabled={readOnly} checked={r.isMaternity} onChange={(v) => patchRow('items', i, { isMaternity: v })} /> },
          {
            title: '',
            fixed: 'right',
            render: (_, __, i) => !readOnly && <Button size="small" type="text" danger icon={<DeleteOutlined />} onClick={() => removeRow('items', i)} />,
          },
        ]}
        summary={() => (
          <Table.Summary.Row>
            <Table.Summary.Cell index={0} colSpan={9}>
              <b>Totals</b> ({d.items.length} lines)
            </Table.Summary.Cell>
            <Table.Summary.Cell index={9} align="right">{fmtMoney(totals.tax)}</Table.Summary.Cell>
            <Table.Summary.Cell index={10} align="right"><b>{fmtMoney(totals.net)}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={11} align="right">{fmtMoney(totals.share)}</Table.Summary.Cell>
          </Table.Summary.Row>
        )}
      />
      {!readOnly && (
        <Button
          icon={<PlusOutlined />}
          style={{ marginTop: 8 }}
          onClick={() =>
            patch({
              items: [
                ...d.items,
                {
                  sequence: nextSeq(d.items),
                  productOrServiceCode: '',
                  productOrServiceSystem: CODE_SYSTEMS[0].value,
                  servicedDate: d.billablePeriodStart ?? dayjs().format('YYYY-MM-DD'),
                  quantity: 1,
                  unitPrice: 0,
                  factor: 1,
                  taxRate: 0,
                  patientShare: 0,
                  careTeamSequence: d.careTeam[0]?.sequence ?? null,
                  diagnosisSequences: d.diagnoses[0] ? [d.diagnoses[0].sequence] : [],
                  informationSequences: [],
                  isPackage: false,
                  isMaternity: false,
                  lenses: [],
                },
              ],
            })
          }
        >
          Add item
        </Button>
      )}
    </>
  );

  const diagnoses = (
    <>
      <Table<ClaimEditDiagnosis>
        size="small"
        rowKey="sequence"
        pagination={false}
        dataSource={d.diagnoses}
        columns={[
          { title: '#', dataIndex: 'sequence', width: 40 },
          { title: 'ICD-10-AM code *', width: 260, render: (_, r, i) => <DiagnosisCodeInput disabled={readOnly} value={r.diagnosisCode} onChange={(code, description) => patchRow('diagnoses', i, description === undefined ? { diagnosisCode: code } : { diagnosisCode: code, diagnosisDisplay: description })} /> },
          { title: 'Description', render: (_, r, i) => <Input size="small" disabled={readOnly} value={r.diagnosisDisplay ?? ''} onChange={(e) => patchRow('diagnoses', i, { diagnosisDisplay: e.target.value })} /> },
          { title: 'Type', render: (_, r, i) => <Select size="small" disabled={readOnly} style={{ width: 130 }} value={r.diagnosisType ?? undefined} onChange={(v) => patchRow('diagnoses', i, { diagnosisType: v })} options={DIAGNOSIS_TYPES.map((v) => ({ value: v, label: v }))} /> },
          ...(d.claimSubType === 'ip'
            ? [{ title: 'On admission', render: (_: unknown, r: ClaimEditDiagnosis, i: number) => <Select size="small" allowClear disabled={readOnly} style={{ width: 100 }} value={r.onAdmissionCode ?? undefined} onChange={(v) => patchRow('diagnoses', i, { onAdmissionCode: v ?? null })} options={['y', 'n', 'u'].map((v) => ({ value: v, label: v }))} /> }]
            : []),
          { title: '', render: (_, __, i) => !readOnly && <Button size="small" type="text" danger icon={<DeleteOutlined />} onClick={() => removeRow('diagnoses', i)} /> },
        ]}
      />
      {!readOnly && (
        <Button
          icon={<PlusOutlined />}
          style={{ marginTop: 8 }}
          onClick={() => patch({ diagnoses: [...d.diagnoses, { sequence: nextSeq(d.diagnoses), diagnosisCode: '', diagnosisType: d.diagnoses.length ? 'secondary' : 'principal' }] })}
        >
          Add diagnosis
        </Button>
      )}
    </>
  );

  const careTeam = (
    <>
      <Table<ClaimEditCareTeam>
        size="small"
        rowKey="sequence"
        pagination={false}
        dataSource={d.careTeam}
        columns={[
          { title: '#', dataIndex: 'sequence', width: 40 },
          {
            title: 'Practitioner *',
            render: (_, r, i) => (
              <div style={{ minWidth: 260 }}>
                {readOnly ? r.practitionerName ?? r.practitionerId : <PractitionerSelect value={r.practitionerId || undefined} onChange={(v) => patchRow('careTeam', i, { practitionerId: v ?? '' })} />}
              </div>
            ),
          },
          { title: 'Role', render: (_, r, i) => <Select size="small" disabled={readOnly} style={{ width: 120 }} value={r.role ?? undefined} onChange={(v) => patchRow('careTeam', i, { role: v })} options={CARE_ROLES.map((v) => ({ value: v, label: v }))} /> },
          { title: 'Qualification (specialty code)', render: (_, r, i) => <Input size="small" disabled={readOnly} value={r.qualification ?? ''} onChange={(e) => patchRow('careTeam', i, { qualification: e.target.value || null })} placeholder="e.g. 08.00" /> },
          { title: '', render: (_, __, i) => !readOnly && <Button size="small" type="text" danger icon={<DeleteOutlined />} onClick={() => removeRow('careTeam', i)} /> },
        ]}
      />
      {!readOnly && (
        <Button
          icon={<PlusOutlined />}
          style={{ marginTop: 8 }}
          onClick={() => patch({ careTeam: [...d.careTeam, { sequence: nextSeq(d.careTeam), practitionerId: d.practitionerId ?? '', role: d.careTeam.length ? 'assist' : 'primary' }] })}
        >
          Add care-team member
        </Button>
      )}
    </>
  );

  const readFile = (file: File, i: number) => {
    const reader = new FileReader();
    reader.onload = () => {
      const base64 = String(reader.result).split(',')[1] ?? '';
      patchRow('supportingInfo', i, {
        attachmentData: base64,
        attachmentContentType: file.type || 'application/octet-stream',
        attachmentTitle: file.name,
        hasAttachment: true,
        removeAttachment: false,
      });
    };
    reader.readAsDataURL(file);
    return false;
  };

  const supporting = (
    <>
      <Table<ClaimEditSupportingInfo>
        size="small"
        rowKey="sequence"
        pagination={false}
        scroll={{ x: 'max-content' }}
        dataSource={d.supportingInfo}
        columns={[
          { title: '#', dataIndex: 'sequence', width: 40 },
          { title: 'Category *', render: (_, r, i) => <Select size="small" showSearch disabled={readOnly} style={{ width: 190 }} value={r.category} onChange={(v) => patchRow('supportingInfo', i, { category: v })} options={INFO_CATEGORIES.map((v) => ({ value: v, label: v }))} /> },
          { title: 'Code', render: (_, r, i) => <Input size="small" disabled={readOnly} style={{ width: 110 }} value={r.codeValue ?? ''} onChange={(e) => patchRow('supportingInfo', i, { codeValue: e.target.value || null })} /> },
          { title: 'Text', render: (_, r, i) => <Input size="small" disabled={readOnly} style={{ width: 200 }} value={r.stringValue ?? ''} onChange={(e) => patchRow('supportingInfo', i, { stringValue: e.target.value || null })} /> },
          { title: 'Value', render: (_, r, i) => <InputNumber size="small" disabled={readOnly} style={{ width: 90 }} value={r.quantityValue ?? null} onChange={(v) => patchRow('supportingInfo', i, { quantityValue: v })} /> },
          { title: 'Unit', render: (_, r, i) => <Input size="small" disabled={readOnly} style={{ width: 70 }} value={r.quantityUnit ?? ''} onChange={(e) => patchRow('supportingInfo', i, { quantityUnit: e.target.value || null })} /> },
          { title: 'Date', render: (_, r, i) => <div style={{ width: 130 }}><DateField disabled={readOnly} value={r.dateValue} onChange={(v) => patchRow('supportingInfo', i, { dateValue: v })} /></div> },
          {
            title: 'Attachment',
            render: (_, r, i) => (
              <Space>
                {r.hasAttachment && !r.removeAttachment ? (
                  <Tag icon={<PaperClipOutlined />}>{r.attachmentTitle ?? 'file'}</Tag>
                ) : (
                  <Typography.Text type="secondary">none</Typography.Text>
                )}
                {!readOnly && (
                  <Upload showUploadList={false} beforeUpload={(f) => readFile(f, i)}>
                    <Button size="small">Upload</Button>
                  </Upload>
                )}
                {!readOnly && r.hasAttachment && !r.removeAttachment && (
                  <Button size="small" type="link" danger onClick={() => patchRow('supportingInfo', i, { removeAttachment: true, attachmentData: null })}>
                    Remove
                  </Button>
                )}
              </Space>
            ),
          },
          { title: '', render: (_, __, i) => !readOnly && <Button size="small" type="text" danger icon={<DeleteOutlined />} onClick={() => removeRow('supportingInfo', i)} /> },
        ]}
      />
      {!readOnly && (
        <Button icon={<PlusOutlined />} style={{ marginTop: 8 }} onClick={() => patch({ supportingInfo: [...d.supportingInfo, { sequence: nextSeq(d.supportingInfo), category: 'info' }] })}>
          Add supporting info
        </Button>
      )}
    </>
  );

  const h = health.data;
  const errorsFor = (prefix: string) => h?.issues.filter((i) => i.severity === 'Error' && i.field?.startsWith(prefix)).length ?? 0;
  const tabLabel = (label: string, prefix: string, count?: number) => (
    <Badge count={errorsFor(prefix)} size="small" offset={[8, -2]}>
      {label}
      {count !== undefined ? ` (${count})` : ''}
    </Badge>
  );

  return (
    <Drawer
      open
      width="92%"
      onClose={() => {
        if (dirty && !window.confirm('Discard unsaved changes?')) return;
        onClose();
      }}
      destroyOnClose
      title={
        <Space wrap>
          <span>{isNew ? `New ${isPreauth ? 'prior authorization' : 'claim'}` : `Edit ${d.claimNumber ?? ''}`}</span>
          {d.nphiesStatus && statusTag(d.nphiesStatus)}
          {dirty && <Tag color="gold">unsaved</Tag>}
        </Space>
      }
      extra={
        <Space>
          {id && (
            <Button icon={<CodeOutlined />} disabled={dirty} onClick={() => setPreview(true)}>
              Preview request JSON
            </Button>
          )}
          {!readOnly && (
            <Button type="primary" icon={<SaveOutlined />} loading={save.isPending} disabled={!dirty && !isNew} onClick={() => save.mutate(d)}>
              Save
            </Button>
          )}
        </Space>
      }
    >
      {readOnly && (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="This record is locked"
          description={loaded.data?.lockedReason ?? 'You do not have permission to edit claims.'}
        />
      )}
      {serverErrors && <Alert type="error" showIcon closable style={{ marginBottom: 12 }} message={serverErrors} onClose={() => setServerErrors(undefined)} />}

      <Row gutter={16}>
        <Col xs={24} xl={18}>
          <Tabs
            items={[
              { key: 'header', label: tabLabel('Header', 'header'), children: header },
              { key: 'items', label: tabLabel('Items', 'items', d.items.length), children: items },
              { key: 'diagnoses', label: tabLabel('Diagnoses', 'diagnos', d.diagnoses.length), children: diagnoses },
              { key: 'careteam', label: tabLabel('Care team', 'careTeam', d.careTeam.length), children: careTeam },
              { key: 'supporting', label: tabLabel('Supporting info', 'supportingInfo', d.supportingInfo.length), children: supporting },
              ...(id
                ? [
                    {
                      key: 'documents',
                      label: (
                        <span>
                          <PaperClipOutlined /> Documents
                        </span>
                      ),
                      children: dirty ? (
                        <Alert type="info" showIcon message="Save or discard your changes first, then attach documents here." />
                      ) : (
                        <ClaimDocumentsTab claimId={id} nphiesStatus={loaded.data?.nphiesStatus} />
                      ),
                    },
                  ]
                : []),
            ]}
          />
        </Col>
        <Col xs={24} xl={6}>
          <Descriptions size="small" bordered column={1} style={{ marginBottom: 12 }}>
            <Descriptions.Item label="Gross">{fmtMoney(totals.gross)}</Descriptions.Item>
            <Descriptions.Item label="VAT">{fmtMoney(totals.tax)}</Descriptions.Item>
            <Descriptions.Item label="Net total">
              <b>{fmtMoney(totals.net)}</b>
            </Descriptions.Item>
            <Descriptions.Item label="Patient share">{fmtMoney(totals.share)}</Descriptions.Item>
            {d.technicalReviewStatus && <Descriptions.Item label="Technical review">{statusTag(d.technicalReviewStatus)}</Descriptions.Item>}
            {d.medicalReviewStatus && <Descriptions.Item label="Medical review">{statusTag(d.medicalReviewStatus)}</Descriptions.Item>}
          </Descriptions>
          <Typography.Title level={5}>
            <SafetyCertificateOutlined /> Health check{' '}
            {h && (
              <Typography.Text type="secondary" style={{ fontSize: 12, fontWeight: 400 }}>
                {h.errorCount} errors · {h.warningCount} warnings
              </Typography.Text>
            )}
          </Typography.Title>
          {dirty && h && <Alert type="warning" showIcon style={{ marginBottom: 8 }} message="Results are from the last save." />}
          <HealthPanel health={h} loading={health.isFetching && !h} />
          {!readOnly && (
            <div style={{ marginTop: 12 }}>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                Change reason (kept in the audit trail)
              </Typography.Text>
              <Input.TextArea rows={2} value={d.changeReason ?? ''} onChange={(e) => patch({ changeReason: e.target.value })} />
            </div>
          )}
          {!id && <Empty style={{ marginTop: 16 }} image={Empty.PRESENTED_IMAGE_SIMPLE} description="Save to get a claim number, health check and request JSON." />}
        </Col>
      </Row>

      {preview && id && <BundlePreviewModal claimId={id} onClose={() => setPreview(false)} />}
    </Drawer>
  );
}
