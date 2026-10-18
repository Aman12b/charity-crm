import { Anchor, Button, Group, NumberInput, Paper, Select, Table, Text } from '@mantine/core';
import { IconPlus } from '@tabler/icons-react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router';
import {
  methodLabels,
  methodOptions,
  paymentStatusOptions,
  type PaymentMethod,
  type PaymentStatus,
} from '../../api/types';
import { PaymentStatusBadge } from '../../components/badges';
import { EmptyRow, ErrorState, LoadingState, TablePager } from '../../components/common';
import { DateField } from '../../components/DateField';
import { PageHeader } from '../../components/PageHeader';
import { formatCurrency, formatDate, formatPeriod } from '../../lib/format';
import { usePayments, type PaymentFilter } from '../finance';
import { monthOptions } from './period';

const initialFilter: PaymentFilter = {
  from: '',
  to: '',
  memberId: '',
  method: '',
  status: 'Recorded',
  periodYear: '',
  periodMonth: '',
  page: 1,
};

/** FR-PAY-02: payment list with filters and the sum of the listed amounts. */
export function PaymentsPage() {
  const navigate = useNavigate();
  const [filter, setFilter] = useState(initialFilter);
  const { data, isLoading, error } = usePayments(filter);
  const set = (patch: Partial<PaymentFilter>) => setFilter({ ...filter, ...patch, page: 1 });

  return (
    <>
      <PageHeader
        title="Zahlungen"
        actions={
          <Button leftSection={<IconPlus size={16} />} onClick={() => navigate('/zahlungen/neu')}>
            Zahlung erfassen
          </Button>
        }
      />
      <Paper withBorder p="md">
        <Group mb="md" align="flex-end">
          <DateField
            label="Zahlungsdatum von"
            clearable
            value={filter.from}
            onChange={(from) => set({ from })}
            w={170}
          />
          <DateField label="bis" clearable value={filter.to} onChange={(to) => set({ to })} w={170} />
          <NumberInput
            label="Beitragsjahr"
            placeholder="Alle"
            min={2000}
            max={2100}
            allowDecimal={false}
            thousandSeparator={false}
            value={filter.periodYear}
            onChange={(v) =>
              set({ periodYear: typeof v === 'number' ? v : '', periodMonth: v ? filter.periodMonth : '' })
            }
            w={130}
          />
          <Select
            label="Beitragsmonat"
            placeholder="Alle"
            data={monthOptions}
            clearable
            disabled={!filter.periodYear}
            value={filter.periodMonth ? String(filter.periodMonth) : null}
            onChange={(v) => set({ periodMonth: v ? Number(v) : '' })}
            w={150}
          />
          <Select
            label="Zahlungsart"
            placeholder="Alle"
            data={methodOptions}
            clearable
            value={filter.method || null}
            onChange={(v) => set({ method: (v ?? '') as PaymentMethod | '' })}
            w={150}
          />
          <Select
            label="Status"
            placeholder="Alle"
            data={paymentStatusOptions}
            clearable
            value={filter.status || null}
            onChange={(v) => set({ status: (v ?? '') as PaymentStatus | '' })}
            w={140}
          />
          {filter.memberId && (
            <Button variant="subtle" onClick={() => set({ memberId: '' })}>
              Mitgliedsfilter entfernen
            </Button>
          )}
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
                      <Table.Th>Datum</Table.Th>
                      <Table.Th>Mitglied</Table.Th>
                      <Table.Th>Zeitraum</Table.Th>
                      <Table.Th>Zahlungsart</Table.Th>
                      <Table.Th>Rechnung</Table.Th>
                      <Table.Th>Status</Table.Th>
                      <Table.Th ta="right">Betrag</Table.Th>
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {data.items.length === 0 && <EmptyRow colSpan={7} />}
                    {data.items.map((p) => (
                      <Table.Tr key={p.id}>
                        <Table.Td>{formatDate(p.paymentDate)}</Table.Td>
                        <Table.Td>
                          <Anchor component={Link} to={`/mitglieder/${p.memberId}`}>
                            {p.memberNumber} – {p.memberName}
                          </Anchor>
                        </Table.Td>
                        <Table.Td>{formatPeriod(p.periodStart, p.periodEnd)}</Table.Td>
                        <Table.Td>{methodLabels[p.method]}</Table.Td>
                        <Table.Td>
                          {p.invoiceId && (
                            <Anchor component={Link} to={`/rechnungen/${p.invoiceId}`}>
                              {p.invoiceNumber}
                            </Anchor>
                          )}
                        </Table.Td>
                        <Table.Td>
                          <PaymentStatusBadge status={p.status} />
                        </Table.Td>
                        <Table.Td ta="right">{formatCurrency(p.amount)}</Table.Td>
                      </Table.Tr>
                    ))}
                  </Table.Tbody>
                  <Table.Tfoot>
                    <Table.Tr>
                      <Table.Th colSpan={6}>
                        Summe{filter.status !== 'Recorded' && ' (ohne stornierte Zahlungen)'}
                      </Table.Th>
                      <Table.Th ta="right">{formatCurrency(data.totalAmount)}</Table.Th>
                    </Table.Tr>
                  </Table.Tfoot>
                </Table>
              </Table.ScrollContainer>
              <TablePager
                page={data.page}
                pageSize={data.pageSize}
                totalCount={data.totalCount}
                onChange={(page) => setFilter({ ...filter, page })}
              />
              <Text size="xs" c="dimmed" mt="xs">
                Die Summe umfasst alle gefilterten Zahlungen über alle Seiten.
              </Text>
            </>
          )
        )}
      </Paper>
    </>
  );
}
