import { apiClient } from '@/api/client';
import type { AppRole } from '@/types/auth';
import type {
  CreateUserPayload,
  UpdateUserPayload,
  UserRecord,
  UsersPage,
} from '@/types/users';

/** Typed wrappers around the admin UsersController (/api/users). */
export const usersApi = {
  /**
   * Role names available for assignment.
   * @deprecated Prefer `accessApi.listRoles()` — the data-driven catalogue that
   * also exposes display names and permission counts. Kept for compatibility.
   */
  getRoles: async (): Promise<AppRole[]> => {
    const { data } = await apiClient.get<AppRole[]>('/users/roles');
    return data;
  },

  list: async (params: { skip: number; take: number; search?: string }): Promise<UsersPage> => {
    const { data } = await apiClient.get<UsersPage>('/users', {
      params: { skip: params.skip, take: params.take, search: params.search || undefined },
    });
    return { items: data?.items ?? [], total: data?.total ?? 0 };
  },

  get: async (id: string): Promise<UserRecord> => {
    const { data } = await apiClient.get<UserRecord>(`/users/${id}`);
    return data;
  },

  create: async (payload: CreateUserPayload): Promise<UserRecord> => {
    const { data } = await apiClient.post<UserRecord>('/users', payload);
    return data;
  },

  update: async (id: string, payload: UpdateUserPayload): Promise<UserRecord> => {
    const { data } = await apiClient.put<UserRecord>(`/users/${id}`, payload);
    return data;
  },

  setActive: async (id: string, isActive: boolean): Promise<UserRecord> => {
 const { data } = await apiClient.post<UserRecord>(`/users/${id}/set-active`, null, {
      params: { isActive },
    });
    return data;
  },

  unlock: async (id: string): Promise<UserRecord> => {
    const { data } = await apiClient.post<UserRecord>(`/users/${id}/unlock`);
    return data;
  },

  resetPassword: async (id: string, newPassword: string): Promise<void> => {
await apiClient.post(`/users/${id}/reset-password`, { newPassword });
  },

  remove: async (id: string): Promise<void> => {
    await apiClient.delete(`/users/${id}`);
  },
};
