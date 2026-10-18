import { Anchor, Button, Group, Paper, Table, Title } from '@mantine/core';
import { IconPlus } from '@tabler/icons-react';
import { Link, useNavigate } from 'react-router';
import { methodLabels, type Member } from '../../api/types';
import { EmailStatusBadge, InvoiceStatusBadge, PaymentStatusBadge } from '../../components/badges';
import { EmptyRow, ErrorState, LoadingState } from '../../components/common';
import { formatCurrency, formatDate, formatPeriod } from '../../lib/format';
import { useMemberInvoices, useMemberPayments } from '../finance';

/** FR-MEM-03: payments and invoices of a member – only rendered for Admin and Cashier. */
export function MemberFinance({ member }: { member: Member }) {
  const navigate = useNavigate();
  const payments = useMemberPayments(member.id);
  const invoices = useMemberInvoices(member.id);

  return (
    <>
      <Paper withBorder p="lg">
        <Group justify="space-between" mb="md">
          <Title order={4}>Zahlungen</Title>
          {member.status === 'Active' && (
            <Button
              size="xs"
              leftSection={<IconPlus size={14} />}
              onClick={() => navigate(`/zahlungen/neu?memberId=${member.id}`)}
            >
              Zahlung erfassen
            </Button>
          )}
        </Group>
        {payments.error && <ErrorState error={payments.error} />}
        {payments.isLoading ? (
          <LoadingState />
        ) : (
          <Table.ScrollContainer minWidth={600}>
            <Table striped>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>Zeitraum</Table.Th>
                  <Table.Th>Zahlungsdatum</Table.Th>
                  <Table.Th>Zahlungsart</Table.Th>
                  <Table.Th>Status</Table.Th>
                  <Table.Th ta="right">Betrag</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {payments.data?.length === 0 && <EmptyRow colSpan={5}>Noch keine Zahlungen.</EmptyRow>}
                {payments.data?.map((p) => (
                  <Table.Tr key={p.id}>
                    <Table.Td>{formatPeriod(p.periodStart, p.periodEnd)}</Table.Td>
                    <Table.Td>{formatDate(p.paymentDate)}</Table.Td>
                    <Table.Td>{methodLabels[p.method]}</Table.Td>
                    <Table.Td>
                      <PaymentStatusBadge status={p.status} />
                    </Table.Td>
                    <Table.Td ta="right">{formatCurrency(p.amount)}</Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </Table.ScrollContainer>
        )}
      </Paper>

      <Paper withBorder p="lg">
        <Title order={4} mb="md">
          Rechnungen
        </Title>
        {invoices.error && <ErrorState error={invoices.error} />}
        {invoices.isLoading ? (
          <LoadingState />
        ) : (
          <Table.ScrollContainer minWidth={600}>
            <Table striped>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>Nummer</Table.Th>
                  <Table.Th>Datum</Table.Th>
                  <Table.Th>Zeitraum</Table.Th>
                  <Table.Th>Status</Table.Th>
                  <Table.Th>E-Mail</Table.Th>
                  <Table.Th ta="right">Betrag</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {invoices.data?.length === 0 && <EmptyRow colSpan={6}>Noch keine Rechnungen.</EmptyRow>}
                {invoices.data?.map((i) => (
                  <Table.Tr key={i.id}>
                    <Table.Td>
                      <Anchor component={Link} to={`/rechnungen/${i.id}`}>
                        {i.invoiceNumber}
                      </Anchor>
                    </Table.Td>
                    <Table.Td>{formatDate(i.issueDate)}</Table.Td>
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
        )}
      </Paper>
    </>
  );
}
