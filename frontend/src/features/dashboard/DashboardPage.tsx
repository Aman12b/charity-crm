import { Anchor, Group, Paper, SimpleGrid, Text, ThemeIcon } from '@mantine/core';
import {
  IconAlertTriangle,
  IconCash,
  IconClock,
  IconUserPlus,
  IconUsers,
  type Icon,
} from '@tabler/icons-react';
import type { ReactNode } from 'react';
import { Link } from 'react-router';
import { useAuth } from '../../auth/useAuth';
import { ErrorState, LoadingState } from '../../components/common';
import { PageHeader } from '../../components/PageHeader';
import { formatCurrency } from '../../lib/format';
import { useDashboard } from '../reports/reportsApi';

function StatTile({
  label,
  value,
  hint,
  icon: TileIcon,
  warning,
  to,
}: {
  label: string;
  value: ReactNode;
  hint?: string;
  icon: Icon;
  warning?: boolean;
  to?: string;
}) {
  const content = (
    <Paper withBorder p="md" h="100%">
      <Group justify="space-between" align="flex-start" wrap="nowrap">
        <div>
          <Text size="sm" c="dimmed" fw={500}>
            {label}
          </Text>
          <Text fz={28} fw={700} lh={1.3}>
            {value}
          </Text>
          {hint && (
            <Text size="xs" c="dimmed">
              {hint}
            </Text>
          )}
        </div>
        {/* Status is never color alone: warning tiles also get an icon and the hint text. */}
        <ThemeIcon variant="light" color={warning ? 'red' : 'teal'} size="lg" aria-hidden>
          {warning ? <IconAlertTriangle size={20} /> : <TileIcon size={20} />}
        </ThemeIcon>
      </Group>
    </Paper>
  );
  return to ? (
    <Anchor component={Link} to={to} underline="never" c="inherit">
      {content}
    </Anchor>
  ) : (
    content
  );
}

/** FR-REP-01: key figures per role. */
export function DashboardPage() {
  const { user } = useAuth();
  const { data, isLoading, error } = useDashboard();
  const monthLabel = new Intl.DateTimeFormat('de-AT', { month: 'long', year: 'numeric' }).format(new Date());

  if (isLoading) return <LoadingState />;
  if (error || !data) return <ErrorState error={error} />;

  return (
    <>
      <PageHeader title={`Willkommen, ${user?.firstName}`} />
      <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }}>
        <StatTile label="Aktive Mitglieder" value={data.activeMembers} icon={IconUsers} to="/mitglieder" />
        <StatTile
          label="Neue Mitglieder"
          value={data.newMembersThisMonth}
          hint={monthLabel}
          icon={IconUserPlus}
        />
        {data.paymentsThisMonthCount !== null && (
          <StatTile
            label="Zahlungen diesen Monat"
            value={formatCurrency(data.paymentsThisMonthSum)}
            hint={`${data.paymentsThisMonthCount} Zahlungen · ${monthLabel}`}
            icon={IconCash}
            to="/zahlungen"
          />
        )}
        {data.openContributions !== null && (
          <StatTile
            label="Offene Beiträge"
            value={data.openContributions}
            hint="für den aktuellen Zeitraum"
            icon={IconClock}
            to="/berichte"
          />
        )}
        {data.failedEmails !== null && (
          <StatTile
            label="Fehlgeschlagene E-Mails"
            value={data.failedEmails}
            hint={data.failedEmails > 0 ? 'Bitte Rechnungen erneut senden' : 'Alle E-Mails zugestellt'}
            icon={IconAlertTriangle}
            warning={data.failedEmails > 0}
            to="/rechnungen"
          />
        )}
      </SimpleGrid>
    </>
  );
}
