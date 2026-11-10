import { useState } from 'react';
import {
  Table,
  Button,
  Space,
  Typography,
  Tag,
  Alert,
  Modal,
  Form,
  Input,
  Popconfirm,
  Tooltip,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, LockOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { usePermissionsList, usePermissionMutations } from './hooks/useAccessControl';
import type { Permission } from '@/types/access';

interface FormValues {
  name: string;
  category: string;
  action?: string;
  description?: string;
}

/** Manage the granular permission catalogue. */
export function PermissionsPage() {
  const { data: permissions = [], isLoading, error } = usePermissionsList();
  const { create, update, remove } = usePermissionMutations();
  const [form] = Form.useForm<FormValues>();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Permission | null>(null);

  const openModal = (permission: Permission | null) => {
    setEditing(permission);
    if (permission) {
   form.setFieldsValue({
    name: permission.name,
      category: permission.category,
        action: permission.action,
        description: permission.description ?? undefined,
      });
    } else {
   form.resetFields();
  }
    setOpen(true);
  };

  const handleOk = async () => {
    const values = await form.validateFields();
    if (editing) {
      await update.mutateAsync({ id: editing.id, payload: values });
    } else {
      await create.mutateAsync(values);
    }
    setOpen(false);
  };

  const columns: ColumnsType<Permission> = [
    {
      title: 'Permission',
      dataIndex: 'name',
      width: 240,
      render: (v: string, p) => (
     <Space>
   <code>{v}</code>
     {p.isSystemPermission && (
      <Tooltip title="System permission — referenced by code">
   <LockOutlined style={{ color: '#999' }} />
      </Tooltip>
    )}
        </Space>
      ),
    },
    {
   title: 'Category',
  dataIndex: 'category',
      width: 160,
      render: (v: string) => <Tag color="geekblue">{v}</Tag>,
      filters: [...new Set(permissions.map((p) => p.category))].map((c) => ({ text: c, value: c })),
    onFilter: (value, record) => record.category === value,
    },
    { title: 'Action', dataIndex: 'action', width: 140 },
    { title: 'Description', dataIndex: 'description', render: (v?: string) => v ?? '—' },
    {
    title: '',
      key: 'actions',
      width: 110,
  fixed: 'right',
      render: (_, p) => (
        <Space>
    <Button size="small" icon={<EditOutlined />} onClick={() => openModal(p)} />
      <Popconfirm
     title="Delete this permission?"
    onConfirm={() => remove.mutate(p.id)}
   disabled={p.isSystemPermission}
    >
   <Button
   size="small"
       danger
   icon={<DeleteOutlined />}
    disabled={p.isSystemPermission}
     />
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
Permissions
    </Typography.Title>
     <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal(null)}>
   New permission
    </Button>
        </Space>

    <Alert
    type="info"
     showIcon
   message="Permissions are the building blocks of access"
      description="Grant them to roles from the Roles tab. Custom permissions only take effect once the backend checks them with [HasPermission]."
     />

        {error && (
  <Alert type="error" showIcon message="Failed to load permissions" description={(error as Error).message} />
)}

     <Table<Permission>
   rowKey="id"
     columns={columns}
     dataSource={permissions}
     loading={isLoading}
   size="middle"
     pagination={{ pageSize: 20, showSizeChanger: true }}
    scroll={{ x: 'max-content' }}
    />
      </Space>

      <Modal
        title={editing ? `Edit permission — ${editing.name}` : 'New permission'}
        open={open}
        onCancel={() => setOpen(false)}
        onOk={handleOk}
   okButtonProps={{ loading: create.isPending || update.isPending }}
        destroyOnClose
      >
        <Form<FormValues> form={form} layout="vertical">
   <Form.Item
   name="name"
   label="Name"
   rules={[{ required: true, message: 'Name is required' }]}
    extra="Use category:action form, e.g. reports:export"
     >
      <Input disabled={editing?.isSystemPermission} placeholder="reports:export" />
   </Form.Item>
      <Form.Item
 name="category"
   label="Category"
     rules={[{ required: true, message: 'Category is required' }]}
   >
 <Input placeholder="Reports" />
   </Form.Item>
   <Form.Item name="action" label="Action">
     <Input placeholder="export" />
     </Form.Item>
   <Form.Item name="description" label="Description">
     <Input.TextArea rows={2} />
     </Form.Item>
    </Form>
      </Modal>
    </>
  );
}
