import { useState } from 'react';
import { Button, Card, Col, DatePicker, Input, Popconfirm, Row, Select, Space, Switch, Table, Tag, Tooltip, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { DeleteOutlined, DownloadOutlined, EyeOutlined, ReloadOutlined, UploadOutlined } from '@ant-design/icons';
import type { Dayjs } from 'dayjs';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { documentsApi } from '@/api/endpoints/documents';
import { useGlobalFilters } from '@/hooks/useGlobalFilters';
import { PERMISSIONS, usePermission } from '@/hooks/usePermission';
import type { ClaimDocument, DocumentQuery } from '@/types/documents';
import { fmtDate, statusTag } from '@/utils/display';
import { ClaimTimelineDrawer } from './ClaimTimelineDrawer';
import { DocumentUploadModal } from './DocumentUploadModal';
import { ENCOUNTER_TYPES, fmtBytes, useDocumentTypes } from './documentUtils';
import { PageHeader, PatientSelect, showError } from './shared';

type Range = [Dayjs | null, Dayjs | null] | null;

interface Filters {
  patientId?: string;
  encounterNumber?: string;
  encounterType?: string;
  invoiceNumber?: string;
  invoiceRange?: Range;
  serviceRange?: Range;
  documentType?: string;
  claimId?: string;
  unattached?: boolean;
  search?: string;
}

const day = (d?: Dayjs | null) => d?.format('YYYY-MM-DD');

export function DocumentsPage() {
  const global = useGlobalFilters();
  const { can } = usePermission();
  const canUpload = can(PERMISSIONS.ClaimsCreate) || can(PERMISSIONS.ClaimsUpdate);
  const canDelete = can(PERMISSIONS.ClaimsDelete);
  const qc = useQueryClient();
  const types = useDocumentTypes();
  const [filters, setFilters] = useState<Filters>({});
  const [paging, setPaging] = useState({ page: 1, pageSize: 25 });
  const [uploading, setUploading] = useState(false);
  const [claimId, setClaimId] = useState<string>();

  const set = (patch: Partial<Filters>) => {
    setFilters((f) => ({ ...f, ...patch }));
    setPaging((p) => ({ ...p, page: 1 }));
  };

  // The global date range filters on service date unless a specific service range is picked.
  const q: DocumentQuery = {
    providerId: global.rcm.providerId,
    from: day(filters.serviceRange?.[0]) ?? global.rcm.from,
    to: day(filters.serviceRange?.[1]) ?? global.rcm.to,
    patientId: filters.patientId,
    encounterNumber: filters.encounterNumber?.trim(),
    encounterType: filters.encounterType,
    invoiceNumber: filters.invoiceNumber?.trim(),
    invoiceFrom: day(filters.invoiceRange?.[0]),
    invoiceTo: day(filters.invoiceRange?.[1]),
    documentType: filters.documentType,
    claimId: filters.claimId?.trim(),
    unattached: filters.unattached || undefined,
    search: filters.search?.trim(),
    ...paging,
  };

  const list = useQuery({ queryKey: ['rcm', 'documents', q], queryFn: () => documentsApi.list(q), placeholderData: keepPreviousData });

  const remove = useMutation({
    mutationFn: (id: string) => documentsApi.remove(id),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['rcm', 'documents'] }),
    onError: (e) => showError('Delete failed', e),
  });

  const open = (doc: ClaimDocument, download = false) =>
    documentsApi.open(doc, { download }).catch((e) => showError('Could not open the document', e));

  const columns: ColumnsType<ClaimDocument> = [
    {
      title: 'Document',
      render: (_, r) => (
        <Space direction="vertical" size={0}>
          <a onClick={() => open(r)}>{r.title || r.fileName}</a>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {r.fileName}
          </Typography.Text>
        </Space>
      ),
    },
    { title: 'Type', dataIndex: 'documentType', render: (v) => <Tag>{v}</Tag> },
    { title: 'Patient', dataIndex: 'patientName', render: (v, r) => v ?? r.patientId },
    {
      title: 'Encounter',
      render: (_, r) =>
        r.encounterNumber || r.encounterType ? (
          <Space size={4}>
            {r.encounterNumber}
            {r.encounterType && <Tag color={r.encounterType === 'IMP' ? 'purple' : 'default'}>{r.encounterType}</Tag>}
          </Space>
        ) : (
          '—'
        ),
    },
    { title: 'Invoice', render: (_, r) => (r.invoiceNumber ? `${r.invoiceNumber}${r.invoiceDate ? ` · ${fmtDate(r.invoiceDate)}` : ''}` : '—') },
    { title: 'Service date', dataIndex: 'serviceDate', render: (v) => fmtDate(v) },
    {
      title: 'Size',
      align: 'right',
      render: (_, r) => (
        <Tooltip title={`Base64 in the claim message: ${fmtBytes(r.encodedSizeBytes)}${r.isCompressed ? ` · original ${fmtBytes(r.originalSizeBytes)}` : ''}`}>
          <span>
            {fmtBytes(r.sizeBytes)}
            {r.isCompressed && (
              <Tag color="green" style={{ marginInlineStart: 4 }}>
                compressed
              </Tag>
            )}
          </span>
        </Tooltip>
      ),
    },
    {
      title: 'Claims',
      render: (_, r) =>
        r.claims.length === 0 ? (
          <Tag>unattached</Tag>
        ) : (
          <Space size={4} wrap>
            {r.claims.map((c) => (
              <Tooltip key={c.claimId} title={`${c.nphiesStatus ?? ''}${c.sent ? ' · sent to NPHIES' : ''}`}>
                <Tag color={c.sent ? 'blue' : 'gold'} style={{ cursor: 'pointer' }} onClick={() => setClaimId(c.claimId)}>
                  {c.claimNumber ?? c.claimId.slice(0, 8)}
                </Tag>
              </Tooltip>
            ))}
          </Space>
        ),
    },
    { title: 'Source', dataIndex: 'source', render: statusTag },
    { title: 'Uploaded', dataIndex: 'createdAt', render: (v, r) => <Tooltip title={r.createdBy}>{fmtDate(v, true)}</Tooltip> },
    {
      title: '',
      width: 120,
      render: (_, r) => (
        <Space size={0}>
          <Tooltip title="View">
            <Button type="text" size="small" icon={<EyeOutlined />} onClick={() => open(r)} />
          </Tooltip>
          <Tooltip title="Download">
            <Button type="text" size="small" icon={<DownloadOutlined />} onClick={() => open(r, true)} />
          </Tooltip>
          <Popconfirm
            title="Delete this document?"
            description={r.claims.length ? 'Detach it from its claims first.' : 'The file is removed from the document store.'}
            disabled={!canDelete || r.claims.length > 0}
            onConfirm={() => remove.mutate(r.id)}
          >
            <Tooltip title={r.claims.length ? 'Attached to a claim' : 'Delete'}>
              <Button type="text" size="small" danger icon={<DeleteOutlined />} disabled={!canDelete || r.claims.length > 0} loading={remove.isPending && remove.variables === r.id} />
            </Tooltip>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Claim documents"
        extra={
          <Space>
            <Button icon={<ReloadOutlined />} onClick={() => list.refetch()} loading={list.isFetching}>
              Refresh
            </Button>
            <Button type="primary" icon={<UploadOutlined />} disabled={!canUpload} onClick={() => setUploading(true)}>
              Upload
            </Button>
          </Space>
        }
      />

      <Card size="small" style={{ marginBottom: 16 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} md={8}>
            <PatientSelect value={filters.patientId} onChange={(v) => set({ patientId: v })} />
          </Col>
          <Col xs={12} md={4}>
            <Input allowClear placeholder="Encounter no." value={filters.encounterNumber} onChange={(e) => set({ encounterNumber: e.target.value })} />
          </Col>
          <Col xs={12} md={4}>
            <Select allowClear style={{ width: '100%' }} placeholder="Encounter type" options={ENCOUNTER_TYPES} value={filters.encounterType} onChange={(v) => set({ encounterType: v })} />
          </Col>
          <Col xs={12} md={4}>
            <Input allowClear placeholder="Invoice no." value={filters.invoiceNumber} onChange={(e) => set({ invoiceNumber: e.target.value })} />
          </Col>
          <Col xs={12} md={4}>
            <Select
              allowClear
              style={{ width: '100%' }}
              placeholder="Document type"
              loading={types.isLoading}
              options={(types.data ?? []).map((t) => ({ value: t, label: t }))}
              value={filters.documentType}
              onChange={(v) => set({ documentType: v })}
            />
          </Col>
          <Col xs={24} md={8}>
            <DatePicker.RangePicker
              style={{ width: '100%' }}
              placeholder={['Invoice from', 'Invoice to']}
              value={filters.invoiceRange}
              onChange={(v) => set({ invoiceRange: v })}
            />
          </Col>
          <Col xs={24} md={8}>
            <DatePicker.RangePicker
              style={{ width: '100%' }}
              placeholder={['Service from', 'Service to']}
              value={filters.serviceRange}
              onChange={(v) => set({ serviceRange: v })}
            />
          </Col>
          <Col xs={12} md={4}>
            <Input allowClear placeholder="Claim id" value={filters.claimId} onChange={(e) => set({ claimId: e.target.value })} />
          </Col>
          <Col xs={12} md={4}>
            <Input.Search allowClear placeholder="Title / file / notes" onSearch={(v) => set({ search: v })} />
          </Col>
          <Col xs={24} md={8}>
            <Space>
              <Switch checked={!!filters.unattached} onChange={(v) => set({ unattached: v })} />
              Only unattached
              <Button type="link" onClick={() => set({ patientId: undefined, encounterNumber: undefined, encounterType: undefined, invoiceNumber: undefined, invoiceRange: null, serviceRange: null, documentType: undefined, claimId: undefined, unattached: false, search: undefined })}>
                Clear filters
              </Button>
            </Space>
          </Col>
        </Row>
      </Card>

      <Table
        rowKey="id"
        size="small"
        loading={list.isLoading}
        columns={columns}
        dataSource={list.data?.items}
        scroll={{ x: 1300 }}
        pagination={{
          current: paging.page,
          pageSize: paging.pageSize,
          total: list.data?.total,
          showSizeChanger: true,
          showTotal: (t) => `${t} documents`,
          onChange: (page, pageSize) => setPaging({ page, pageSize }),
        }}
      />

      <DocumentUploadModal
        open={uploading}
        onClose={() => setUploading(false)}
        defaults={{ providerId: global.rcm.providerId, patientId: filters.patientId }}
        onUploaded={() => void qc.invalidateQueries({ queryKey: ['rcm', 'documents'] })}
      />
      {claimId && <ClaimTimelineDrawer claimId={claimId} onClose={() => setClaimId(undefined)} />}
    </>
  );
}
