import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Anchor,
  Button,
  Grid,
  Group,
  Modal,
  Paper,
  Stack,
  Text,
  Textarea,
  Title,
} from '@mantine/core';
import { IconArrowLeft, IconDownload, IconMail } from '@tabler/icons-react';
import { useState, type ReactNode } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useParams } from 'react-router';
import { z } from 'zod';
import { intervalLabels, methodLabels, type Invoice } from '../../api/types';
import { EmailStatusBadge, InvoiceStatusBadge } from '../../components/badges';
import { ErrorState, LoadingState } from '../../components/common';
import { PageHeader } from '../../components/PageHeader';
import { formatCurrency, formatDate, formatDateTime, formatPeriod } from '../../lib/format';
import { handleFormError, notifyError, notifySuccess } from '../../lib/forms';
import { requiredText } from '../../lib/schemas';
import { downloadInvoicePdf, useCancelInvoice, useInvoice } from '../finance';
import { ResendInvoiceModal } from './ResendInvoiceModal';

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <Text size="xs" c="dimmed" tt="uppercase" fw={600}>
        {label}
      </Text>
      <Text>{children || '–'}</Text>
    </div>
  );
}

/** FR-INV-02 detail and download, FR-INV-04 cancellation. Invoices are read-only (FR-INV-03). */
export function InvoiceDetailPage() {
  const { id } = useParams();
  const { data: invoice, isLoading, error } = useInvoice(id);
  const [cancelling, setCancelling] = useState(false);
  const [downloading, setDownloading] = useState(false);

  if (isLoading) return <LoadingState />;
  if (error || !invoice) return <ErrorState error={error} />;

  const onDownload = async () => {
    setDownloading(true);
    try {
      await downloadInvoicePdf(invoice);
    } catch (e) {
      notifyError(e);
    } finally {
      setDownloading(false);
    }
  };

  const m = invoice.member;
  return (
    <>
      <Anchor component={Link} to="/rechnungen" size="sm">
        <Group gap={4}>
          <IconArrowLeft size={14} /> Zurück zur Liste
        </Group>
      </Anchor>
      <PageHeader
        title={`Rechnung ${invoice.invoiceNumber}`}
        actions={
          <>
            <Button
              variant="default"
              leftSection={<IconDownload size={16} />}
              loading={downloading}
              onClick={onDownload}
            >
              PDF herunterladen
            </Button>
            {invoice.status === 'Issued' && (
              <Button color="red" variant="light" onClick={() => setCancelling(true)}>
                Stornieren
              </Button>
            )}
          </>
        }
      />

      <Stack>
        {invoice.status === 'Cancelled' && (
          <Alert color="red" title="Storniert">
            Storniert am {formatDateTime(invoice.cancelledAt)}. Grund: {invoice.cancellationReason}
          </Alert>
        )}
        <Paper withBorder p="lg">
          <Group justify="space-between" mb="md">
            <Title order={4}>Rechnungsdaten</Title>
            <InvoiceStatusBadge status={invoice.status} />
          </Group>
          <Grid>
            <Grid.Col span={{ base: 12, sm: 4 }}>
              <Field label="Rechnungsdatum">{formatDate(invoice.issueDate)}</Field>
            </Grid.Col>
            <Grid.Col span={{ base: 12, sm: 4 }}>
              <Field label="Zeitraum">
                {formatPeriod(invoice.periodStart, invoice.periodEnd)} ({intervalLabels[invoice.interval]})
              </Field>
            </Grid.Col>
            <Grid.Col span={{ base: 12, sm: 4 }}>
              <Field label="Betrag">{formatCurrency(invoice.amount)}</Field>
            </Grid.Col>
            <Grid.Col span={{ base: 12, sm: 4 }}>
              <Field label="Zahlungsart">{methodLabels[invoice.paymentMethod]}</Field>
            </Grid.Col>
            <Grid.Col span={{ base: 12, sm: 4 }}>
              <Field label="Zahlungsdatum">{formatDate(invoice.paymentDate)}</Field>
            </Grid.Col>
            <Grid.Col span={{ base: 12, sm: 4 }}>
              <Field label="Referenz">{invoice.paymentReference}</Field>
            </Grid.Col>
          </Grid>
        </Paper>

        <Paper withBorder p="lg">
          <Title order={4} mb="md">
            Empfänger (Stand bei Ausstellung)
          </Title>
          <Grid>
            <Grid.Col span={{ base: 12, sm: 6 }}>
              <Field label="Mitglied">
                <Anchor component={Link} to={`/mitglieder/${invoice.memberId}`}>
                  {m.memberNumber} – {m.firstName} {m.lastName}
                </Anchor>
              </Field>
            </Grid.Col>
            <Grid.Col span={{ base: 12, sm: 6 }}>
              <Field label="Adresse">
                {m.street}, {m.postalCode} {m.city}, {m.country}
              </Field>
            </Grid.Col>
          </Grid>
        </Paper>

        <EmailSection invoice={invoice} />
      </Stack>

      <CancelInvoiceModal invoice={invoice} opened={cancelling} onClose={() => setCancelling(false)} />
    </>
  );
}

function EmailSection({ invoice }: { invoice: Invoice }) {
  const [resending, setResending] = useState(false);
  return (
    <Paper withBorder p="lg">
      <Group justify="space-between" mb="md">
        <Group gap="sm">
          <Title order={4}>E-Mail-Versand</Title>
          <EmailStatusBadge status={invoice.emailStatus} />
        </Group>
        <Button
          size="xs"
          variant="light"
          leftSection={<IconMail size={14} />}
          onClick={() => setResending(true)}
        >
          Erneut senden
        </Button>
      </Group>
      <ResendInvoiceModal invoice={invoice} opened={resending} onClose={() => setResending(false)} />
      <Grid>
        <Grid.Col span={{ base: 12, sm: 6 }}>
          <Field label="Empfänger">{invoice.member.email}</Field>
        </Grid.Col>
        <Grid.Col span={{ base: 12, sm: 6 }}>
          <Field label="Versendet am">{invoice.emailSentAt && formatDateTime(invoice.emailSentAt)}</Field>
        </Grid.Col>
        {invoice.emailLastError && (
          <Grid.Col span={12}>
            <Alert color="red" title="Letzter Fehler">
              {invoice.emailLastError}
            </Alert>
          </Grid.Col>
        )}
      </Grid>
    </Paper>
  );
}

const cancelSchema = z.object({ reason: requiredText(500) });

function CancelInvoiceModal({
  invoice,
  opened,
  onClose,
}: {
  invoice: Invoice;
  opened: boolean;
  onClose: () => void;
}) {
  const cancel = useCancelInvoice();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<{ reason: string }>({ resolver: zodResolver(cancelSchema), defaultValues: { reason: '' } });

  const onSubmit = handleSubmit(async ({ reason }) => {
    try {
      await cancel.mutateAsync({ id: invoice.id, reason });
      notifySuccess(`Die Rechnung ${invoice.invoiceNumber} wurde storniert.`);
      onClose();
    } catch (e) {
      handleFormError(e, setError, ['reason']);
    }
  });

  return (
    <Modal opened={opened} onClose={onClose} title="Rechnung stornieren">
      <form onSubmit={onSubmit} noValidate>
        <Stack>
          <Text size="sm">
            Rechnung <b>{invoice.invoiceNumber}</b> und die zugehörige Zahlung werden storniert. Die
            Rechnungsnummer wird nicht wiederverwendet; der Zeitraum kann danach erneut bezahlt werden.
          </Text>
          <Textarea
            label="Grund"
            withAsterisk
            autosize
            minRows={2}
            {...register('reason')}
            error={errors.reason?.message}
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={onClose}>
              Abbrechen
            </Button>
            <Button type="submit" color="red" loading={isSubmitting}>
              Stornieren
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
