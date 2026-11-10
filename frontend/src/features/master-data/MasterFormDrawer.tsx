import { useMemo, useState } from 'react';
import {
  Alert,
  Button,
  DatePicker,
  Drawer,
  Form,
  Input,
  InputNumber,
  Popconfirm,
  Select,
  Space,
  Spin,
  Switch,
  Typography,
  message,
} from 'antd';
import { DeleteOutlined, SaveOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs, { type Dayjs } from 'dayjs';
import { getMasterFieldErrors, mastersApi } from '@/api/endpoints/masters';
import { useDebounce } from '@/hooks/useDebounce';
import { useHospitalLocked } from '@/hooks/useGlobalFilters';
import type { ExplorerRow } from '@/types/explorer';
import type { MasterField, MasterTableInfo, MasterValues } from '@/types/masters';
import { getApiErrorMessage } from '@/utils/apiError';

interface Props {
  master: MasterTableInfo;
  /** Row id to edit; undefined creates a new row. */
  id?: string;
  /** Default hospital for new rows (global filter). */
  defaultHospitalId?: string;
  onClose: () => void;
}

function parseFilter(filter?: string | null): Record<string, string> {
  if (!filter) return {};
  const [k, v] = filter.split('=');
  return k && v ? { [k]: v } : {};
}

interface LookupSelectProps {
  field: MasterField;
  hospitalId?: string;
  value?: string | null;
  onChange?: (v: string | null) => void;
  disabled?: boolean;
}

/** Searchable select over `api/masters/lookup/{table}`; also resolves the label of the current value. */
function LookupSelect({ field, hospitalId, value, onChange, disabled }: LookupSelectProps) {
  const [search, setSearch] = useState('');
  const debounced = useDebounce(search, 300);
  const filters = parseFilter(field.lookupFilter);
  const table = field.lookup!;

  const list = useQuery({
    queryKey: ['master-lookup', table, debounced, hospitalId, filters],
    queryFn: () => mastersApi.lookup(table, { search: debounced, hospitalId, filters, take: 50 }),
    staleTime: 60_000,
  });
  const current = useQuery({
    queryKey: ['master-lookup-value', table, value],
    queryFn: () => mastersApi.lookup(table, { filters: { id: value! }, take: 1 }),
    enabled: !!value && !list.data?.some((o) => o.value === value),
    staleTime: 5 * 60_000,
  });

  const options = useMemo(() => {
    const items = [...(list.data ?? [])];
    for (const c of current.data ?? []) if (!items.some((o) => o.value === c.value)) items.unshift(c);
    return items.map((o) => ({ value: o.value, label: o.label }));
  }, [list.data, current.data]);

  return (
    <Select
      showSearch
      allowClear
      disabled={disabled}
      value={value || undefined}
      onChange={(v) => onChange?.(v ?? null)}
      filterOption={false}
      onSearch={setSearch}
      loading={list.isFetching}
      options={options}
      placeholder={`Search ${table}`}
      notFoundContent={list.isFetching ? <Spin size="small" /> : 'No matches'}
    />
  );
}

function toFormValue(field: MasterField, v: unknown): unknown {
  if (v === null || v === undefined) return field.type === 'boolean' ? false : undefined;
  if (field.type === 'date') return dayjs(String(v));
  return v;
}

function fromFormValue(field: MasterField, v: unknown): unknown {
  if (v === undefined || v === '') return null;
  if (field.type === 'date') return v ? (v as Dayjs).format('YYYY-MM-DDTHH:mm:ss') : null;
  return v;
}

interface FieldInputProps {
  field: MasterField;
  hospitalId?: string;
  locked: boolean;
  /** Injected by Form.Item. */
  value?: unknown;
  checked?: boolean;
  onChange?: unknown;
}

function FieldInput({ field, hospitalId, locked, ...control }: FieldInputProps) {
  const props = control as Record<string, unknown>;
  if (field.lookup) return <LookupSelect field={field} hospitalId={hospitalId} disabled={locked} {...props} />;
  if (field.options?.length)
    return (
      <Select
        allowClear={!field.required}
        showSearch={field.options.length > 10}
        disabled={locked}
        options={field.options.map((o) => ({ value: o, label: o }))}
        {...props}
      />
    );
  switch (field.type) {
    case 'boolean':
      return <Switch disabled={locked} {...props} />;
    case 'date':
      return <DatePicker style={{ width: '100%' }} disabled={locked} {...props} />;
    case 'number':
      return <InputNumber style={{ width: '100%' }} precision={0} disabled={locked} {...props} />;
    case 'decimal':
      return <InputNumber style={{ width: '100%' }} step={0.01} disabled={locked} {...props} />;
    default:
      return field.large || (field.maxLength ?? 0) > 500 ? (
        <Input.TextArea autoSize={{ minRows: 2, maxRows: 8 }} maxLength={field.maxLength ?? undefined} disabled={locked} {...props} />
      ) : (
        <Input maxLength={field.maxLength ?? undefined} disabled={locked} {...props} />
      );
  }
}

/** Create / edit / delete form generated from the master's field metadata, with server-side validation messages. */
export function MasterFormDrawer({ master, id, defaultHospitalId, onClose }: Props) {
  const [form] = Form.useForm();
  const queryClient = useQueryClient();
  const lockedHospital = useHospitalLocked();
  const [formError, setFormError] = useState<string | null>(null);
  const isNew = !id;
  const editable = isNew ? master.canCreate : master.canUpdate;
  const fields = master.fields.filter((f) => !(isNew && f.readOnly));
  const hospitalField = master.hospitalField ?? undefined;
  const hospitalValue = Form.useWatch(hospitalField ?? '__none', form) as string | undefined;

  const record = useQuery({
    queryKey: ['master-record', master.table, id],
    queryFn: () => mastersApi.get(master.table, id!),
    enabled: !isNew,
  });

  const initialValues = useMemo(() => {
    const row: ExplorerRow = record.data?.row ?? {};
    const initial: Record<string, unknown> = {};
    for (const f of fields) initial[f.key] = toFormValue(f, row[f.key]);
    if (isNew) {
      for (const f of fields) if (f.type === 'boolean' && /^(isActive|active)$/i.test(f.key)) initial[f.key] = true;
      const status = fields.find((f) => f.key === 'status' && f.options?.includes('active'));
      if (status) initial[status.key] = 'active';
      if (hospitalField) initial[hospitalField] = lockedHospital ?? defaultHospitalId;
    }
    return initial;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [record.data, master.table, id]);

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ['explorer', master.table] });
    void queryClient.invalidateQueries({ queryKey: ['masters-catalog'] });
    void queryClient.invalidateQueries({ queryKey: ['master-lookup', master.table] });
  };

  const save = useMutation({
    mutationFn: (values: MasterValues) =>
      isNew ? mastersApi.create(master.table, values) : mastersApi.update(master.table, id!, values),
    onSuccess: () => {
      void message.success(`${master.label} ${isNew ? 'created' : 'updated'}`);
      invalidate();
      onClose();
    },
    onError: (e) => {
      const errors = getMasterFieldErrors(e);
      const known = Object.entries(errors).filter(([k]) => fields.some((f) => f.key === k));
      form.setFields(known.map(([name, text]) => ({ name, errors: [text] })));
      const other = Object.entries(errors).filter(([k]) => !fields.some((f) => f.key === k));
      setFormError(
        known.length && !other.length ? 'Please correct the highlighted fields.' : other.map(([k, v]) => `${k}: ${v}`).join(' ') || getApiErrorMessage(e),
      );
    },
  });

  const remove = useMutation({
    mutationFn: () => mastersApi.remove(master.table, id!),
    onSuccess: (r) => {
      void message.success(
        r.deactivated
          ? `Deactivated: still referenced by ${r.referencedBy.join(', ')}`
          : `${master.label} deleted`,
      );
      invalidate();
      onClose();
    },
    onError: (e) => setFormError(getApiErrorMessage(e)),
  });

  const submit = (values: Record<string, unknown>) => {
    setFormError(null);
    const payload: MasterValues = {};
    for (const f of fields) if (!f.readOnly) payload[f.key] = fromFormValue(f, values[f.key]);
    save.mutate(payload);
  };

  const title = isNew ? `New ${master.label}` : `Edit ${master.label}`;

  return (
    <Drawer
      open
      width={640}
      title={title}
      onClose={onClose}
      destroyOnClose
      extra={
        <Space>
          {!isNew && master.canDelete && (
            <Popconfirm
              title={`Delete this ${master.label}?`}
              description="Rows still used by claims or other records are deactivated instead."
              onConfirm={() => remove.mutate()}
            >
              <Button danger icon={<DeleteOutlined />} loading={remove.isPending}>
                Delete
              </Button>
            </Popconfirm>
          )}
          {editable && (
            <Button type="primary" icon={<SaveOutlined />} loading={save.isPending} onClick={() => form.submit()}>
              Save
            </Button>
          )}
        </Space>
      }
    >
      <Typography.Paragraph type="secondary" style={{ marginBottom: 8 }}>
        {master.purpose}
      </Typography.Paragraph>
      <Alert
        type="info"
        style={{ marginBottom: 16 }}
        message={
          <span>
            <Typography.Text strong>Feeds in claim JSON: </Typography.Text>
            <Typography.Text code>{master.feeds}</Typography.Text>
          </span>
        }
      />
      {!editable && (
        <Alert type="info" showIcon style={{ marginBottom: 16 }} message="Read only: you do not have permission to change this master." />
      )}
      {formError && <Alert type="error" showIcon closable style={{ marginBottom: 16 }} message={formError} onClose={() => setFormError(null)} />}
      {record.isLoading ? (
        <Spin />
      ) : record.error ? (
        <Alert type="error" message={getApiErrorMessage(record.error)} />
      ) : (
        <Form
          key={`${master.table}:${id ?? 'new'}:${record.dataUpdatedAt}`}
          form={form}
          layout="vertical"
          initialValues={initialValues}
          onFinish={submit}
          disabled={!editable}
          requiredMark
        >
          {fields.map((f) => {
            const locked = f.readOnly || (!!lockedHospital && f.key === hospitalField);
            return (
              <Form.Item
                key={f.key}
                name={f.key}
                label={f.label}
                valuePropName={f.type === 'boolean' ? 'checked' : 'value'}
                rules={f.required && !f.readOnly ? [{ required: true, message: `${f.label} is required` }] : undefined}
                tooltip={f.lookup ? `Pick from ${f.lookup}` : undefined}
              >
                <FieldInput field={f} hospitalId={f.key === hospitalField ? undefined : hospitalValue} locked={locked} />
              </Form.Item>
            );
          })}
        </Form>
      )}
    </Drawer>
  );
}
