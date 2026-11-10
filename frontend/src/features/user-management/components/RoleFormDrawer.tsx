import { useEffect, useMemo, useState } from 'react';
import { Drawer, Form, Input, Button, Space, Collapse, Checkbox, Tag, Empty, Alert } from 'antd';
import { usePermissionsList, useRoleMutations } from '../hooks/useAccessControl';
import type { Role } from '@/types/access';

interface Props {
open: boolean;
  role: Role | null;
  onClose: () => void;
}

interface FormValues {
  name: string;
  displayName: string;
  description?: string;
}

/**
 * Create/edit a role and grant permissions in one place.
 * Permissions are grouped by category with select-all per group.
 */
export function RoleFormDrawer({ open, role, onClose }: Props) {
  const [form] = Form.useForm<FormValues>();
  const { data: permissions = [], isLoading } = usePermissionsList();
  const { create, update } = useRoleMutations();
  const [selected, setSelected] = useState<string[]>([]);
  const isEdit = Boolean(role);

  useEffect(() => {
 if (!open) return;
    if (role) {
  form.setFieldsValue({
    name: role.name,
        displayName: role.displayName,
   description: role.description ?? undefined,
      });
      setSelected(role.permissionIds);
    } else {
      form.resetFields();
    setSelected([]);
    }
  }, [open, role, form]);

  const grouped = useMemo(() => {
    const map = new Map<string, typeof permissions>();
    permissions.forEach((p) => {
      const list = map.get(p.category) ?? [];
      list.push(p);
  map.set(p.category, list);
    });
    return [...map.entries()].sort((a, b) => a[0].localeCompare(b[0]));
  }, [permissions]);

  const toggle = (id: string, checked: boolean) =>
    setSelected((prev) => (checked ? [...new Set([...prev, id])] : prev.filter((x) => x !== id)));

  const toggleCategory = (ids: string[], checked: boolean) =>
 setSelected((prev) =>
      checked ? [...new Set([...prev, ...ids])] : prev.filter((x) => !ids.includes(x)),
    );

  const handleSubmit = async (values: FormValues) => {
    const payload = {
      name: values.name,
      displayName: values.displayName,
      description: values.description ?? null,
      permissionIds: selected,
    };
    if (isEdit && role) {
      await update.mutateAsync({ id: role.id, payload });
    } else {
      await create.mutateAsync(payload);
    }
    onClose();
  };

  return (
    <Drawer
      title={isEdit ? `Edit role — ${role?.displayName}` : 'Create role'}
   open={open}
      onClose={onClose}
      width={640}
      destroyOnClose
      extra={
        <Space>
          <Button onClick={onClose}>Cancel</Button>
          <Button
    type="primary"
     loading={create.isPending || update.isPending}
  onClick={() => form.submit()}
          >
   {isEdit ? 'Save' : 'Create'}
          </Button>
   </Space>
 }
    >
      <Form<FormValues> form={form} layout="vertical" onFinish={handleSubmit}>
  <Form.Item
          name="name"
   label="Role name"
     rules={[{ required: true, message: 'Role name is required' }]}
       extra="Machine name used in code and tokens, e.g. BillingSpecialist."
        >
      <Input disabled={role?.isSystemRole} placeholder="BillingSpecialist" />
        </Form.Item>

        <Form.Item
          name="displayName"
          label="Display name"
 rules={[{ required: true, message: 'Display name is required' }]}
        >
        <Input placeholder="Billing Specialist" />
        </Form.Item>

        <Form.Item name="description" label="Description">
        <Input.TextArea rows={2} placeholder="What is this role for?" />
        </Form.Item>
      </Form>

      {role?.isSystemRole && (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
    message="System role"
          description="The role name is fixed, but you can freely change its permissions."
/>
      )}

      <Space style={{ marginBottom: 8 }}>
        <strong>Permissions</strong>
    <Tag color="blue">{selected.length} selected</Tag>
      </Space>

      {isLoading ? null : grouped.length === 0 ? (
        <Empty description="No permissions defined" />
    ) : (
        <Collapse
  defaultActiveKey={grouped.map(([c]) => c)}
     items={grouped.map(([category, perms]) => {
            const ids = perms.map((p) => p.id);
 const allChecked = ids.every((id) => selected.includes(id));
  const someChecked = ids.some((id) => selected.includes(id));
return {
       key: category,
     label: (
 <Space>
       <Checkbox
         checked={allChecked}
    indeterminate={!allChecked && someChecked}
  onClick={(e) => e.stopPropagation()}
       onChange={(e) => toggleCategory(ids, e.target.checked)}
       />
    <strong>{category}</strong>
 <Tag>{ids.filter((id) => selected.includes(id)).length}/{ids.length}</Tag>
     </Space>
    ),
              children: (
   <Space direction="vertical">
         {perms.map((p) => (
      <Checkbox
   key={p.id}
     checked={selected.includes(p.id)}
     onChange={(e) => toggle(p.id, e.target.checked)}
            >
     <code>{p.name}</code>
   {p.description ? ` — ${p.description}` : ''}
        </Checkbox>
     ))}
            </Space>
       ),
      };
          })}
        />
      )}
    </Drawer>
  );
}
