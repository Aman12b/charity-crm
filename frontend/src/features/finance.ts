import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiGet, apiPost, apiPut, downloadFile, toQuery } from '../api/client';
import type {
  EmailStatus,
  Invoice,
  InvoiceListItem,
  InvoiceStatus,
  PagedResult,
  Payment,
  PaymentList,
  PaymentMethod,
  PaymentStatus,
  RecordPaymentResult,
  Settings,
} from '../api/types';

// Query hooks for settings, payments and invoices. Any write invalidates all finance data.

const financeKeys = ['settings', 'payments', 'invoices', 'members', 'reports'];

function useInvalidateFinance() {
  const queryClient = useQueryClient();
  return () => Promise.all(financeKeys.map((key) => queryClient.invalidateQueries({ queryKey: [key] })));
}

export function useSettings() {
  return useQuery({ queryKey: ['settings'], queryFn: () => apiGet<Settings>('/settings') });
}

export type SettingsInput = Omit<Settings, 'updatedAt'>;

export function useUpdateSettings() {
  const invalidate = useInvalidateFinance();
  return useMutation({
    mutationFn: (input: SettingsInput) => apiPut<Settings>('/settings', input),
    onSuccess: invalidate,
  });
}

export interface PaymentFilter {
  from: string;
  to: string;
  memberId: string;
  method: PaymentMethod | '';
  status: PaymentStatus | '';
  periodYear: number | '';
  periodMonth: number | '';
  page: number;
}

export function usePayments(filter: PaymentFilter) {
  return useQuery({
    queryKey: ['payments', 'list', filter],
    queryFn: () => apiGet<PaymentList>(`/payments${toQuery({ ...filter })}`),
    placeholderData: keepPreviousData,
  });
}

export function useMemberPayments(memberId: string) {
  return useQuery({
    queryKey: ['payments', 'member', memberId],
    queryFn: () => apiGet<Payment[]>(`/members/${memberId}/payments`),
  });
}

export interface RecordPaymentInput {
  memberId: string;
  year: number;
  month: number | null;
  amount: number;
  paymentDate: string;
  method: PaymentMethod;
  reference: string | null;
}

export function useRecordPayment() {
  const invalidate = useInvalidateFinance();
  return useMutation({
    mutationFn: (input: RecordPaymentInput) => apiPost<RecordPaymentResult>('/payments', input),
    onSuccess: invalidate,
  });
}

export interface InvoiceFilter {
  search: string;
  from: string;
  to: string;
  status: InvoiceStatus | '';
  emailStatus: EmailStatus | '';
  page: number;
}

export function useInvoices(filter: InvoiceFilter) {
  return useQuery({
    queryKey: ['invoices', 'list', filter],
    queryFn: () => apiGet<PagedResult<InvoiceListItem>>(`/invoices${toQuery({ ...filter })}`),
    placeholderData: keepPreviousData,
  });
}

export function useMemberInvoices(memberId: string) {
  return useQuery({
    queryKey: ['invoices', 'member', memberId],
    queryFn: () => apiGet<InvoiceListItem[]>(`/members/${memberId}/invoices`),
  });
}

export function useInvoice(id: string | undefined) {
  return useQuery({
    queryKey: ['invoices', 'detail', id],
    queryFn: () => apiGet<Invoice>(`/invoices/${id}`),
    enabled: !!id,
  });
}

export function downloadInvoicePdf(invoice: { id: string; invoiceNumber: string }) {
  return downloadFile(`/invoices/${invoice.id}/pdf`, `Rechnung_${invoice.invoiceNumber}.pdf`);
}

export function useCancelInvoice() {
  const invalidate = useInvalidateFinance();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      apiPost<Invoice>(`/invoices/${id}/cancel`, { reason }),
    onSuccess: invalidate,
  });
}
