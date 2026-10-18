import { ActionIcon, Badge, Button, Group, Menu, Paper, Select, Table } from '@mantine/core';
import { IconDots, IconPlus } from '@tabler/icons-react';
import { useState } from 'react';
import { roleLabels, roleOptions, type Role, type User } from '../../api/types';
import { useAuth } from '../../auth/useAuth';
import { EmptyRow, ErrorState, LoadingState, TablePager } from '../../components/common';
import { confirmAction } from '../../components/confirm';
import { PageHeader } from '../../components/PageHeader';
import { formatDateTime } from '../../lib/format';
import { notifyError, notifySuccess } from '../../lib/forms';
import { ResetPasswordModal } from './ResetPasswordModal';
import { UserFormModal } from './UserFormModal';
import { useUserMutations, useUsers, type UserFilter } from './usersApi';

const statusOptions = [
  { value: 'true', label: 'Aktiv' },
  { value: 'false', label: 'Inaktiv' },
];

/** FR-USR-01..06 */
export function UsersPage() {
  const { user: currentUser } = useAuth();
  const [filter, setFilter] = useState<UserFilter>({ role: '', isActive: '', page: 1 });
  const [editing, setEditing] = useState<User | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [resetting, setResetting] = useState<User | null>(null);
  const { data, isLoading, error } = useUsers(filter);
  const { deactivate, reactivate } = useUserMutations();

  const openCreate = () => {
    setEditing(null);
    setFormOpen(true);
  };
  const openEdit = (user: User) => {
    setEditing(user);
    setFormOpen(true);
  };

  const toggleActive = (user: User) =>
    confirmAction({
      title: user.isActive ? 'Benutzer deaktivieren' : 'Benutzer reaktivieren',
      message: user.isActive
        ? `${user.firstName} ${user.lastName} wird deaktiviert und sofort abgemeldet.`
        : `${user.firstName} ${user.lastName} kann sich danach wieder anmelden.`,
      confirmLabel: user.isActive ? 'Deaktivieren' : 'Reaktivieren',
      danger: user.isActive,
      onConfirm: async () => {
        try {
          await (user.isActive ? deactivate : reactivate).mutateAsync(user.id);
          notifySuccess(
            user.isActive ? 'Der Benutzer wurde deaktiviert.' : 'Der Benutzer wurde reaktiviert.',
          );
        } catch (e) {
          notifyError(e);
        }
      },
    });

  return (
    <>
      <PageHeader
        title="Benutzerverwaltung"
        actions={
          <Button leftSection={<IconPlus size={16} />} onClick={openCreate}>
            Neuer Benutzer
          </Button>
        }
      />

      <Paper withBorder p="md">
        <Group mb="md">
          <Select
            label="Rolle"
            placeholder="Alle"
            data={roleOptions}
            clearable
            value={filter.role || null}
            onChange={(v) => setFilter({ ...filter, role: (v ?? '') as Role | '', page: 1 })}
          />
          <Select
            label="Status"
            placeholder="Alle"
            data={statusOptions}
            clearable
            value={filter.isActive || null}
            onChange={(v) => setFilter({ ...filter, isActive: (v ?? '') as UserFilter['isActive'], page: 1 })}
          />
        </Group>

        {error && <ErrorState error={error} />}
        {isLoading ? (
          <LoadingState />
        ) : (
          data && (
            <>
              <Table.ScrollContainer minWidth={700}>
                <Table striped highlightOnHover>
                  <Table.Thead>
                    <Table.Tr>
                      <Table.Th>Name</Table.Th>
                      <Table.Th>E-Mail</Table.Th>
                      <Table.Th>Rolle</Table.Th>
                      <Table.Th>Status</Table.Th>
                      <Table.Th>Letzte Anmeldung</Table.Th>
                      <Table.Th aria-label="Aktionen" />
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {data.items.length === 0 && <EmptyRow colSpan={6} />}
                    {data.items.map((u) => (
                      <Table.Tr key={u.id}>
                        <Table.Td>
                          {u.lastName} {u.firstName}
                        </Table.Td>
                        <Table.Td>{u.email}</Table.Td>
                        <Table.Td>{roleLabels[u.role]}</Table.Td>
                        <Table.Td>
                          <Badge color={u.isActive ? 'teal' : 'gray'} variant="light">
                            {u.isActive ? 'Aktiv' : 'Inaktiv'}
                          </Badge>
                        </Table.Td>
                        <Table.Td>{formatDateTime(u.lastLoginAt)}</Table.Td>
                        <Table.Td>
                          <Menu position="bottom-end" withinPortal>
                            <Menu.Target>
                              <ActionIcon
                                variant="subtle"
                                aria-label={`Aktionen für ${u.firstName} ${u.lastName}`}
                              >
                                <IconDots size={18} />
                              </ActionIcon>
                            </Menu.Target>
                            <Menu.Dropdown>
                              <Menu.Item onClick={() => openEdit(u)}>Bearbeiten</Menu.Item>
                              <Menu.Item onClick={() => setResetting(u)}>Passwort zurücksetzen</Menu.Item>
                              <Menu.Item
                                color={u.isActive ? 'red' : undefined}
                                disabled={u.id === currentUser?.id}
                                onClick={() => toggleActive(u)}
                              >
                                {u.isActive ? 'Deaktivieren' : 'Reaktivieren'}
                              </Menu.Item>
                            </Menu.Dropdown>
                          </Menu>
                        </Table.Td>
                      </Table.Tr>
                    ))}
                  </Table.Tbody>
                </Table>
              </Table.ScrollContainer>
              <TablePager
                page={data.page}
                pageSize={data.pageSize}
                totalCount={data.totalCount}
                onChange={(page) => setFilter({ ...filter, page })}
              />
            </>
          )
        )}
      </Paper>

      <UserFormModal user={editing} opened={formOpen} onClose={() => setFormOpen(false)} />
      <ResetPasswordModal user={resetting} onClose={() => setResetting(null)} />
    </>
  );
}
