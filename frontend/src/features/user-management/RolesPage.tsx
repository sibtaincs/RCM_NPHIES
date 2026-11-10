import { useState } from 'react';
import { Table, Button, Space, Typography, Tag, Alert, Tooltip, Popconfirm } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, LockOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { useRolesList, useRoleMutations } from './hooks/useAccessControl';
import { RoleFormDrawer } from './components/RoleFormDrawer';
import type { Role } from '@/types/access';

/** Manage roles and the permissions granted to each. */
export function RolesPage() {
  const { data: roles = [], isLoading, error } = useRolesList();
  const { remove } = useRoleMutations();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Role | null>(null);

  const openCreate = () => {
    setEditing(null);
    setOpen(true);
  };

  const columns: ColumnsType<Role> = [
    {
      title: 'Role',
      dataIndex: 'displayName',
      width: 220,
      render: (v: string, r) => (
        <Space>
          <Typography.Text strong>{v}</Typography.Text>
          {r.isSystemRole && (
            <Tooltip title="System role — cannot be renamed or deleted">
              <LockOutlined style={{ color: '#999' }} />
            </Tooltip>
          )}
        </Space>
      ),
    },
    {
      title: 'Name',
      dataIndex: 'name',
      width: 190,
      render: (v: string) => <code>{v}</code>,
    },
    { title: 'Description', dataIndex: 'description', width: 260, render: (v?: string) => v ?? '—' },
    {
      title: 'Permissions',
      width: 140,
      align: 'right',
      render: (_, r) => <Tag color="blue">{r.permissionNames.length}</Tag>,
    },
    {
      title: '',
      key: 'actions',
      width: 120,
      fixed: 'right',
      render: (_, r) => (
        <Space>
          <Tooltip title="Edit role & permissions">
            <Button
              size="small"
              icon={<EditOutlined />}
              onClick={() => {
                setEditing(r);
                setOpen(true);
              }}
            />
          </Tooltip>
          <Popconfirm
            title="Delete this role?"
            description="Only unused, non-system roles can be deleted."
            onConfirm={() => remove.mutate(r.id)}
            disabled={r.isSystemRole}
          >
            <Button size="small" danger icon={<DeleteOutlined />} disabled={r.isSystemRole} />
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <>
      <Space direction="vertical" style={{ width: '100%' }} size="middle">
        <Space style={{ justifyContent: 'space-between', width: '100%' }}>
          <Typography.Title level={4} style={{ margin: 0 }}>
            Roles
          </Typography.Title>
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            New role
          </Button>
        </Space>

        <Alert
          type="info"
          showIcon
          message="Roles drive access across the whole system"
          description="Permissions granted here are written into each user's token at sign-in. Users must sign out and back in for changes to take effect."
        />

        {error && <Alert type="error" showIcon message="Failed to load roles" description={(error as Error).message} />}

        <Table<Role>
          rowKey="id"
          columns={columns}
          dataSource={roles}
          loading={isLoading}
          size="middle"
          pagination={false}
          scroll={{ x: 'max-content' }}
        />
      </Space>

      <RoleFormDrawer open={open} role={editing} onClose={() => setOpen(false)} />
    </>
  );
}
