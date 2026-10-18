import type { Role } from '../components/navigation';

export type { Role };

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CurrentUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: Role;
  mustChangePassword: boolean;
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: Role;
  isActive: boolean;
  mustChangePassword: boolean;
  lastLoginAt: string | null;
  createdAt: string;
  version: string;
}

export const roleLabels: Record<Role, string> = {
  Admin: 'Administrator',
  Cashier: 'Kassier',
  Secretary: 'Schriftführer',
};

export const roleOptions = Object.entries(roleLabels).map(([value, label]) => ({ value, label }));

export type BillingInterval = 'Monthly' | 'Yearly';
export type MemberStatus = 'Active' | 'Inactive';

export const intervalLabels: Record<BillingInterval, string> = { Monthly: 'Monatlich', Yearly: 'Jährlich' };
export const memberStatusLabels: Record<MemberStatus, string> = { Active: 'Aktiv', Inactive: 'Inaktiv' };
export const intervalOptions = Object.entries(intervalLabels).map(([value, label]) => ({ value, label }));
export const memberStatusOptions = Object.entries(memberStatusLabels).map(([value, label]) => ({
  value,
  label,
}));

export interface MemberListItem {
  id: string;
  memberNumber: string;
  firstName: string;
  lastName: string;
  city: string;
  email: string | null;
  billingInterval: BillingInterval;
  status: MemberStatus;
}

export interface Member extends MemberListItem {
  dateOfBirth: string;
  street: string;
  postalCode: string;
  country: string;
  phone: string | null;
  joinDate: string;
  leaveDate: string | null;
  leaveReason: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
  rowVersion: number;
}

export interface MemberInput {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  street: string;
  postalCode: string;
  city: string;
  country: string;
  email: string;
  phone: string;
  billingInterval: BillingInterval;
  joinDate: string;
  notes: string;
}

export interface DuplicateMember {
  id: string;
  memberNumber: string;
  firstName: string;
  lastName: string;
  city: string;
  status: MemberStatus;
}

export interface Settings {
  name: string;
  street: string;
  postalCode: string;
  city: string;
  country: string;
  email: string | null;
  phone: string | null;
  iban: string | null;
  bic: string | null;
  zvrNumber: string | null;
  invoiceFooterText: string | null;
  monthlyFee: number;
  yearlyFee: number;
  updatedAt: string;
}

export type PaymentMethod = 'BankTransfer' | 'Cash' | 'Other';
export type PaymentStatus = 'Recorded' | 'Cancelled';
export type InvoiceStatus = 'Issued' | 'Cancelled';
export type EmailStatus = 'NotApplicable' | 'Pending' | 'Sent' | 'Failed';

export const methodLabels: Record<PaymentMethod, string> = {
  BankTransfer: 'Überweisung',
  Cash: 'Bar',
  Other: 'Sonstige',
};
export const paymentStatusLabels: Record<PaymentStatus, string> = {
  Recorded: 'Erfasst',
  Cancelled: 'Storniert',
};
export const invoiceStatusLabels: Record<InvoiceStatus, string> = {
  Issued: 'Ausgestellt',
  Cancelled: 'Storniert',
};
export const emailStatusLabels: Record<EmailStatus, string> = {
  NotApplicable: 'Keine E-Mail',
  Pending: 'Ausstehend',
  Sent: 'Versendet',
  Failed: 'Fehlgeschlagen',
};
export const methodOptions = Object.entries(methodLabels).map(([value, label]) => ({ value, label }));
export const paymentStatusOptions = Object.entries(paymentStatusLabels).map(([value, label]) => ({
  value,
  label,
}));
export const invoiceStatusOptions = Object.entries(invoiceStatusLabels).map(([value, label]) => ({
  value,
  label,
}));
export const emailStatusOptions = Object.entries(emailStatusLabels).map(([value, label]) => ({
  value,
  label,
}));

export interface Payment {
  id: string;
  memberId: string;
  memberNumber: string;
  memberName: string;
  interval: BillingInterval;
  periodStart: string;
  periodEnd: string;
  amount: number;
  paymentDate: string;
  method: PaymentMethod;
  reference: string | null;
  status: PaymentStatus;
  invoiceId: string | null;
  invoiceNumber: string | null;
  createdAt: string;
}

export interface PaymentList extends PagedResult<Payment> {
  totalAmount: number;
}

export interface RecordPaymentResult {
  paymentId: string;
  invoiceId: string;
  invoiceNumber: string;
}

export interface InvoiceListItem {
  id: string;
  invoiceNumber: string;
  issueDate: string;
  memberId: string;
  memberNumber: string;
  memberName: string;
  periodStart: string;
  periodEnd: string;
  amount: number;
  status: InvoiceStatus;
  emailStatus: EmailStatus;
}

export interface Invoice {
  id: string;
  invoiceNumber: string;
  issueDate: string;
  memberId: string;
  paymentId: string;
  periodStart: string;
  periodEnd: string;
  amount: number;
  interval: BillingInterval;
  paymentMethod: PaymentMethod;
  paymentDate: string;
  paymentReference: string | null;
  member: {
    memberNumber: string;
    firstName: string;
    lastName: string;
    street: string;
    postalCode: string;
    city: string;
    country: string;
    email: string | null;
  };
  organization: { name: string };
  status: InvoiceStatus;
  cancelledAt: string | null;
  cancellationReason: string | null;
  emailStatus: EmailStatus;
  emailSentAt: string | null;
  emailLastError: string | null;
  createdAt: string;
}

export interface Dashboard {
  activeMembers: number;
  newMembersThisMonth: number;
  paymentsThisMonthCount: number | null;
  paymentsThisMonthSum: number | null;
  failedEmails: number | null;
  openContributions: number | null;
}

export interface OpenContribution {
  memberId: string;
  memberNumber: string;
  firstName: string;
  lastName: string;
  email: string | null;
  billingInterval: BillingInterval;
  periodStart: string;
  periodEnd: string;
  expectedAmount: number;
}

export interface OpenContributions extends PagedResult<OpenContribution> {
  year: number;
  month: number;
}

export interface PaymentSummary {
  year: number;
  months: { month: number; count: number; sum: number }[];
  totalCount: number;
  totalSum: number;
}

export interface AuditEntry {
  id: number;
  timestamp: string;
  userId: string | null;
  userName: string | null;
  action: string;
  entityType: string | null;
  entityId: string | null;
  changes: string | null;
}
