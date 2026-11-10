import { useState } from 'react';
import { Select, Space, Typography, Table, Tag, Alert, Empty, Card } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useUsers } from './hooks/useUsers';
import { useEffectiveAccess } from './hooks/useAccessControl';
import { RoleTag } from './components/RoleTags';
import type { EffectivePermission } from '@/types/access';

const columns: ColumnsType<EffectivePermission> = [
  {
    title: 'Category',
    dataIndex: 'category',
    width: 180,
    render: (v: string) => <Tag color="geekblue">{v}</Tag>,
  },
  {
    title: 'Permission',
    dataIndex: 'name',
 width: 260,
    render: (v: string) => <code>{v}</code>,
  },
  {
title: 'Granted by',
    dataIndex: 'grantedByRoles',
    render: (roles: string[]) => (
      <Space wrap>
{roles.map((r) => (
          <RoleTag key={r} role={r} />
        ))}
  </Space>
 ),
  },
];

/** Read-only view of exactly what a chosen user can do, and why. */
export function EffectiveAccessPage() {
  const { users } = useUsers();
  const [userId, setUserId] = useState<string | undefined>();
  const { data, isLoading } = useEffectiveAccess(userId);

  return (
    <Space direction="vertical" style={{ width: '100%' }} size="middle">
    <Typography.Title level={4} style={{ margin: 0 }}>
      Effective Access
      </Typography.Title>

      <Alert
        type="info"
 showIcon
   message="Answers the question: what can this user actually do?"
        description="Permissions are the union of everything granted by the user's roles."
      />

      <Select
 showSearch
        allowClear
        style={{ width: 360 }}
        placeholder="Select a user…"
     optionFilterProp="label"
  value={userId}
     onChange={setUserId}
      options={users.map((u) => ({
          label: `${u.username} — ${u.firstName} ${u.lastName}`.trim(),
    value: u.id,
        }))}
      />

      {!userId ? (
        <Empty description="Select a user to inspect their access" />
      ) : (
        <>
       <Card size="small">
            <Space wrap>
      <Typography.Text strong>Roles:</Typography.Text>
              {data?.roles.length ? (
    data.roles.map((r) => <RoleTag key={r} role={r} />)
           ) : (
         <Tag>None</Tag>
              )}
       <Typography.Text type="secondary">
      {data?.permissions.length ?? 0} effective permission(s)
              </Typography.Text>
        </Space>
</Card>

 <Table<EffectivePermission>
         rowKey="name"
      columns={columns}
         dataSource={data?.permissions ?? []}
    loading={isLoading}
            size="middle"
            pagination={false}
    scroll={{ x: 'max-content' }}
          />
        </>
      )}
    </Space>
  );
}
