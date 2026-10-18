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
