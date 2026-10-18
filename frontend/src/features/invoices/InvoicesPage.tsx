import { Anchor, Paper, Select, Table, TextInput, Group } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconSearch } from '@tabler/icons-react';
import { useState } from 'react';
import { Link } from 'react-router';
import {
  emailStatusOptions,
  invoiceStatusOptions,
  type EmailStatus,
  type InvoiceStatus,
} from '../../api/types';
import { EmailStatusBadge, InvoiceStatusBadge } from '../../components/badges';
import { EmptyRow, ErrorState, LoadingState, TablePager } from '../../components/common';
import { DateField } from '../../components/DateField';
import { PageHeader } from '../../components/PageHeader';
import { formatCurrency, formatDate, formatPeriod } from '../../lib/format';
import { useInvoices, type InvoiceFilter } from '../finance';

/** FR-INV-02: invoice list filterable by number/member, date range, status and email status. */
export function InvoicesPage() {
  const [search, setSearch] = useState('');
  const [debouncedSearch] = useDebouncedValue(search, 300);
  const [filter, setFilter] = useState<Omit<InvoiceFilter, 'search'>>({
    from: '',
    to: '',
    status: '',
    emailStatus: '',
    page: 1,
  });
  const { data, isLoading, error } = useInvoices({ ...filter, search: debouncedSearch });
  const set = (patch: Partial<InvoiceFilter>) => setFilter({ ...filter, ...patch, page: 1 });

  return (
    <>
      <PageHeader title="Rechnungen" />
      <Paper withBorder p="md">
        <Group mb="md" align="flex-end">
          <TextInput
            label="Suche"
            placeholder="Rechnungsnr., Mitgliedsnr. oder Name"
            leftSection={<IconSearch size={16} />}
            value={search}
            onChange={(e) => {
              setSearch(e.currentTarget.value);
              set({});
            }}
            w={300}
            maw="100%"
          />
          <DateField
            label="Rechnungsdatum von"
            clearable
            value={filter.from}
            onChange={(from) => set({ from })}
            w={170}
          />
          <DateField label="bis" clearable value={filter.to} onChange={(to) => set({ to })} w={170} />
          <Select
            label="Status"
            placeholder="Alle"
            data={invoiceStatusOptions}
            clearable
            value={filter.status || null}
            onChange={(v) => set({ status: (v ?? '') as InvoiceStatus | '' })}
            w={150}
          />
          <Select
            label="E-Mail-Status"
            placeholder="Alle"
            data={emailStatusOptions}
            clearable
            value={filter.emailStatus || null}
            onChange={(v) => set({ emailStatus: (v ?? '') as EmailStatus | '' })}
            w={170}
          />
        </Group>

        {error && <ErrorState error={error} />}
        {isLoading ? (
          <LoadingState />
        ) : (
          data && (
            <>
              <Table.ScrollContainer minWidth={820}>
                <Table striped highlightOnHover>
                  <Table.Thead>
                    <Table.Tr>
                      <Table.Th>Nummer</Table.Th>
                      <Table.Th>Datum</Table.Th>
                      <Table.Th>Mitglied</Table.Th>
                      <Table.Th>Zeitraum</Table.Th>
                      <Table.Th>Status</Table.Th>
                      <Table.Th>E-Mail</Table.Th>
                      <Table.Th ta="right">Betrag</Table.Th>
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {data.items.length === 0 && <EmptyRow colSpan={7} />}
                    {data.items.map((i) => (
                      <Table.Tr key={i.id}>
                        <Table.Td>
                          <Anchor component={Link} to={`/rechnungen/${i.id}`}>
                            {i.invoiceNumber}
                          </Anchor>
                        </Table.Td>
                        <Table.Td>{formatDate(i.issueDate)}</Table.Td>
                        <Table.Td>
                          {i.memberNumber} – {i.memberName}
                        </Table.Td>
                        <Table.Td>{formatPeriod(i.periodStart, i.periodEnd)}</Table.Td>
                        <Table.Td>
                          <InvoiceStatusBadge status={i.status} />
                        </Table.Td>
                        <Table.Td>
                          <EmailStatusBadge status={i.emailStatus} />
                        </Table.Td>
                        <Table.Td ta="right">{formatCurrency(i.amount)}</Table.Td>
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
