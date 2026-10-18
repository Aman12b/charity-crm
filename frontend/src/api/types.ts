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
