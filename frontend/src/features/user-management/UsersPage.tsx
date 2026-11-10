import { Tabs } from 'antd';
import { UsersListPage } from './UsersListPage';
import { RolesPage } from './RolesPage';
import { PermissionsPage } from './PermissionsPage';
import { EffectiveAccessPage } from './EffectiveAccessPage';

/** Admin hub for access control: users, roles, permissions and effective access. */
export function UsersPage() {
  return (
    <Tabs
      defaultActiveKey="users"
    destroyInactiveTabPane
      items={[
        { key: 'users', label: 'Users', children: <UsersListPage /> },
        { key: 'roles', label: 'Roles', children: <RolesPage /> },
        { key: 'permissions', label: 'Permissions', children: <PermissionsPage /> },
        { key: 'effective', label: 'Effective Access', children: <EffectiveAccessPage /> },
      ]}
    />
  );
}
