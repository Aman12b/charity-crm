import {
  IconCash,
  IconChartBar,
  IconFileInvoice,
  IconHistory,
  IconLayoutDashboard,
  IconSettings,
  IconUserCog,
  IconUsers,
  type Icon,
} from '@tabler/icons-react';

export type Role = 'Admin' | 'Cashier' | 'Secretary';

export interface NavItem {
  label: string;
  to: string;
  icon: Icon;
  roles: Role[];
}

const all: Role[] = ['Admin', 'Cashier', 'Secretary'];
const finance: Role[] = ['Admin', 'Cashier'];
const admin: Role[] = ['Admin'];

// Sidebar entries; only those matching the user's role are shown (NFR-UX-03).
export const navItems: NavItem[] = [
  { label: 'Dashboard', to: '/', icon: IconLayoutDashboard, roles: all },
  { label: 'Mitglieder', to: '/mitglieder', icon: IconUsers, roles: all },
  { label: 'Zahlungen', to: '/zahlungen', icon: IconCash, roles: finance },
  { label: 'Rechnungen', to: '/rechnungen', icon: IconFileInvoice, roles: finance },
  { label: 'Berichte', to: '/berichte', icon: IconChartBar, roles: finance },
  { label: 'Benutzer', to: '/benutzer', icon: IconUserCog, roles: admin },
  { label: 'Einstellungen', to: '/einstellungen', icon: IconSettings, roles: admin },
  { label: 'Protokoll', to: '/protokoll', icon: IconHistory, roles: admin },
];

export function navItemsFor(role: Role | undefined): NavItem[] {
  return role ? navItems.filter((item) => item.roles.includes(role)) : [];
}
