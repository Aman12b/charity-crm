import { AppShell, Burger, Group, NavLink, Text, Title } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import type { ReactNode } from 'react';
import { NavLink as RouterNavLink, Outlet, useLocation } from 'react-router';
import { navItemsFor, type Role } from './navigation';

interface AppLayoutProps {
  role?: Role;
  headerRight?: ReactNode;
}

export function AppLayout({ role, headerRight }: AppLayoutProps) {
  const [opened, { toggle, close }] = useDisclosure();
  const location = useLocation();

  const isActive = (to: string) => (to === '/' ? location.pathname === '/' : location.pathname.startsWith(to));

  return (
    <AppShell
      header={{ height: 56 }}
      navbar={{ width: 230, breakpoint: 'sm', collapsed: { mobile: !opened } }}
      padding="md"
    >
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between">
          <Group gap="sm">
            <Burger opened={opened} onClick={toggle} hiddenFrom="sm" size="sm" aria-label="Navigation umschalten" />
            <Title order={3} c="teal.8">
              Charity CRM
            </Title>
          </Group>
          {headerRight}
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="xs" aria-label="Hauptnavigation">
        {navItemsFor(role).map((item) => (
          <NavLink
            key={item.to}
            component={RouterNavLink}
            to={item.to}
            label={item.label}
            leftSection={<item.icon size={18} stroke={1.6} />}
            active={isActive(item.to)}
            onClick={close}
          />
        ))}
        {!role && (
          <Text size="sm" c="dimmed" p="xs">
            Keine Navigation verfügbar.
          </Text>
        )}
      </AppShell.Navbar>

      <AppShell.Main>
        <Outlet />
      </AppShell.Main>
    </AppShell>
  );
}
