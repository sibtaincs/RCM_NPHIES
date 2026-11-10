import { useEffect } from 'react';
import { Drawer, Form, Input, Select, Button, Space, Alert } from 'antd';
import { useUserMutations } from '../hooks/useUsers';
import { useRolesList } from '../hooks/useAccessControl';
import { MIN_PASSWORD_LENGTH } from '../schemas/userSchemas';
import type { AppRole } from '@/types/auth';
import type { UserRecord } from '@/types/users';

interface UserFormDrawerProps {
  open: boolean;
  /** null = create mode; otherwise edit mode. */
  user: UserRecord | null;
  onClose: () => void;
}

interface FormValues {
  username: string;
  email: string;
password?: string;
  firstName?: string;
  lastName?: string;
  roles: AppRole[];
  organizationId?: string;
  departmentId?: string;
}

/** Create/Edit user drawer. Username is immutable after creation. */
export function UserFormDrawer({ open, user, onClose }: UserFormDrawerProps) {
  const [form] = Form.useForm<FormValues>();
  const { data: roles = [], isLoading: rolesLoading } = useRolesList();
  const { create, update } = useUserMutations();
  const isEdit = Boolean(user);

  useEffect(() => {
    if (!open) return;
    if (user) {
      form.setFieldsValue({
        username: user.username,
   email: user.email,
    firstName: user.firstName,
        lastName: user.lastName,
     roles: user.roles,
   organizationId: user.organizationId ?? undefined,
   departmentId: user.departmentId ?? undefined,
      });
    } else {
  form.resetFields();
    }
  }, [open, user, form]);

  const handleSubmit = async (values: FormValues) => {
    if (isEdit && user) {
      await update.mutateAsync({
    id: user.id,
      payload: {
    email: values.email,
     firstName: values.firstName,
     lastName: values.lastName,
      roles: values.roles,
   organizationId: values.organizationId ?? null,
      departmentId: values.departmentId ?? null,
        },
      });
    } else {
      await create.mutateAsync({
     username: values.username,
        email: values.email,
        password: values.password!,
   firstName: values.firstName,
   lastName: values.lastName,
     roles: values.roles,
        organizationId: values.organizationId ?? null,
   departmentId: values.departmentId ?? null,
    });
    }
    onClose();
  };

  const submitting = create.isPending || update.isPending;

  return (
    <Drawer
      title={isEdit ? `Edit user — ${user?.username}` : 'Create user'}
      open={open}
      onClose={onClose}
      width={520}
      destroyOnClose
    extra={
        <Space>
     <Button onClick={onClose}>Cancel</Button>
   <Button type="primary" loading={submitting} onClick={() => form.submit()}>
     {isEdit ? 'Save' : 'Create'}
   </Button>
      </Space>
      }
    >
<Form<FormValues> form={form} layout="vertical" onFinish={handleSubmit}>
        <Form.Item
   name="username"
   label="Username"
     rules={[{ required: true, min: 3, message: 'Username must be at least 3 characters' }]}
   >
      <Input disabled={isEdit} placeholder="jdoe" autoComplete="off" />
        </Form.Item>

        <Form.Item
name="email"
     label="Email"
    rules={[{ required: true, type: 'email', message: 'Enter a valid email address' }]}
        >
   <Input placeholder="user@example.com" autoComplete="off" />
        </Form.Item>

  {!isEdit && (
   <Form.Item
name="password"
  label="Temporary password"
    rules={[
     { required: true, message: 'Password is required' },
     { min: MIN_PASSWORD_LENGTH, message: `At least ${MIN_PASSWORD_LENGTH} characters` },
        ]}
    extra={`Minimum ${MIN_PASSWORD_LENGTH} characters. The user should change it after first sign-in.`}
    >
<Input.Password autoComplete="new-password" />
     </Form.Item>
     )}

      <Space style={{ display: 'flex' }} size="middle">
    <Form.Item name="firstName" label="First name" style={{ flex: 1 }}>
   <Input />
   </Form.Item>
   <Form.Item name="lastName" label="Last name" style={{ flex: 1 }}>
   <Input />
     </Form.Item>
    </Space>

        <Form.Item
     name="roles"
       label="Roles"
    rules={[{ required: true, message: 'Select at least one role' }]}
    extra="Roles determine which screens and actions the user can access. Manage them on the Roles tab."
        >
     <Select
    mode="multiple"
loading={rolesLoading}
     placeholder="Select one or more roles"
     optionFilterProp="label"
       options={roles
    .filter((r) => r.isActive)
      .map((r) => ({
   label: `${r.displayName} (${r.permissionNames.length} permissions)`,
     value: r.name,
     }))}
        />
</Form.Item>

        <Space style={{ display: 'flex' }} size="middle">
    <Form.Item name="organizationId" label="Organization ID" style={{ flex: 1 }}>
    <Input placeholder="optional" />
   </Form.Item>
    <Form.Item name="departmentId" label="Department ID" style={{ flex: 1 }}>
    <Input placeholder="optional" />
    </Form.Item>
        </Space>

   {isEdit && (
   <Alert
     type="info"
    showIcon
   message="Username cannot be changed."
     description="Use Reset password to issue new credentials."
     />
  )}
  </Form>
    </Drawer>
  );
}
