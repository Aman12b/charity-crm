import { Text } from '@mantine/core';
import { modals } from '@mantine/modals';
import type { ReactNode } from 'react';

/** Confirmation dialog for destructive actions (NFR-UX-04). */
export function confirmAction(options: {
  title: string;
  message: ReactNode;
  confirmLabel: string;
  onConfirm: () => void;
  danger?: boolean;
}) {
  modals.openConfirmModal({
    title: options.title,
    children: <Text size="sm">{options.message}</Text>,
    labels: { confirm: options.confirmLabel, cancel: 'Abbrechen' },
    confirmProps: { color: options.danger === false ? 'teal' : 'red' },
    onConfirm: options.onConfirm,
  });
}
