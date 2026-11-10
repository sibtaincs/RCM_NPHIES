import { useState } from 'react';
import { Alert, AutoComplete, Card, Descriptions, Modal, Select, Space, Spin, Statistic, Tooltip, Typography } from 'antd';
import type { ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { explorerApi } from '@/api/endpoints/explorer';
import { useDebounce } from '@/hooks/useDebounce';
import { useInsurers } from '@/hooks/useGlobalFilters';
import type { ExplorerRow } from '@/types/explorer';
import type { SubmissionResult, TaskResult } from '@/types/rcm';
import { getApiErrorMessage } from '@/utils/apiError';
import { fmtMoney, statusTag } from '@/utils/display';

/** Page title + right-aligned actions. */
export function PageHeader({ title, extra }: { title: string; extra?: ReactNode }) {
  return (
    <Space style={{ width: '100%', justifyContent: 'space-between', marginBottom: 16 }} wrap>
      <Typography.Title level={3} style={{ margin: 0 }}>
        {title}
      </Typography.Title>
      {extra}
    </Space>
  );
}

export function Kpi({ title, value, suffix, precision, color }: { title: string; value?: number | null; suffix?: string; precision?: number; color?: string }) {
  return (
    <Card size="small">
      <Statistic title={title} value={value ?? 0} suffix={suffix} precision={precision} valueStyle={color ? { color } : undefined} />
    </Card>
  );
}

/** Shows the NPHIES outcome of a submission / task operation. */
export function showOperationResult(title: string, result: SubmissionResult | TaskResult) {
  const errors = result.errors ?? [];
  const submission = 'use' in result ? result : undefined;
  const task = 'taskStatus' in result ? result : undefined;
  const ok = errors.length === 0 && !/error|fail/i.test(result.transactionStatus);
  (ok ? Modal.success : Modal.warning)({
    title,
    width: 640,
    content: (
      <>
        <Descriptions size="small" column={1} bordered style={{ marginTop: 12 }}>
          <Descriptions.Item label="Transaction">{statusTag(result.transactionStatus)}</Descriptions.Item>
          {result.claimStatus && <Descriptions.Item label="Claim status">{statusTag(result.claimStatus)}</Descriptions.Item>}
          {submission?.outcome && <Descriptions.Item label="Outcome">{statusTag(submission.outcome)}</Descriptions.Item>}
          {submission?.adjudicationOutcome && (
            <Descriptions.Item label="Adjudication">{statusTag(submission.adjudicationOutcome)}</Descriptions.Item>
          )}
          {submission?.adjudication && (
            <Descriptions.Item label="Benefit / Patient share">
              {fmtMoney(submission.adjudication.totalBenefit)} / {fmtMoney(submission.adjudication.totalPatientShare)}
            </Descriptions.Item>
          )}
          {task?.taskStatus && <Descriptions.Item label="Task status">{statusTag(task.taskStatus)}</Descriptions.Item>}
          {task?.processingStatus && <Descriptions.Item label="Processing">{task.processingStatus}</Descriptions.Item>}
          {result.messageId && <Descriptions.Item label="Message id">{result.messageId}</Descriptions.Item>}
        </Descriptions>
        {errors.length > 0 && (
          <Alert type="error" style={{ marginTop: 12 }} message="NPHIES errors" description={<ul>{errors.map((e) => <li key={e}>{e}</li>)}</ul>} />
        )}
      </>
    ),
  });
}

export function showError(title: string, error: unknown) {
  Modal.error({ title, content: getApiErrorMessage(error) });
}

function rowLabel(r: ExplorerRow, keys: string[]): string {
  return keys
    .map((k) => r[k])
    .filter((v) => v !== null && v !== undefined && v !== '')
    .join(' · ');
}

/** Generic remote-search select over an explorer table. */
export function RemoteSelect({
  table,
  labelKeys,
  value,
  onChange,
  placeholder,
  filters,
  disabled,
}: {
  table: string;
  labelKeys: string[];
  value?: string;
  onChange?: (v?: string) => void;
  placeholder: string;
  filters?: Record<string, string | undefined>;
  disabled?: boolean;
}) {
  const [search, setSearch] = useState('');
  const term = useDebounce(search, 300);
  const { data, isFetching } = useQuery({
    queryKey: ['remote-select', table, term, filters],
    queryFn: () => explorerApi.query(table, { search: term || undefined, pageSize: 20, filters }),
    enabled: !disabled,
  });
  const inList = !!value && (data?.items ?? []).some((r) => String(r.id) === value);
  const selected = useQuery({
    queryKey: ['explorer-record', table, value],
    queryFn: () => explorerApi.get(table, value!),
    enabled: !!value && !inList,
    staleTime: 5 * 60_000,
  });
  const options = (data?.items ?? []).map((r) => ({ value: String(r.id), label: rowLabel(r, labelKeys) || String(r.id) }));
  if (value && !inList && selected.data?.row) {
    options.unshift({ value, label: rowLabel(selected.data.row, labelKeys) || value });
  }
  return (
    <Select
      showSearch
      allowClear
      disabled={disabled}
      filterOption={false}
      loading={isFetching}
      placeholder={placeholder}
      value={value}
      onSearch={setSearch}
      onChange={(v) => onChange?.(v)}
      options={options}
    />
  );
}

export function PatientSelect(props: { value?: string; onChange?: (v?: string) => void; disabled?: boolean }) {
  return <RemoteSelect table="Patients" labelKeys={['fullName', 'nationalId', 'mRN']} placeholder="Search patient by name / ID / MRN" {...props} />;
}

export function CoverageSelect(props: { patientId?: string; value?: string; onChange?: (v?: string) => void; disabled?: boolean }) {
  return (
    <RemoteSelect
      table="Coverages"
      labelKeys={['policyNumber', 'memberID', 'planCode', 'coverageType']}
      placeholder={props.patientId ? 'Select coverage (optional)' : 'Pick a patient first'}
      filters={{ patientId: props.patientId }}
      disabled={!props.patientId || props.disabled}
      value={props.value}
      onChange={props.onChange}
    />
  );
}

export function InsurerSelect({ value, onChange, placeholder = 'Insurer', disabled }: { value?: string; onChange?: (v?: string) => void; placeholder?: string; disabled?: boolean }) {
  const { data = [], isLoading } = useInsurers();
  return (
    <Select
      allowClear
      showSearch
      optionFilterProp="label"
      loading={isLoading}
      placeholder={placeholder}
      value={value}
      onChange={onChange}
      disabled={disabled}
      style={{ minWidth: 200 }}
      options={data.map((i) => ({ value: i.id, label: i.name }))}
    />
  );
}

type SelectProps = { value?: string; onChange?: (v?: string) => void; disabled?: boolean };

export function PractitionerSelect(props: SelectProps) {
  return (
    <RemoteSelect
      table="Practitioners"
      labelKeys={['firstName', 'lastName', 'practitionerLicenseNumber', 'specialization']}
      placeholder="Search practitioner"
      {...props}
    />
  );
}

export function ProviderSelect(props: SelectProps) {
  return (
    <RemoteSelect
      table="Organizations"
      labelKeys={['organizationName', 'licenseNumber']}
      placeholder="Provider / hospital"
      filters={{ organizationType: 'prov' }}
      {...props}
    />
  );
}

export function EncounterSelect(props: SelectProps & { patientId?: string }) {
  return (
    <RemoteSelect
      table="Encounters"
      labelKeys={['identifierValue', 'class', 'periodStart']}
      placeholder={props.patientId ? 'Encounter (optional)' : 'Pick a patient first'}
      filters={{ patientId: props.patientId }}
      disabled={!props.patientId || props.disabled}
      value={props.value}
      onChange={props.onChange}
    />
  );
}

export function LocationSelect(props: SelectProps & { organizationId?: string }) {
  return (
    <RemoteSelect
      table="Locations"
      labelKeys={['locationName', 'locationLicense']}
      placeholder="Location (optional)"
      filters={{ organizationId: props.organizationId }}
      value={props.value}
      onChange={props.onChange}
      disabled={props.disabled}
    />
  );
}

/**
 * ICD-10-AM code input backed by the DiagnosisCodeMasters table: type a code or words of the name, pick a row to
 * fill code + description (works for adding a new row or replacing an existing code). Free text is still accepted
 * for codes not yet in the master, flagged with a warning.
 */
export function DiagnosisCodeInput({
  value,
  onChange,
  disabled,
}: {
  value?: string;
  onChange: (code: string, description?: string) => void;
  disabled?: boolean;
}) {
  const [search, setSearch] = useState('');
  const term = useDebounce(search, 250);
  const { data, isFetching } = useQuery({
    queryKey: ['diagnosis-master', term],
    queryFn: () => explorerApi.query('DiagnosisCodeMasters', { search: term || undefined, pageSize: 20, filters: { isActive: 'true' } }),
    enabled: !disabled,
    staleTime: 5 * 60_000,
  });
  const exact = useQuery({
    queryKey: ['diagnosis-master-exact', value],
    queryFn: () => explorerApi.query('DiagnosisCodeMasters', { search: value, pageSize: 5 }),
    enabled: !!value && !disabled,
    staleTime: 5 * 60_000,
  });
  const codeOf = (r: ExplorerRow) => String(r.nphiesDiagnosisCode || r.diagnosisCode || '');
  const rows = data?.items ?? [];
  const known = !value || (exact.data?.items ?? []).some((r) => codeOf(r).toUpperCase() === value.toUpperCase());
  return (
    <Tooltip title={known || exact.isFetching ? undefined : 'Code not found in the diagnosis master'}>
      <AutoComplete
        size="small"
        style={{ width: '100%' }}
        disabled={disabled}
        value={value}
        status={known || exact.isFetching ? undefined : 'warning'}
        placeholder="Search code or diagnosis name"
        popupMatchSelectWidth={480}
        notFoundContent={isFetching ? <Spin size="small" /> : 'No matching diagnosis in master'}
        onSearch={setSearch}
        onChange={(v: string) => onChange((v ?? '').toUpperCase())}
        onSelect={(v: string) => {
          const row = rows.find((r) => codeOf(r) === v);
          onChange(v, row ? String(row.diagnosisName ?? '') : undefined);
        }}
        options={rows.map((r) => ({
          value: codeOf(r),
          label: (
            <span>
              <b>{codeOf(r)}</b> · {String(r.diagnosisName ?? '')}
              {r.diagnosisCategory ? <Typography.Text type="secondary"> ({String(r.diagnosisCategory)})</Typography.Text> : null}
            </span>
          ),
        }))}
      />
    </Tooltip>
  );
}