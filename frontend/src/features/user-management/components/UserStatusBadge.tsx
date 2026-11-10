import { Badge, Space } from 'antd';
import type { UserRecord } from '@/types/users';

/** Composite status: Locked takes precedence over Inactive over Active. */
export function UserStatusBadge({ user }: { user: UserRecord }) {
  if (user.isLocked) {
    return (
      <Space>
     <Badge status="error" text="Locked" />
      </Space>
    );
  }
  if (!user.isActive) {
    return <Badge status="default" text="Inactive" />;
  }
  return <Badge status="success" text="Active" />;
}
