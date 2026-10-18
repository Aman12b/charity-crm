import { Badge, Button, Group, Paper, Select, Table, TextInput } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconDownload, IconPlus, IconSearch } from '@tabler/icons-react';
import { useState } from 'react';
import { useNavigate } from 'react-router';
import {
  intervalLabels,
  intervalOptions,
  memberStatusLabels,
  memberStatusOptions,
  type BillingInterval,
  type MemberStatus,
} from '../../api/types';
import { useHasRole } from '../../auth/useAuth';
import { EmptyRow, ErrorState, LoadingState, TablePager } from '../../components/common';
import { PageHeader } from '../../components/PageHeader';
import { notifyError } from '../../lib/forms';
import { exportMembers, useMembers, type MemberFilter } from './membersApi';

/** FR-MEM-02 list & search, FR-MEM-07 export. */
export function MembersPage() {
  const navigate = useNavigate();
  const canWrite = useHasRole('Admin', 'Secretary');
  const [search, setSearch] = useState('');
  const [debouncedSearch] = useDebouncedValue(search, 300);
  const [filter, setFilter] = useState<Omit<MemberFilter, 'search'>>({
    status: 'Active',
    interval: '',
    page: 1,
  });
  const fullFilter = { ...filter, search: debouncedSearch };
  const { data, isLoading, error } = useMembers(fullFilter);
  const [exporting, setExporting] = useState(false);

  const onExport = async () => {
    setExporting(true);
    try {
      await exportMembers({ search: debouncedSearch, status: filter.status, interval: filter.interval });
    } catch (e) {
      notifyError(e);
    } finally {
      setExporting(false);
    }
  };

  return (
    <>
      <PageHeader
        title="Mitglieder"
        actions={
          canWrite && (
            <>
              <Button
                variant="default"
                leftSection={<IconDownload size={16} />}
                loading={exporting}
                onClick={onExport}
              >
                CSV exportieren
              </Button>
              <Button leftSection={<IconPlus size={16} />} onClick={() => navigate('/mitglieder/neu')}>
                Neues Mitglied
              </Button>
            </>
          )
        }
      />

      <Paper withBorder p="md">
        <Group mb="md" align="flex-end">
          <TextInput
            label="Suche"
            placeholder="Name, Mitgliedsnummer oder E-Mail"
            leftSection={<IconSearch size={16} />}
            value={search}
            onChange={(e) => {
              setSearch(e.currentTarget.value);
              setFilter({ ...filter, page: 1 });
            }}
            w={320}
            maw="100%"
          />
          <Select
            label="Status"
            placeholder="Alle"
            data={memberStatusOptions}
            clearable
            value={filter.status || null}
            onChange={(v) => setFilter({ ...filter, status: (v ?? '') as MemberStatus | '', page: 1 })}
          />
          <Select
            label="Zahlungsintervall"
            placeholder="Alle"
            data={intervalOptions}
            clearable
            value={filter.interval || null}
            onChange={(v) => setFilter({ ...filter, interval: (v ?? '') as BillingInterval | '', page: 1 })}
          />
        </Group>

        {error && <ErrorState error={error} />}
        {isLoading ? (
          <LoadingState />
        ) : (
          data && (
            <>
              <Table.ScrollContainer minWidth={640}>
                <Table striped highlightOnHover>
                  <Table.Thead>
                    <Table.Tr>
                      <Table.Th>Nr.</Table.Th>
                      <Table.Th>Name</Table.Th>
                      <Table.Th>Ort</Table.Th>
                      <Table.Th>Intervall</Table.Th>
                      <Table.Th>Status</Table.Th>
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {data.items.length === 0 && <EmptyRow colSpan={5} />}
                    {data.items.map((m) => (
                      <Table.Tr
                        key={m.id}
                        style={{ cursor: 'pointer' }}
                        onClick={() => navigate(`/mitglieder/${m.id}`)}
                      >
                        <Table.Td>{m.memberNumber}</Table.Td>
                        <Table.Td>
                          {/* Real link for keyboard users (NFR-UX-05). */}
                          <a
                            href={`/mitglieder/${m.id}`}
                            onClick={(e) => {
                              e.preventDefault();
                              navigate(`/mitglieder/${m.id}`);
                            }}
                          >
                            {m.lastName} {m.firstName}
                          </a>
                        </Table.Td>
                        <Table.Td>{m.city}</Table.Td>
                        <Table.Td>{intervalLabels[m.billingInterval]}</Table.Td>
                        <Table.Td>
                          <Badge color={m.status === 'Active' ? 'teal' : 'gray'} variant="light">
                            {memberStatusLabels[m.status]}
                          </Badge>
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
