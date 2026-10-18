import { Code, Group, List, Paper, Select, Table, Text, TextInput } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { apiGet } from '../../api/client';
import type { PagedResult, User } from '../../api/types';
import { EmptyRow, ErrorState, LoadingState, TablePager } from '../../components/common';
import { DateField } from '../../components/DateField';
import { PageHeader } from '../../components/PageHeader';
import { formatDateTime } from '../../lib/format';
import { useAuditLog, type AuditFilter } from '../reports/reportsApi';
import { actionLabels, entityTypeLabels, parseChanges } from './auditFormat';

const entityOptions = Object.entries(entityTypeLabels).map(([value, label]) => ({ value, label }));

/** FR-AUD-02: read-only audit log, filterable by user, entity and date range (Admin only). */
export function AuditPage() {
  const [filter, setFilter] = useState<Omit<AuditFilter, 'entityId'>>({
    userId: '',
    entityType: '',
    from: '',
    to: '',
    page: 1,
  });
  const [entityId, setEntityId] = useState('');
  const [debouncedEntityId] = useDebouncedValue(entityId, 300);
  const { data, isLoading, error } = useAuditLog({ ...filter, entityId: debouncedEntityId });
  const users = useQuery({
    queryKey: ['users', 'all-for-audit'],
    queryFn: () => apiGet<PagedResult<User>>('/users?pageSize=100'),
  });
  const set = (patch: Partial<AuditFilter>) => setFilter({ ...filter, ...patch, page: 1 });

  return (
    <>
      <PageHeader title="Protokoll" />
      <Paper withBorder p="md">
        <Group mb="md" align="flex-end">
          <Select
            label="Benutzer"
            placeholder="Alle"
            searchable
            clearable
            data={(users.data?.items ?? []).map((u) => ({
              value: u.id,
              label: `${u.lastName} ${u.firstName}`,
            }))}
            value={filter.userId || null}
            onChange={(v) => set({ userId: v ?? '' })}
            w={220}
          />
          <Select
            label="Objekttyp"
            placeholder="Alle"
            clearable
            data={entityOptions}
            value={filter.entityType || null}
            onChange={(v) => set({ entityType: v ?? '' })}
            w={170}
          />
          <TextInput
            label="Objekt-ID"
            value={entityId}
            onChange={(e) => {
              setEntityId(e.currentTarget.value);
              set({});
            }}
            w={300}
          />
          <DateField label="von" clearable value={filter.from} onChange={(from) => set({ from })} w={160} />
          <DateField label="bis" clearable value={filter.to} onChange={(to) => set({ to })} w={160} />
        </Group>

        {error && <ErrorState error={error} />}
        {isLoading ? (
          <LoadingState />
        ) : (
          data && (
            <>
              <Table.ScrollContainer minWidth={900}>
                <Table striped verticalSpacing="xs">
                  <Table.Thead>
                    <Table.Tr>
                      <Table.Th>Zeitpunkt</Table.Th>
                      <Table.Th>Benutzer</Table.Th>
                      <Table.Th>Aktion</Table.Th>
                      <Table.Th>Objekt</Table.Th>
                      <Table.Th>Änderungen</Table.Th>
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {data.items.length === 0 && <EmptyRow colSpan={5} />}
                    {data.items.map((a) => (
                      <Table.Tr key={a.id}>
                        <Table.Td style={{ whiteSpace: 'nowrap' }}>{formatDateTime(a.timestamp)}</Table.Td>
                        <Table.Td>{a.userName ?? '–'}</Table.Td>
                        <Table.Td>{actionLabels[a.action] ?? a.action}</Table.Td>
                        <Table.Td>
                          {a.entityType && (
                            <>
                              <Text size="sm">{entityTypeLabels[a.entityType] ?? a.entityType}</Text>
                              {a.entityId && <Code>{a.entityId}</Code>}
                            </>
                          )}
                        </Table.Td>
                        <Table.Td>
                          <List size="xs" listStyleType="none">
                            {parseChanges(a.changes).map((c) => (
                              <List.Item key={c.field}>
                                <b>{c.field}</b>: {c.old} → {c.new}
                              </List.Item>
                            ))}
                          </List>
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
    </>
  );
}
