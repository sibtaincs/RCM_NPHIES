import { Modal, Form, Input, Alert } from 'antd';
import { useUserMutations } from '../hooks/useUsers';
import { MIN_PASSWORD_LENGTH } from '../schemas/userSchemas';
import type { UserRecord } from '@/types/users';

interface Props {
  open: boolean;
  user: UserRecord | null;
  onClose: () => void;
}

/** Admin password reset. Revokes the user's refresh tokens server-side. */
export function PasswordResetModal({ open, user, onClose }: Props) {
  const [form] = Form.useForm<{ newPassword: string; confirm: string }>();
  const { resetPassword } = useUserMutations();

  const handleOk = async () => {
  const values = await form.validateFields();
    if (!user) return;
    await resetPassword.mutateAsync({ id: user.id, newPassword: values.newPassword });
    form.resetFields();
    onClose();
  };

  return (
    <Modal
      title={`Reset password — ${user?.username ?? ''}`}
open={open}
      onCancel={onClose}
      onOk={handleOk}
      okText="Reset password"
    okButtonProps={{ danger: true, loading: resetPassword.isPending }}
   destroyOnClose
    >
      <Alert
        type="warning"
  showIcon
 style={{ marginBottom: 16 }}
    message="This signs the user out everywhere"
        description="All active sessions are revoked. The user must sign in again with the new password."
      />
      <Form form={form} layout="vertical">
  <Form.Item
    name="newPassword"
      label="New password"
      rules={[
   { required: true, message: 'Password is required' },
   { min: MIN_PASSWORD_LENGTH, message: `At least ${MIN_PASSWORD_LENGTH} characters` },
   ]}
   >
     <Input.Password autoComplete="new-password" />
        </Form.Item>
   <Form.Item
    name="confirm"
    label="Confirm password"
     dependencies={['newPassword']}
   rules={[
    { required: true, message: 'Please confirm the password' },
     ({ getFieldValue }) => ({
       validator(_, value) {
       if (!value || getFieldValue('newPassword') === value) return Promise.resolve();
      return Promise.reject(new Error('Passwords do not match'));
  },
        }),
      ]}
      >
   <Input.Password autoComplete="new-password" />
        </Form.Item>
      </Form>
    </Modal>
  );
}
