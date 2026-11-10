import { useState } from 'react';
import {
  Table,
  Input,
  Button,
  Space,
  Typography,
  Alert,
  Dropdown,
  Tooltip,
} from 'antd';
import {
  PlusOutlined,
  ReloadOutlined,
  SearchOutlined,
  MoreOutlined,
  EditOutlined,
  KeyOutlined,
  UnlockOutlined,
  StopOutlined,
  CheckCircleOutlined,
  DeleteOutlined,
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { useUsers, useUserMutations } from './hooks/useUsers';
import { RoleTags } from './components/RoleTags';
import { UserStatusBadge } from './components/UserStatusBadge';
import { UserFormDrawer } from './components/UserFormDrawer';
import { PasswordResetModal } from './components/PasswordResetModal';
import { useAuthStore } from '@/store/authStore';
import { orDash } from '@/utils/format';
import type { UserRecord } from '@/types/users';

export function UsersListPage() {
  const {
    users,
    total,
    isLoading,
    isFetching,
    error,
    refetch,
    page,
    pageSize,
    search,
    setSearch,
    setPage,
    setPageSize,
  } = useUsers();

  const { setActive, unlock, remove } = useUserMutations();
  const currentUsername = useAuthStore((s) => s.user?.username);

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<UserRecord | null>(null);
  const [resetTarget, setResetTarget] = useState<UserRecord | null>(null);

  const openCreate = () => {
    setEditing(null);
    setFormOpen(true);
  };

  const openEdit = (user: UserRecord) => {
    setEditing(user);
    setFormOpen(true);
  };

  const columns: ColumnsType<UserRecord> = [
    {
      title: 'Username',
    dataIndex: 'username',
      width: 170,
      fixed: 'left',
      render: (v: string) => (
      <Space>
     <Typography.Text strong>{v}</Typography.Text>
     {v === currentUsername && <Typography.Text type="secondary">(you)</Typography.Text>}
        </Space>
    ),
    },
    {
      title: 'Name',
    width: 200,
   render: (_, u) => orDash([u.firstName, u.lastName].filter(Boolean).join(' ')),
    },
    { title: 'Email', dataIndex: 'email', width: 230, render: orDash },
    {
  title: 'Roles',
      dataIndex: 'roles',
      width: 280,
      render: (_, u) => <RoleTags roles={u.roles} />,
    },
    {
      title: 'Status',
      width: 120,
      render: (_, u) => <UserStatusBadge user={u} />,
    },
    {
      title: 'Failed logins',
      dataIndex: 'failedLoginAttempts',
 width: 130,
      align: 'right',
      render: (v: number) => (v > 0 ? <Typography.Text type="danger">{v}</Typography.Text> : '0'),
    },
    {
      title: 'Last login',
      dataIndex: 'lastLoginAt',
      width: 170,
      render: (v?: string | null) => (v ? dayjs(v).format('YYYY-MM-DD HH:mm') : 'Never'),
    },
    {
      title: '',
      key: 'actions',
      width: 60,
      fixed: 'right',
      render: (_, u) => {
        const isSelf = u.username === currentUsername;
        return (
     <Dropdown
       trigger={['click']}
      menu={{
       items: [
        { key: 'edit', icon: <EditOutlined />, label: 'Edit' },
      { key: 'reset', icon: <KeyOutlined />, label: 'Reset password' },
       ...(u.isLocked
     ? [{ key: 'unlock', icon: <UnlockOutlined />, label: 'Unlock' }]
      : []),
     {
        key: 'toggle',
       icon: u.isActive ? <StopOutlined /> : <CheckCircleOutlined />,
 label: u.isActive ? 'Deactivate' : 'Activate',
   disabled: isSelf,
     },
   { type: 'divider' as const },
   {
      key: 'delete',
        icon: <DeleteOutlined />,
      label: 'Delete',
     danger: true,
     disabled: isSelf,
       },
    ],
        onClick: ({ key }) => {
     if (key === 'edit') openEdit(u);
      if (key === 'reset') setResetTarget(u);
     if (key === 'unlock') unlock.mutate(u.id);
        if (key === 'toggle') setActive.mutate({ id: u.id, isActive: !u.isActive });
      if (key === 'delete') {
     // Confirm inline to avoid an extra modal component.
   if (window.confirm(`Delete user "${u.username}"? This is a soft delete.`)) {
     remove.mutate(u.id);
       }
       }
   },
      }}
     >
   <Tooltip title="Actions">
       <Button type="text" icon={<MoreOutlined />} />
     </Tooltip>
        </Dropdown>
  );
      },
    },
  ];

  return (
    <>
      <Space direction="vertical" style={{ width: '100%' }} size="middle">
        <Space style={{ justifyContent: 'space-between', width: '100%' }}>
   <Typography.Title level={3} style={{ margin: 0 }}>
   User Management
   </Typography.Title>
     <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
     New user
     </Button>
    </Space>

    <Space>
       <Input
      allowClear
      prefix={<SearchOutlined />}
     placeholder="Search username, email or name…"
       value={search}
    onChange={(e) => setSearch(e.target.value)}
       style={{ width: 340 }}
    />
     <Button icon={<ReloadOutlined />} onClick={() => void refetch()} loading={isFetching}>
 Refresh
   </Button>
     <Typography.Text type="secondary">{total} user(s)</Typography.Text>
    </Space>

        {error && (
       <Alert type="error" showIcon message="Failed to load users" description={error.message} />
      )}

     <Table<UserRecord>
     rowKey="id"
     columns={columns}
    dataSource={users}
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
       onChange: (p, s) => {
       setPage(p);
     if (s !== pageSize) setPageSize(s);
       },
     }}
   />
      </Space>

      <UserFormDrawer open={formOpen} user={editing} onClose={() => setFormOpen(false)} />
      <PasswordResetModal
 open={Boolean(resetTarget)}
    user={resetTarget}
    onClose={() => setResetTarget(null)}
    />
    </>
  );
}
