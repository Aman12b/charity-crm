import { Alert, Center, Group, Loader, Pagination, Table, Text } from '@mantine/core';
import type { ReactNode } from 'react';
import { errorMessage } from '../lib/forms';

export function LoadingState() {
  return (
    <Center py="xl">
      <Loader aria-label="Wird geladen" />
    </Center>
  );
}

export function ErrorState({ error }: { error: unknown }) {
  return (
    <Alert color="red" title="Fehler" role="alert">
      {errorMessage(error)}
    </Alert>
  );
}

export function EmptyRow({
  colSpan,
  children = 'Keine Einträge gefunden.',
}: {
  colSpan: number;
  children?: ReactNode;
}) {
  return (
    <Table.Tr>
      <Table.Td colSpan={colSpan}>
        <Text c="dimmed" ta="center" py="md">
          {children}
        </Text>
      </Table.Td>
    </Table.Tr>
  );
}

export function TablePager({
  page,
  pageSize,
  totalCount,
  onChange,
}: {
  page: number;
  pageSize: number;
  totalCount: number;
  onChange: (page: number) => void;
}) {
  const pages = Math.max(1, Math.ceil(totalCount / pageSize));
  return (
    <Group justify="space-between" mt="md">
      <Text size="sm" c="dimmed">
        {totalCount} {totalCount === 1 ? 'Eintrag' : 'Einträge'}
      </Text>
      {pages > 1 && (
        <Pagination
          value={page}
          onChange={onChange}
          total={pages}
          size="sm"
          getControlProps={(control) => ({
            'aria-label':
              control === 'previous' ? 'Vorherige Seite' : control === 'next' ? 'Nächste Seite' : control,
          })}
        />
      )}
    </Group>
  );
}
