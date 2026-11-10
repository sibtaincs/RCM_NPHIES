import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { message } from 'antd';
import { accessApi } from '@/api/endpoints/access';
import { getApiErrorMessage } from '@/utils/apiError';
import type { SavePermissionPayload, SaveRolePayload } from '@/types/access';

const ROLES_KEY = 'access-roles';
const PERMS_KEY = 'access-permissions';

export function useRolesList() {
  return useQuery({ queryKey: [ROLES_KEY], queryFn: accessApi.listRoles });
}

export function usePermissionsList() {
  return useQuery({ queryKey: [PERMS_KEY], queryFn: accessApi.listPermissions });
}

export function useEffectiveAccess(userId?: string) {
  return useQuery({
    queryKey: ['effective-access', userId],
    queryFn: () => accessApi.getEffectiveAccess(userId!),
    enabled: Boolean(userId),
  });
}

const onError = (e: unknown) => message.error(getApiErrorMessage(e));

export function useRoleMutations() {
  const qc = useQueryClient();
  const invalidate = () => qc.invalidateQueries({ queryKey: [ROLES_KEY] });

  const create = useMutation({
    mutationFn: (payload: SaveRolePayload) => accessApi.createRole(payload),
    onSuccess: () => {
      message.success('Role created');
      void invalidate();
    },
    onError,
  });

  const update = useMutation({
 mutationFn: ({ id, payload }: { id: string; payload: SaveRolePayload }) =>
      accessApi.updateRole(id, payload),
    onSuccess: () => {
      message.success('Role updated');
      void invalidate();
    },
    onError,
  });

  const remove = useMutation({
    mutationFn: (id: string) => accessApi.deleteRole(id),
    onSuccess: () => {
      message.success('Role deleted');
      void invalidate();
    },
    onError,
  });

  const setPermissions = useMutation({
    mutationFn: ({ id, permissionIds }: { id: string; permissionIds: string[] }) =>
      accessApi.setRolePermissions(id, permissionIds),
    onSuccess: () => {
      message.success('Permissions saved');
      void invalidate();
    },
    onError,
  });

  return { create, update, remove, setPermissions };
}

export function usePermissionMutations() {
  const qc = useQueryClient();
  const invalidate = () => {
    void qc.invalidateQueries({ queryKey: [PERMS_KEY] });
    void qc.invalidateQueries({ queryKey: [ROLES_KEY] });
  };

  const create = useMutation({
    mutationFn: (payload: SavePermissionPayload) => accessApi.createPermission(payload),
    onSuccess: () => {
      message.success('Permission created');
      invalidate();
    },
    onError,
  });

  const update = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: SavePermissionPayload }) =>
      accessApi.updatePermission(id, payload),
    onSuccess: () => {
      message.success('Permission updated');
      invalidate();
    },
    onError,
  });

  const remove = useMutation({
    mutationFn: (id: string) => accessApi.deletePermission(id),
    onSuccess: () => {
    message.success('Permission deleted');
      invalidate();
    },
    onError,
  });

  return { create, update, remove };
}
