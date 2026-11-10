import { useMemo, useState, type ReactNode } from 'react';
import {
  Badge,
  Button,
  Checkbox,
  Input,
  Popover,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
  message,
} from 'antd';
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table';
import type { FilterValue, SorterResult } from 'antd/es/table/interface';
import { DownloadOutlined, FilterOutlined, ReloadOutlined, SettingOutlined } from '@ant-design/icons';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { explorerApi } from '@/api/endpoints/explorer';
import { useDebounce } from '@/hooks/useDebounce';
import { useGlobalFilters } from '@/hooks/useGlobalFilters';
import type { ExplorerColumn, ExplorerRow, ExplorerTable } from '@/types/explorer';
import { getApiErrorMessage } from '@/utils/apiError';
import { renderValue } from '@/utils/display';
import { RecordDrawer } from './RecordDrawer';

interface Props {
  table: ExplorerTable;
  /** Locked equality filters (used for master-detail child grids). */
  fixedFilters?: Record<string, string>;
  /** Child grids ignore the global date range so all lines of a header are shown. */
  applyDateRange?: boolean;
  compact?: boolean;
  /** Columns shown initially (defaults to the first non-large columns). */
  initialColumns?: string[];
  /** Replaces the default read-only record drawer on row click. */
  onRowClick?: (row: ExplorerRow) => void;
  /** Extra toolbar actions (e.g. "Add"). */
  toolbarExtra?: ReactNode;
}

const MAX_DEFAULT_COLUMNS = 10;

function defaultColumns(table: ExplorerTable, hidden: string[]): string[] {
  const candidates = table.columns.filter((c) => !c.large && !hidden.includes(c.key));
  const preferred = candidates.filter((c) => c.isKey || c.type !== 'guid');
  const ordered = [...preferred, ...candidates.filter((c) => !preferred.includes(c))];
  return ordered.slice(0, MAX_DEFAULT_COLUMNS).map((c) => c.key);
}

function columnFilterDropdown(col: ExplorerColumn): ColumnsType<ExplorerRow>[number]['filterDropdown'] {
  return ({ selectedKeys, setSelectedKeys, confirm, clearFilters }) => (
    <Space direction="vertical" style={{ padding: 8, width: 220 }}>
      {col.type === 'boolean' ? (
        <Select
          style={{ width: '100%' }}
          placeholder="Any"
          value={(selectedKeys[0] as string) ?? undefined}
          onChange={(v) => setSelectedKeys(v ? [v] : [])}
          options={[
            { value: 'true', label: 'Yes' },
            { value: 'false', label: 'No' },
          ]}
        />
      ) : (
        <Input
          autoFocus
          placeholder={`${col.label} equals...`}
          value={selectedKeys[0] as string}
          onChange={(e) => setSelectedKeys(e.target.value ? [e.target.value] : [])}
          onPressEnter={() => confirm()}
        />
      )}
      <Space>
        <Button type="primary" size="small" onClick={() => confirm()}>
          Apply
        </Button>
        <Button
          size="small"
          onClick={() => {
            clearFilters?.();
            confirm();
          }}
        >
          Reset
        </Button>
      </Space>
    </Space>
  );
}

/** Generic, metadata-driven grid for any table with paging, sorting, search, filters and export. */
export function ExplorerGrid({
  table,
  fixedFilters,
  applyDateRange = true,
  compact,
  initialColumns,
  onRowClick,
  toolbarExtra,
}: Props) {
  const global = useGlobalFilters();
  const hiddenKeys = Object.keys(fixedFilters ?? {});
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(compact ? 10 : 25);
  const [search, setSearch] = useState('');
  const [dateField, setDateField] = useState<string | undefined>(table.defaultDateField ?? undefined);
  const [sort, setSort] = useState<{ by?: string; dir?: 'asc' | 'desc' }>({});
  const [columnFilters, setColumnFilters] = useState<Record<string, string>>({});
  const [visible, setVisible] = useState<string[]>(() => {
    const preset = (initialColumns ?? []).filter((k) => table.columns.some((c) => c.key === k));
    return preset.length > 0 ? preset : defaultColumns(table, hiddenKeys);
  });
  const [selected, setSelected] = useState<ExplorerRow | null>(null);
  const [exporting, setExporting] = useState(false);
  const debouncedSearch = useDebounce(search);

  const params = {
    hospitalId: global.hospitalId,
    from: applyDateRange && dateField ? global.from : undefined,
    to: applyDateRange && dateField ? global.to : undefined,
    dateField,
    search: debouncedSearch || undefined,
    sortBy: sort.by,
    sortDir: sort.dir,
    page,
    pageSize,
    filters: { ...columnFilters, ...fixedFilters },
  };

  const { data, isFetching, error, refetch } = useQuery({
    queryKey: ['explorer', table.name, params],
    queryFn: () => explorerApi.query(table.name, params),
    placeholderData: keepPreviousData,
  });

  const columns = useMemo<ColumnsType<ExplorerRow>>(
    () =>
      table.columns
        .filter((c) => visible.includes(c.key))
        .map((c) => ({
          key: c.key,
          dataIndex: c.key,
          title: c.label,
          ellipsis: true,
          width: c.type === 'guid' ? 140 : c.type === 'date' ? 160 : undefined,
          sorter: true,
          sortOrder: sort.by === c.key ? (sort.dir === 'asc' ? 'ascend' : 'descend') : null,
          filteredValue: columnFilters[c.key] ? [columnFilters[c.key]] : null,
          filterDropdown: c.type === 'date' ? undefined : columnFilterDropdown(c),
          align: c.type === 'number' || c.type === 'decimal' ? ('right' as const) : undefined,
          render: (v: unknown) => renderValue(v, c.type),
        })),
    [table, visible, sort, columnFilters],
  );

  const onTableChange = (
    pagination: TablePaginationConfig,
    filters: Record<string, FilterValue | null>,
    sorter: SorterResult<ExplorerRow> | SorterResult<ExplorerRow>[],
  ) => {
    const s = Array.isArray(sorter) ? sorter[0] : sorter;
    setSort(s?.order ? { by: String(s.columnKey), dir: s.order === 'ascend' ? 'asc' : 'desc' } : {});
    const next: Record<string, string> = {};
    for (const [k, v] of Object.entries(filters)) if (v?.[0] !== undefined) next[k] = String(v[0]);
    setColumnFilters(next);
    setPage(pagination.current ?? 1);
    setPageSize(pagination.pageSize ?? pageSize);
  };

  const exportCsv = async () => {
    setExporting(true);
    try {
      await explorerApi.exportCsv(table.name, params);
    } catch (e) {
      void message.error(getApiErrorMessage(e, 'Export failed'));
    } finally {
      setExporting(false);
    }
  };

  const activeFilters = Object.entries(columnFilters);
  const columnChooser = (
    <Checkbox.Group
      style={{ display: 'flex', flexDirection: 'column', maxHeight: 360, overflow: 'auto' }}
      value={visible}
      onChange={(v) => setVisible(v as string[])}
      options={table.columns.filter((c) => !c.large).map((c) => ({ value: c.key, label: c.label }))}
    />
  );

  return (
    <>
      <Space wrap style={{ marginBottom: 12, width: '100%', justifyContent: 'space-between' }}>
        <Space wrap>
          <Input.Search
            allowClear
            placeholder="Search text columns"
            style={{ width: compact ? 200 : 280 }}
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
          {applyDateRange && table.dateFields.length > 0 && (
            <Tooltip title="Which date column the global date range applies to">
              <Select
                style={{ minWidth: 190 }}
                allowClear
                placeholder="No date filter"
                value={dateField}
                onChange={(v) => {
                  setDateField(v);
                  setPage(1);
                }}
                options={table.dateFields.map((f) => ({
                  value: f,
                  label: `Date: ${table.columns.find((c) => c.key === f)?.label ?? f}`,
                }))}
              />
            </Tooltip>
          )}
          {!table.hospitalScoped && !compact && (
            <Tooltip title="This table is shared across hospitals; the hospital filter does not apply.">
              <Tag>All hospitals</Tag>
            </Tooltip>
          )}
          {activeFilters.map(([k, v]) => (
            <Tag
              key={k}
              icon={<FilterOutlined />}
              closable
              onClose={() => {
                const rest = { ...columnFilters };
                delete rest[k];
                setColumnFilters(rest);
              }}
            >
              {table.columns.find((c) => c.key === k)?.label ?? k} = {v}
            </Tag>
          ))}
        </Space>
        <Space>
          {toolbarExtra}
          <Badge count={data?.total ?? 0} overflowCount={999999} showZero color="#1677ff" />
          <Popover trigger="click" title="Columns" content={columnChooser} placement="bottomRight">
            <Button icon={<SettingOutlined />} />
          </Popover>
          <Button icon={<ReloadOutlined />} onClick={() => void refetch()} />
          <Button icon={<DownloadOutlined />} loading={exporting} onClick={() => void exportCsv()}>
            CSV
          </Button>
        </Space>
      </Space>
      {error && (
        <Typography.Text type="danger" style={{ display: 'block', marginBottom: 8 }}>
          {getApiErrorMessage(error)}
        </Typography.Text>
      )}
      <Table<ExplorerRow>
        size={compact ? 'small' : 'middle'}
        rowKey={(r) => String(r[table.keyField])}
        loading={isFetching}
        columns={columns}
        dataSource={data?.items ?? []}
        scroll={{ x: 'max-content' }}
        onChange={onTableChange}
        onRow={(r) => ({ onClick: () => (onRowClick ? onRowClick(r) : setSelected(r)), style: { cursor: 'pointer' } })}
        pagination={{
          current: page,
          pageSize,
          total: data?.total ?? 0,
          showSizeChanger: true,
          pageSizeOptions: [10, 25, 50, 100, 250],
          showTotal: (t, [a, b]) => `${a}-${b} of ${t}`,
        }}
      />
      {selected && (
        <RecordDrawer table={table} id={String(selected[table.keyField])} onClose={() => setSelected(null)} />
      )}
    </>
  );
}
