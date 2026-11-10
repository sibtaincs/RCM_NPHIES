import { apiClient } from '@/api/client';
import type {
  EffectiveAccess,
  Permission,
  Role,
  SavePermissionPayload,
  SaveRolePayload,
} from '@/types/access';

/** Typed wrappers around the AccessControlController (/api/access). */
export const accessApi = {
  // ---- Roles ----
  listRoles: async (): Promise<Role[]> => {
    const { data } = await apiClient.get<Role[]>('/access/roles');
    return data ?? [];
  },
  createRole: async (payload: SaveRolePayload): Promise<Role> => {
    const { data } = await apiClient.post<Role>('/access/roles', payload);
    return data;
  },
  updateRole: async (id: string, payload: SaveRolePayload): Promise<Role> => {
    const { data } = await apiClient.put<Role>(`/access/roles/${id}`, payload);
    return data;
  },
  deleteRole: async (id: string): Promise<void> => {
  await apiClient.delete(`/access/roles/${id}`);
  },
  /** Replace a role's full permission set (used by the matrix UI). */
  setRolePermissions: async (id: string, permissionIds: string[]): Promise<Role> => {
    const { data } = await apiClient.put<Role>(`/access/roles/${id}/permissions`, {
      permissionIds,
    });
    return data;
  },

  // ---- Permissions ----
  listPermissions: async (): Promise<Permission[]> => {
    const { data } = await apiClient.get<Permission[]>('/access/permissions');
    return data ?? [];
  },
  createPermission: async (payload: SavePermissionPayload): Promise<Permission> => {
  const { data } = await apiClient.post<Permission>('/access/permissions', payload);
    return data;
  },
  updatePermission: async (id: string, payload: SavePermissionPayload): Promise<Permission> => {
    const { data } = await apiClient.put<Permission>(`/access/permissions/${id}`, payload);
    return data;
  },
  deletePermission: async (id: string): Promise<void> => {
    await apiClient.delete(`/access/permissions/${id}`);
  },

  // ---- Effective access ----
  getEffectiveAccess: async (userId: string): Promise<EffectiveAccess> => {
    const { data } = await apiClient.get<EffectiveAccess>(
      `/access/users/${userId}/effective-permissions`,
    );
    return data;
  },
};
