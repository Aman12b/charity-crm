import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { apiGet, downloadFile, toQuery } from '../../api/client';
import type { AuditEntry, Dashboard, OpenContributions, PagedResult, PaymentSummary } from '../../api/types';

export function useDashboard() {
  return useQuery({
    queryKey: ['reports', 'dashboard'],
    queryFn: () => apiGet<Dashboard>('/reports/dashboard'),
  });
}

export interface OpenFilter {
  year: number;
  month: number;
  search: string;
  page: number;
}

export function useOpenContributions(filter: OpenFilter) {
  return useQuery({
    queryKey: ['reports', 'open', filter],
    queryFn: () => apiGet<OpenContributions>(`/reports/open-contributions${toQuery({ ...filter })}`),
    placeholderData: keepPreviousData,
  });
}

export function usePaymentSummary(year: number) {
  return useQuery({
    queryKey: ['reports', 'summary', year],
    queryFn: () => apiGet<PaymentSummary>(`/reports/payment-summary?year=${year}`),
    placeholderData: keepPreviousData,
  });
}

export function exportPaymentSummary(year: number) {
  return downloadFile(`/reports/payment-summary.csv?year=${year}`, `Zahlungsuebersicht_${year}.csv`);
}

export interface AuditFilter {
  userId: string;
  entityType: string;
  entityId: string;
  from: string;
  to: string;
  page: number;
}

export function useAuditLog(filter: AuditFilter) {
  return useQuery({
    queryKey: ['audit', filter],
    queryFn: () => apiGet<PagedResult<AuditEntry>>(`/audit${toQuery({ ...filter })}`),
    placeholderData: keepPreviousData,
  });
}
