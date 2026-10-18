import {
  Anchor,
  Button,
  Group,
  NumberInput,
  Paper,
  Select,
  Table,
  Tabs,
  Text,
  TextInput,
} from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconDownload, IconSearch } from '@tabler/icons-react';
import { useState } from 'react';
import { Link } from 'react-router';
import { intervalLabels } from '../../api/types';
import { EmptyRow, ErrorState, LoadingState, TablePager } from '../../components/common';
import { PageHeader } from '../../components/PageHeader';
import { formatCurrency, formatPeriod, monthName } from '../../lib/format';
import { notifyError } from '../../lib/forms';
import { monthOptions } from '../payments/period';
import { exportPaymentSummary, useOpenContributions, usePaymentSummary } from './reportsApi';

const now = new Date();

function YearInput({ value, onChange }: { value: number; onChange: (year: number) => void }) {
  return (
    <NumberInput
      label="Jahr"
      min={2000}
      max={2100}
      allowDecimal={false}
      thousandSeparator={false}
      value={value}
      onChange={(v) => typeof v === 'number' && v >= 2000 && v <= 2100 && onChange(v)}
      w={110}
    />
  );
}

/** FR-REP-02 (open contributions) and FR-REP-03 (payment summary). */
export function ReportsPage() {
  return (
    <>
      <PageHeader title="Berichte" />
      <Tabs defaultValue="open" keepMounted={false}>
        <Tabs.List mb="md">
          <Tabs.Tab value="open">Offene Beiträge</Tabs.Tab>
          <Tabs.Tab value="summary">Zahlungsübersicht</Tabs.Tab>
        </Tabs.List>
        <Tabs.Panel value="open">
          <OpenContributionsReport />
        </Tabs.Panel>
        <Tabs.Panel value="summary">
          <PaymentSummaryReport />
        </Tabs.Panel>
      </Tabs>
    </>
  );
}

function OpenContributionsReport() {
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [debouncedSearch] = useDebouncedValue(search, 300);
  const { data, isLoading, error } = useOpenContributions({ year, month, search: debouncedSearch, page });

  return (
    <Paper withBorder p="md">
      <Group mb="xs" align="flex-end">
        <Select
          label="Monat"
          data={monthOptions}
          allowDeselect={false}
          value={String(month)}
          onChange={(v) => {
            setMonth(Number(v));
            setPage(1);
          }}
          w={150}
        />
        <YearInput
          value={year}
          onChange={(y) => {
            setYear(y);
            setPage(1);
          }}
        />
        <TextInput
          label="Suche"
          placeholder="Name oder Mitgliedsnummer"
          leftSection={<IconSearch size={16} />}
          value={search}
          onChange={(e) => {
            setSearch(e.currentTarget.value);
            setPage(1);
          }}
          w={260}
        />
      </Group>
      <Text size="sm" c="dimmed" mb="md">
        Aktive Mitglieder ohne Zahlung für {monthName(month)} {year} (Monatszahler) bzw. das Jahr {year}{' '}
        (Jahreszahler).
      </Text>

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
                    <Table.Th>Mitglied</Table.Th>
                    <Table.Th>Intervall</Table.Th>
                    <Table.Th>Offener Zeitraum</Table.Th>
                    <Table.Th>E-Mail</Table.Th>
                    <Table.Th ta="right">Standardbeitrag</Table.Th>
                    <Table.Th aria-label="Aktionen" />
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {data.items.length === 0 && <EmptyRow colSpan={6}>Keine offenen Beiträge.</EmptyRow>}
                  {data.items.map((o) => (
                    <Table.Tr key={o.memberId}>
                      <Table.Td>
                        <Anchor component={Link} to={`/mitglieder/${o.memberId}`}>
                          {o.memberNumber} – {o.lastName} {o.firstName}
                        </Anchor>
                      </Table.Td>
                      <Table.Td>{intervalLabels[o.billingInterval]}</Table.Td>
                      <Table.Td>{formatPeriod(o.periodStart, o.periodEnd)}</Table.Td>
                      <Table.Td>{o.email ?? '–'}</Table.Td>
                      <Table.Td ta="right">{formatCurrency(o.expectedAmount)}</Table.Td>
                      <Table.Td>
                        <Anchor component={Link} to={`/zahlungen/neu?memberId=${o.memberId}`} size="sm">
                          Zahlung erfassen
                        </Anchor>
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
              onChange={setPage}
            />
          </>
        )
      )}
    </Paper>
  );
}

function PaymentSummaryReport() {
  const [year, setYear] = useState(now.getFullYear());
  const [exporting, setExporting] = useState(false);
  const { data, isLoading, error } = usePaymentSummary(year);

  const onExport = async () => {
    setExporting(true);
    try {
      await exportPaymentSummary(year);
    } catch (e) {
      notifyError(e);
    } finally {
      setExporting(false);
    }
  };

  return (
    <Paper withBorder p="md" maw={640}>
      <Group mb="md" align="flex-end" justify="space-between">
        <YearInput value={year} onChange={setYear} />
        <Button
          variant="default"
          leftSection={<IconDownload size={16} />}
          loading={exporting}
          onClick={onExport}
        >
          CSV exportieren
        </Button>
      </Group>
      {error && <ErrorState error={error} />}
      {isLoading ? (
        <LoadingState />
      ) : (
        data && (
          <Table striped>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Monat</Table.Th>
                <Table.Th ta="right">Anzahl</Table.Th>
                <Table.Th ta="right">Summe</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {data.months.map((m) => (
                <Table.Tr key={m.month}>
                  <Table.Td>{monthName(m.month)}</Table.Td>
                  <Table.Td ta="right">{m.count}</Table.Td>
                  <Table.Td ta="right">{formatCurrency(m.sum)}</Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
            <Table.Tfoot>
              <Table.Tr>
                <Table.Th>Gesamt {data.year}</Table.Th>
                <Table.Th ta="right">{data.totalCount}</Table.Th>
                <Table.Th ta="right">{formatCurrency(data.totalSum)}</Table.Th>
              </Table.Tr>
            </Table.Tfoot>
          </Table>
        )
      )}
      <Text size="xs" c="dimmed" mt="xs">
        Erfasste Zahlungen nach Zahlungsdatum; stornierte Zahlungen sind nicht enthalten.
      </Text>
    </Paper>
  );
}
