import { Table, Input, Space, Button, Alert, Typography } from 'antd';
import { ReloadOutlined, SearchOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { useMasterData } from '@/hooks/useMasterData';

interface DataTableProps<T> {
  /** API route segment, e.g. "PayerMasters". */
  resource: string;
  columns: ColumnsType<T>;
  /** Placeholder text for the search box. */
  searchPlaceholder?: string;
  /** Unique row key field. Defaults to "id". */
  rowKey?: keyof T & string;
}

/**
 * Generic server-paginated grid for any master-data resource.
 * Handles search, pagination, loading, refresh and error display.
 */
export function DataTable<T extends object>({
  resource,
  columns,
  searchPlaceholder = 'Search…',
  rowKey = 'id' as keyof T & string,
}: DataTableProps<T>) {
  const { rows, total, isLoading, isFetching, error, refetch, page, pageSize, search, setSearch, setPage, setPageSize } =
    useMasterData<T>(resource);

  return (
    <Space direction="vertical" style={{ width: '100%' }} size="middle">
      <Space>
   <Input
   allowClear
       prefix={<SearchOutlined />}
     placeholder={searchPlaceholder}
     value={search}
    onChange={(e) => setSearch(e.target.value)}
      style={{ width: 320 }}
        />
        <Button icon={<ReloadOutlined />} onClick={() => void refetch()} loading={isFetching}>
Refresh
        </Button>
 <Typography.Text type="secondary">{total} record(s)</Typography.Text>
   </Space>

      {error && (
   <Alert
  type="error"
       showIcon
     message="Failed to load data"
     description={error.message}
        />
      )}

      <Table<T>
    rowKey={(record) => String((record as Record<string, unknown>)[rowKey] ?? JSON.stringify(record))}
   columns={columns}
   dataSource={rows}
   loading={isLoading}
        size="middle"
        scroll={{ x: 'max-content' }}
 pagination={{
     current: page,
     pageSize,
    total,
     showSizeChanger: true,
     pageSizeOptions: ['10', '20', '50', '100'],
      showTotal: (t, range) => `${range[0]}-${range[1]} of ${t}`,
    onChange: (nextPage, nextSize) => {
      setPage(nextPage);
     if (nextSize !== pageSize) setPageSize(nextSize);
     },
 }}
      />
    </Space>
  );
}
