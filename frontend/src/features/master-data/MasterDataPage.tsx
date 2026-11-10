import { useMemo, useState } from 'react';
import {
  Alert,
  Badge,
  Button,
  Card,
  Col,
  Empty,
  Menu,
  Row,
  Space,
  Spin,
  Table,
  Tabs,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import { ApartmentOutlined, PlusOutlined, TableOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { explorerApi } from '@/api/endpoints/explorer';
import { mastersApi } from '@/api/endpoints/masters';
import { ExplorerGrid } from '@/features/data-explorer/ExplorerGrid';
import { useGlobalFilters } from '@/hooks/useGlobalFilters';
import type { MasterTableInfo } from '@/types/masters';
import { getApiErrorMessage } from '@/utils/apiError';
import { MasterFormDrawer } from './MasterFormDrawer';

/** Where each master lands in the NPHIES request bundle (claim / pre-auth / eligibility). */
const BUNDLE_MAP: { resource: string; elements: string; tables: string[] }[] = [
  { resource: 'MessageHeader', elements: 'sender, destination (license ids)', tables: ['Organizations', 'PayerMasters'] },
  { resource: 'Patient', elements: 'identifier (NI / PRC / PPN), name (en / ar), gender, birthDate', tables: ['Patients'] },
  {
    resource: 'Coverage',
    elements: 'subscriberId / memberId, payor, class (plan), relationship, costToBeneficiary',
    tables: ['Coverages', 'PayerPolicyMasters', 'PolicyBenefitCoverages'],
  },
  { resource: 'Organization', elements: 'Claim.provider, Claim.insurer, Coverage.payor', tables: ['Organizations', 'PayerMasters'] },
  {
    resource: 'Practitioner / careTeam',
    elements: 'Claim.careTeam.provider, role, qualification (specialty)',
    tables: ['Practitioners', 'DoctorMasters', 'DoctorQualifications'],
  },
  { resource: 'Location / Encounter', elements: 'Claim.facility, Encounter.serviceProvider, serviceType', tables: ['Locations', 'ClinicMasters'] },
  {
    resource: 'Claim.item',
    elements: 'productOrService, modifier, unitPrice, benefit category',
    tables: ['ServiceCodeMasters', 'MedicationCodeMasters', 'MedicalDeviceCodeMasters', 'ModifierCodeMasters', 'BenefitCodeMasters'],
  },
  { resource: 'Claim.diagnosis', elements: 'diagnosisCodeableConcept (ICD-10-AM)', tables: ['DiagnosisCodeMasters'] },
  { resource: 'Code systems', elements: 'local code to NPHIES code-system translation', tables: ['NphiesCodeMappings'] },
  {
    resource: 'Validation',
    elements: 'pre-submission rules, NPHIES error explanations',
    tables: ['ClaimSubmissionRules', 'ErrorCodeMasters'],
  },
];

function ClaimJsonMap({ catalog, onOpen }: { catalog: MasterTableInfo[]; onOpen: (table: string) => void }) {
  const byTable = new Map(catalog.map((m) => [m.table.toLowerCase(), m]));
  return (
    <>
      <Typography.Paragraph type="secondary">
        Every NPHIES request bundle is assembled from these masters plus the claim itself (items, diagnoses, supporting
        info). Red counts mean the master is empty and claims that need it will fail validation.
      </Typography.Paragraph>
      <Row gutter={[16, 16]}>
        {BUNDLE_MAP.map((b) => (
          <Col key={b.resource} xs={24} md={12} xl={8}>
            <Card size="small" title={<Space><ApartmentOutlined />{b.resource}</Space>} style={{ height: '100%' }}>
              <Typography.Paragraph type="secondary" style={{ minHeight: 44 }}>
                {b.elements}
              </Typography.Paragraph>
              <Space wrap>
                {b.tables.map((t) => {
                  const m = byTable.get(t.toLowerCase());
                  if (!m) return null;
                  return (
                    <Tooltip key={t} title={m.feeds}>
                      <Tag color={m.count ? 'blue' : 'red'} style={{ cursor: 'pointer' }} onClick={() => onOpen(m.table)}>
                        {m.label} ({m.count ?? 0})
                      </Tag>
                    </Tooltip>
                  );
                })}
              </Space>
            </Card>
          </Col>
        ))}
      </Row>
      <Typography.Title level={5} style={{ marginTop: 24 }}>
        All masters
      </Typography.Title>
      <Table<MasterTableInfo>
        size="small"
        rowKey="table"
        pagination={false}
        dataSource={catalog}
        onRow={(r) => ({ onClick: () => onOpen(r.table), style: { cursor: 'pointer' } })}
        columns={[
          { title: 'Group', dataIndex: 'group', width: 110 },
          { title: 'Master', dataIndex: 'label', width: 200 },
          { title: 'Feeds in claim JSON', dataIndex: 'feeds', render: (v: string) => <Typography.Text code>{v}</Typography.Text> },
          {
            title: 'Rows',
            dataIndex: 'count',
            width: 90,
            align: 'right',
            render: (v?: number | null) => <Typography.Text type={v ? undefined : 'danger'}>{v ?? 0}</Typography.Text>,
          },
        ]}
      />
    </>
  );
}

/**
 * Master Data: maintains every table used to build NPHIES eligibility, pre-auth and claim request JSON.
 * Forms are generated from `api/masters/catalog` metadata; grids honour the global hospital and date filters.
 */
export function MasterDataPage() {
  const global = useGlobalFilters();
  const [tab, setTab] = useState('masters');
  const [selectedTable, setSelectedTable] = useState<string>();
  const [editing, setEditing] = useState<{ id?: string } | null>(null);

  const catalog = useQuery({
    queryKey: ['masters-catalog', global.hospitalId],
    queryFn: () => mastersApi.catalog(global.hospitalId),
  });
  const tables = useQuery({ queryKey: ['explorer', 'tables'], queryFn: explorerApi.tables, staleTime: 10 * 60_000 });

  const masters = useMemo(() => catalog.data ?? [], [catalog.data]);
  const current = masters.find((m) => m.table === selectedTable) ?? masters[0];
  const explorerTable = useMemo(
    () => tables.data?.find((t) => t.name.toLowerCase() === current?.table.toLowerCase()),
    [tables.data, current],
  );

  const menuItems = useMemo(() => {
    const groups = [...new Set(masters.map((m) => m.group))];
    return groups.map((g) => ({
      key: `group:${g}`,
      type: 'group' as const,
      label: g,
      children: masters
        .filter((m) => m.group === g)
        .map((m) => ({
          key: m.table,
          label: (
            <Space style={{ width: '100%', justifyContent: 'space-between' }}>
              <span>{m.label}</span>
              <Badge count={m.count ?? 0} showZero overflowCount={99999} color={m.count ? '#1677ff' : '#ff4d4f'} />
            </Space>
          ),
        })),
    }));
  }, [masters]);

  const open = (table: string) => {
    setSelectedTable(table);
    setTab('masters');
  };

  if (catalog.isLoading) return <Spin style={{ display: 'block', marginTop: 64 }} />;
  if (catalog.error) return <Alert type="error" showIcon message={getApiErrorMessage(catalog.error)} />;
  if (!masters.length) return <Empty description="You do not have access to any master tables." />;

  return (
    <>
      <Typography.Title level={3}>Master Data</Typography.Title>
      <Tabs
        activeKey={tab}
        onChange={setTab}
        items={[
          {
            key: 'masters',
            label: (
              <span>
                <TableOutlined /> Masters
              </span>
            ),
            children: (
              <Row gutter={16} wrap={false}>
                <Col flex="280px">
                  <Menu
                    mode="inline"
                    selectedKeys={current ? [current.table] : []}
                    onClick={(e) => setSelectedTable(e.key)}
                    items={menuItems}
                    style={{ borderInlineEnd: 0 }}
                  />
                </Col>
                <Col flex="auto" style={{ minWidth: 0 }}>
                  {current && (
                    <Card
                      size="small"
                      title={current.label}
                      extra={<Typography.Text type="secondary">{current.table}</Typography.Text>}
                    >
                      <Typography.Paragraph type="secondary" style={{ marginBottom: 4 }}>
                        {current.purpose}
                      </Typography.Paragraph>
                      <Typography.Paragraph style={{ marginBottom: 12 }}>
                        <Typography.Text strong>Feeds: </Typography.Text>
                        <Typography.Text code>{current.feeds}</Typography.Text>
                      </Typography.Paragraph>
                      {explorerTable ? (
                        <ExplorerGrid
                          key={explorerTable.name}
                          table={explorerTable}
                          initialColumns={current.fields.filter((f) => f.summary).map((f) => f.key)}
                          onRowClick={(r) => setEditing({ id: String(r[explorerTable.keyField]) })}
                          toolbarExtra={
                            current.canCreate && (
                              <Button type="primary" icon={<PlusOutlined />} onClick={() => setEditing({})}>
                                Add
                              </Button>
                            )
                          }
                        />
                      ) : (
                        <Spin />
                      )}
                    </Card>
                  )}
                </Col>
              </Row>
            ),
          },
          {
            key: 'map',
            label: (
              <span>
                <ApartmentOutlined /> Claim JSON map
              </span>
            ),
            children: <ClaimJsonMap catalog={masters} onOpen={open} />,
          },
        ]}
      />
      {editing && current && (
        <MasterFormDrawer
          master={current}
          id={editing.id}
          defaultHospitalId={global.hospitalId}
          onClose={() => setEditing(null)}
        />
      )}
    </>
  );
}
