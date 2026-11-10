import { useMemo, useState } from 'react';
import { Card, Empty, Input, Layout, Menu, Space, Spin, Tag, Typography } from 'antd';
import { TableOutlined } from '@ant-design/icons';
import { useNavigate, useParams } from 'react-router-dom';
import { useExplorerTables } from './useExplorerTables';
import { ExplorerGrid } from './ExplorerGrid';
import { getApiErrorMessage } from '@/utils/apiError';

/** Every database table grouped by category, each with filters, master-detail and export. */
export function DataExplorerPage() {
  const { table: tableName } = useParams();
  const navigate = useNavigate();
  const { byCategory, byName, isLoading, error } = useExplorerTables();
  const [filter, setFilter] = useState('');

  const menuItems = useMemo(
    () =>
      byCategory
        .map(([category, tables]) => ({
          key: `cat:${category}`,
          label: category,
          type: 'group' as const,
          children: tables
            .filter((t) => !filter || t.label.toLowerCase().includes(filter.toLowerCase()))
            .map((t) => ({ key: t.name, icon: <TableOutlined />, label: t.label })),
        }))
        .filter((g) => g.children.length > 0),
    [byCategory, filter],
  );

  const table = tableName ? byName.get(tableName) : undefined;

  if (isLoading) return <Spin />;
  if (error) return <Typography.Text type="danger">{getApiErrorMessage(error)}</Typography.Text>;

  return (
    <Layout style={{ background: 'transparent', gap: 16 }} hasSider>
      <Card size="small" style={{ width: 260, flexShrink: 0 }} styles={{ body: { padding: 8 } }}>
        <Input.Search
          allowClear
          placeholder="Find table"
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          style={{ marginBottom: 8 }}
        />
        <div style={{ maxHeight: 'calc(100vh - 220px)', overflow: 'auto' }}>
          <Menu
            mode="inline"
            selectedKeys={table ? [table.name] : []}
            items={menuItems}
            onClick={({ key }) => navigate(`/data/${key}`)}
            style={{ borderInlineEnd: 'none' }}
          />
        </div>
      </Card>
      <Card style={{ flex: 1, minWidth: 0 }}>
        {table ? (
          <>
            <Space style={{ marginBottom: 12 }} wrap>
              <Typography.Title level={4} style={{ margin: 0 }}>
                {table.label}
              </Typography.Title>
              <Tag>{table.category}</Tag>
              {table.hospitalScoped && <Tag color="blue">Hospital: {table.hospitalPath}</Tag>}
              {table.children.length > 0 && (
                <Tag color="purple">Details: {table.children.map((c) => c.label).join(', ')}</Tag>
              )}
            </Space>
            <ExplorerGrid key={table.name} table={table} />
          </>
        ) : (
          <Empty description="Pick a table on the left" />
        )}
      </Card>
    </Layout>
  );
}
