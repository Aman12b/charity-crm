import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiGet, apiPost, apiPut, toQuery } from '../../api/client';
import type { PagedResult, Role, User } from '../../api/types';

export interface UserFilter {
  role: Role | '';
  isActive: '' | 'true' | 'false';
  page: number;
}

export interface UserInput {
  email: string;
  firstName: string;
  lastName: string;
  role: Role;
}

const usersKey = ['users'];

export function useUsers(filter: UserFilter) {
  return useQuery({
    queryKey: [...usersKey, filter],
    queryFn: () => apiGet<PagedResult<User>>(`/users${toQuery({ ...filter, pageSize: 25 })}`),
    placeholderData: keepPreviousData,
  });
}

export function useUserMutations() {
  const queryClient = useQueryClient();
  const onSuccess = () => queryClient.invalidateQueries({ queryKey: usersKey });

  return {
    create: useMutation({
      mutationFn: (input: UserInput & { temporaryPassword: string }) => apiPost<User>('/users', input),
      onSuccess,
    }),
    update: useMutation({
      mutationFn: ({ id, ...input }: UserInput & { id: string; version: string }) =>
        apiPut<User>(`/users/${id}`, input),
      onSuccess,
    }),
    deactivate: useMutation({
      mutationFn: (id: string) => apiPost<User>(`/users/${id}/deactivate`),
      onSuccess,
    }),
    reactivate: useMutation({
      mutationFn: (id: string) => apiPost<User>(`/users/${id}/reactivate`),
      onSuccess,
    }),
    resetPassword: useMutation({
      mutationFn: ({ id, temporaryPassword }: { id: string; temporaryPassword: string }) =>
        apiPost<User>(`/users/${id}/reset-password`, { temporaryPassword }),
      onSuccess,
    }),
  };
}
