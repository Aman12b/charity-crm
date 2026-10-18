import { zodResolver } from '@hookform/resolvers/zod';
import { Button, Group, Modal, Stack, Text, TextInput } from '@mantine/core';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import type { Invoice } from '../../api/types';
import { handleFormError, notifySuccess } from '../../lib/forms';
import { optionalEmailSchema } from '../../lib/schemas';
import { useResendInvoice } from '../finance';

const schema = z.object({ email: optionalEmailSchema });

/** FR-MAIL-03: resend the invoice, optionally to a different address. */
export function ResendInvoiceModal({
  invoice,
  opened,
  onClose,
}: {
  invoice: Invoice;
  opened: boolean;
  onClose: () => void;
}) {
  const resend = useResendInvoice();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<{ email: string }>({
    resolver: zodResolver(schema),
    values: { email: invoice.member.email ?? '' },
  });

  const onSubmit = handleSubmit(async ({ email }) => {
    try {
      await resend.mutateAsync({ id: invoice.id, email: email.trim() || null });
      notifySuccess('Die Rechnung wird in Kürze erneut versendet.');
      onClose();
    } catch (e) {
      handleFormError(e, setError, ['email']);
    }
  });

  return (
    <Modal opened={opened} onClose={onClose} title="Rechnung erneut senden">
      <form onSubmit={onSubmit} noValidate>
        <Stack>
          <Text size="sm">
            Rechnung <b>{invoice.invoiceNumber}</b> wird als PDF-Anhang per E-Mail versendet.
          </Text>
          <TextInput
            label="E-Mail-Adresse"
            type="email"
            description="Bei Bedarf kann eine andere Adresse angegeben werden."
            {...register('email')}
            error={errors.email?.message}
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={onClose}>
              Abbrechen
            </Button>
            <Button type="submit" loading={isSubmitting}>
              Senden
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
