import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiGet, apiPost, apiPut, downloadFile, toQuery } from '../../api/client';
import type {
  BillingInterval,
  DuplicateMember,
  Member,
  MemberInput,
  MemberListItem,
  MemberStatus,
  PagedResult,
} from '../../api/types';

export interface MemberFilter {
  search: string;
  status: MemberStatus | '';
  interval: BillingInterval | '';
  page: number;
}

const membersKey = ['members'];

/** Empty optional fields are sent as null so the API stores "no value". */
function toBody(input: MemberInput) {
  return {
    ...input,
    country: input.country.trim() || null,
    email: input.email.trim() || null,
    phone: input.phone.trim() || null,
    notes: input.notes.trim() || null,
    joinDate: input.joinDate || null,
  };
}

export function useMembers(filter: MemberFilter) {
  return useQuery({
    queryKey: [...membersKey, 'list', filter],
    queryFn: () => apiGet<PagedResult<MemberListItem>>(`/members${toQuery({ ...filter })}`),
    placeholderData: keepPreviousData,
  });
}

export function useMember(id: string | undefined) {
  return useQuery({
    queryKey: [...membersKey, 'detail', id],
    queryFn: () => apiGet<Member>(`/members/${id}`),
    enabled: !!id,
  });
}

export function exportMembers(filter: Omit<MemberFilter, 'page'>) {
  return downloadFile(`/members/export.csv${toQuery({ ...filter })}`, 'Mitglieder.csv');
}

export function checkDuplicates(input: Pick<MemberInput, 'firstName' | 'lastName' | 'dateOfBirth'>) {
  return apiPost<DuplicateMember[]>('/members/duplicate-check', input);
}

export function useMemberMutations() {
  const queryClient = useQueryClient();
  const onSuccess = () => queryClient.invalidateQueries({ queryKey: membersKey });

  return {
    create: useMutation({
      mutationFn: ({ input, confirmDuplicate }: { input: MemberInput; confirmDuplicate: boolean }) =>
        apiPost<Member>('/members', { ...toBody(input), confirmDuplicate }),
      onSuccess,
    }),
    update: useMutation({
      mutationFn: ({ id, input, rowVersion }: { id: string; input: MemberInput; rowVersion: number }) =>
        apiPut<Member>(`/members/${id}`, { ...toBody(input), rowVersion }),
      onSuccess,
    }),
    deregister: useMutation({
      mutationFn: ({ id, leaveDate, reason }: { id: string; leaveDate: string; reason: string }) =>
        apiPost<Member>(`/members/${id}/deregister`, { leaveDate, reason: reason.trim() || null }),
      onSuccess,
    }),
    reactivate: useMutation({
      mutationFn: (id: string) => apiPost<Member>(`/members/${id}/reactivate`),
      onSuccess,
    }),
  };
}
