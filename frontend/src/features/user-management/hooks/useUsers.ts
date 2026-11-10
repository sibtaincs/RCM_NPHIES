import { useState } from 'react';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { message } from 'antd';
import { usersApi } from '@/api/endpoints/users';
import { useDebounce } from '@/hooks/useDebounce';
import { getApiErrorMessage } from '@/utils/apiError';
import type { CreateUserPayload, UpdateUserPayload, UsersPage } from '@/types/users';

const USERS_KEY = 'users';

/** Paged + searchable user list. */
export function useUsers() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounce(search);

const query = useQuery<UsersPage>({
    queryKey: [USERS_KEY, page, pageSize, debouncedSearch],
    queryFn: () =>
      usersApi.list({ skip: (page - 1) * pageSize, take: pageSize, search: debouncedSearch }),
    placeholderData: keepPreviousData,
  });

  return {
    users: query.data?.items ?? [],
    total: query.data?.total ?? 0,
    isLoading: query.isLoading,
    isFetching: query.isFetching,
    error: query.error as Error | null,
    refetch: query.refetch,
    page,
    pageSize,
    search,
    setSearch: (v: string) => {
      setSearch(v);
      setPage(1);
    },
 setPage,
    setPageSize,
  };
}

/** The list of assignable role names (cached — rarely changes). */
export function useRoles() {
  return useQuery({
 queryKey: ['roles'],
    queryFn: usersApi.getRoles,
    staleTime: 5 * 60_000,
  });
}

/** All user mutations, each invalidating the list on success. */
export function useUserMutations() {
  const qc = useQueryClient();
  const invalidate = () => qc.invalidateQueries({ queryKey: [USERS_KEY] });

  const onError = (e: unknown) => message.error(getApiErrorMessage(e));

  const create = useMutation({
    mutationFn: (payload: CreateUserPayload) => usersApi.create(payload),
    onSuccess: () => {
    message.success('User created');
      void invalidate();
    },
    onError,
  });

  const update = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateUserPayload }) =>
    usersApi.update(id, payload),
    onSuccess: () => {
      message.success('User updated');
      void invalidate();
    },
    onError,
  });

  const setActive = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
    usersApi.setActive(id, isActive),
    onSuccess: (_d, v) => {
      message.success(v.isActive ? 'User activated' : 'User deactivated');
      void invalidate();
    },
onError,
  });

  const unlock = useMutation({
    mutationFn: (id: string) => usersApi.unlock(id),
    onSuccess: () => {
    message.success('User unlocked');
   void invalidate();
    },
    onError,
  });

  const resetPassword = useMutation({
    mutationFn: ({ id, newPassword }: { id: string; newPassword: string }) =>
      usersApi.resetPassword(id, newPassword),
    onSuccess: () => message.success('Password reset. The user must sign in again.'),
    onError,
  });

  const remove = useMutation({
    mutationFn: (id: string) => usersApi.remove(id),
    onSuccess: () => {
      message.success('User deleted');
      void invalidate();
    },
    onError,
  });

  return { create, update, setActive, unlock, resetPassword, remove };
}
