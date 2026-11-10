import { Badge, Descriptions, Drawer, Empty, Spin, Tabs, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { explorerApi } from '@/api/endpoints/explorer';
import { useExplorerTables } from './useExplorerTables';
import type { ExplorerTable } from '@/types/explorer';
import { getApiErrorMessage } from '@/utils/apiError';
import { renderValue } from '@/utils/display';
import { ExplorerGrid } from './ExplorerGrid';

interface Props {
  table: ExplorerTable;
  id: string;
  onClose: () => void;
}

function LargeValue({ value }: { value: unknown }) {
  if (value === null || value === undefined || value === '') return <Typography.Text type="secondary">{'—'}</Typography.Text>;
  let text = String(value);
  try {
    text = JSON.stringify(JSON.parse(text), null, 2);
  } catch {
    /* not JSON */
  }
  return (
    <pre style={{ maxHeight: 280, overflow: 'auto', margin: 0, fontSize: 12, whiteSpace: 'pre-wrap', wordBreak: 'break-all' }}>
      {text}
    </pre>
  );
}

/** Master record (all columns) with one tab per related child table. */
export function RecordDrawer({ table, id, onClose }: Props) {
  const { byName } = useExplorerTables();
  const { data, isLoading, error } = useQuery({
    queryKey: ['explorer-record', table.name, id],
    queryFn: () => explorerApi.get(table.name, id),
  });

  const detailsTab = data && (
    <>
      <Descriptions bordered size="small" column={{ xs: 1, md: 2 }}>
        {table.columns
          .filter((c) => !c.large)
          .map((c) => (
            <Descriptions.Item key={c.key} label={c.label}>
              {renderValue(data.row[c.key], c.type)}
            </Descriptions.Item>
          ))}
      </Descriptions>
      {table.columns
        .filter((c) => c.large)
        .map((c) => (
          <div key={c.key} style={{ marginTop: 16 }}>
            <Typography.Text strong>{c.label}</Typography.Text>
            <LargeValue value={data.row[c.key]} />
          </div>
        ))}
    </>
  );

  const childTabs = (data?.children ?? []).map((child) => {
    const childTable = byName.get(child.table);
    return {
      key: `${child.table}:${child.foreignKey}`,
      label: (
        <span>
          {child.label} <Badge count={child.count} showZero color={child.count ? '#1677ff' : '#d9d9d9'} />
        </span>
      ),
      children: childTable ? (
        <ExplorerGrid
          compact
          applyDateRange={false}
          table={childTable}
          fixedFilters={{ [child.foreignKey]: String(child.value) }}
        />
      ) : (
        <Empty description="No access to this table" />
      ),
    };
  });

  return (
    <Drawer open width="85%" title={`${table.label} · ${id}`} onClose={onClose} destroyOnClose>
      {isLoading && <Spin />}
      {error && <Typography.Text type="danger">{getApiErrorMessage(error)}</Typography.Text>}
      {data && <Tabs items={[{ key: '_details', label: 'Details', children: detailsTab }, ...childTabs]} />}
    </Drawer>
  );
}
