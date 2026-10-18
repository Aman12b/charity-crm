import { zodResolver } from '@hookform/resolvers/zod';
import {
  Alert,
  Button,
  Group,
  NumberInput,
  Paper,
  Select,
  SimpleGrid,
  Stack,
  Text,
  TextInput,
} from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { useEffect, useMemo, useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { useNavigate, useSearchParams } from 'react-router';
import { z } from 'zod';
import { intervalLabels, methodOptions, type MemberListItem } from '../../api/types';
import { MoneyInput } from '../../components/badges';
import { DateField } from '../../components/DateField';
import { PageHeader } from '../../components/PageHeader';
import { formatPeriod, todayIso } from '../../lib/format';
import { handleFormError, notifySuccess } from '../../lib/forms';
import { optionalText } from '../../lib/schemas';
import { useRecordPayment, useSettings } from '../finance';
import { useMember, useMembers } from '../members/membersApi';
import { defaultAmount, monthOptions, periodFor } from './period';

const now = new Date();

const schema = z.object({
  memberId: z.string().min(1, 'Bitte ein Mitglied wählen.'),
  year: z
    .number({ message: 'Bitte ein Jahr eingeben.' })
    .int()
    .min(2000, 'Ungültiges Jahr.')
    .max(2100, 'Ungültiges Jahr.'),
  month: z.number().int().min(1).max(12).nullable(),
  amount: z.number({ message: 'Bitte einen Betrag eingeben.' }).gt(0, 'Der Betrag muss größer als 0 sein.'),
  paymentDate: z
    .string()
    .min(1, 'Bitte das Zahlungsdatum eingeben.')
    .refine((v) => v <= todayIso(), 'Das Zahlungsdatum darf nicht in der Zukunft liegen.'),
  method: z.enum(['BankTransfer', 'Cash', 'Other'], { message: 'Bitte eine Zahlungsart wählen.' }),
  reference: optionalText(200),
});
type FormValues = z.infer<typeof schema>;

/** FR-PAY-01: select member → period by interval → prefilled amount → method → save (creates the invoice). */
export function RecordPaymentPage() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const preselectedId = params.get('memberId') ?? '';
  const { data: settings } = useSettings();
  const record = useRecordPayment();

  const [search, setSearch] = useState('');
  const [debouncedSearch] = useDebouncedValue(search, 250);
  const { data: candidates } = useMembers({
    search: debouncedSearch,
    status: 'Active',
    interval: '',
    page: 1,
  });
  const { data: preselected } = useMember(preselectedId || undefined);
  const [selected, setSelected] = useState<MemberListItem | null>(null);
  const member = selected ?? (preselected && preselected.status === 'Active' ? preselected : null);

  const {
    register,
    control,
    handleSubmit,
    setValue,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      memberId: preselectedId,
      year: now.getFullYear(),
      month: now.getMonth() + 1,
      paymentDate: todayIso(),
      method: 'BankTransfer',
      reference: '',
    },
  });
  const [year, month] = useWatch({ control, name: ['year', 'month'] });

  // Interval decides the period type; fees prefill the amount whenever the member changes.
  useEffect(() => {
    if (!member) return;
    setValue('memberId', member.id);
    setValue('month', member.billingInterval === 'Monthly' ? (month ?? now.getMonth() + 1) : null);
    if (settings) setValue('amount', defaultAmount(member.billingInterval, settings));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [member?.id, settings]);

  const options = useMemo(() => {
    const list = [...(candidates?.items ?? [])];
    if (member && !list.some((m) => m.id === member.id)) list.unshift(member);
    return list.map((m) => ({
      value: m.id,
      label: `${m.memberNumber} – ${m.lastName} ${m.firstName} (${m.city})`,
    }));
  }, [candidates, member]);

  const onSubmit = handleSubmit(async (values) => {
    try {
      const result = await record.mutateAsync({ ...values, reference: values.reference || null });
      notifySuccess(`Zahlung erfasst, Rechnung ${result.invoiceNumber} wurde erstellt.`);
      navigate(`/rechnungen/${result.invoiceId}`);
    } catch (e) {
      handleFormError(e, setError, [
        'memberId',
        'year',
        'month',
        'amount',
        'paymentDate',
        'method',
        'reference',
      ]);
    }
  });

  const period = member && year ? periodFor(member.billingInterval, year, month) : null;

  return (
    <>
      <PageHeader title="Zahlung erfassen" />
      <Paper withBorder p="lg" maw={760}>
        <form onSubmit={onSubmit} noValidate>
          <Stack>
            {preselected && preselected.status !== 'Active' && (
              <Alert color="yellow">Für inaktive Mitglieder können keine Zahlungen erfasst werden.</Alert>
            )}
            <Controller
              control={control}
              name="memberId"
              render={({ field }) => (
                <Select
                  label="Mitglied"
                  placeholder="Name oder Mitgliedsnummer eingeben"
                  withAsterisk
                  searchable
                  filter={({ options: all }) => all}
                  nothingFoundMessage="Kein aktives Mitglied gefunden"
                  data={options}
                  value={field.value || null}
                  onSearchChange={setSearch}
                  onChange={(id) => {
                    field.onChange(id ?? '');
                    setSelected(candidates?.items.find((m) => m.id === id) ?? null);
                  }}
                  error={errors.memberId?.message}
                />
              )}
            />

            {member && (
              <Text size="sm" c="dimmed">
                Zahlungsintervall: <b>{intervalLabels[member.billingInterval]}</b>
              </Text>
            )}

            <SimpleGrid cols={{ base: 1, sm: 2 }}>
              {member?.billingInterval === 'Monthly' && (
                <Controller
                  control={control}
                  name="month"
                  render={({ field }) => (
                    <Select
                      label="Monat"
                      withAsterisk
                      data={monthOptions}
                      allowDeselect={false}
                      value={field.value ? String(field.value) : null}
                      onChange={(v) => field.onChange(v ? Number(v) : null)}
                      error={errors.month?.message}
                    />
                  )}
                />
              )}
              <Controller
                control={control}
                name="year"
                render={({ field }) => (
                  <NumberInput
                    label="Jahr"
                    withAsterisk
                    min={2000}
                    max={2100}
                    allowDecimal={false}
                    thousandSeparator={false}
                    value={field.value}
                    onChange={(v) => field.onChange(typeof v === 'number' ? v : undefined)}
                    error={errors.year?.message}
                  />
                )}
              />
            </SimpleGrid>
            {period && (
              <Text size="sm">
                Beitragszeitraum: <b>{formatPeriod(period.start, period.end)}</b>
              </Text>
            )}

            <SimpleGrid cols={{ base: 1, sm: 2 }}>
              <Controller
                control={control}
                name="amount"
                render={({ field }) => (
                  <MoneyInput
                    label="Betrag"
                    withAsterisk
                    value={field.value}
                    onChange={(v) => field.onChange(typeof v === 'number' ? v : undefined)}
                    error={errors.amount?.message}
                  />
                )}
              />
              <Controller
                control={control}
                name="paymentDate"
                render={({ field }) => (
                  <DateField
                    label="Zahlungsdatum"
                    withAsterisk
                    maxDate={todayIso()}
                    value={field.value}
                    onChange={field.onChange}
                    error={errors.paymentDate?.message}
                  />
                )}
              />
              <Controller
                control={control}
                name="method"
                render={({ field }) => (
                  <Select
                    label="Zahlungsart"
                    withAsterisk
                    data={methodOptions}
                    allowDeselect={false}
                    value={field.value}
                    onChange={(v) => field.onChange(v)}
                    error={errors.method?.message}
                  />
                )}
              />
              <TextInput
                label="Referenz / Notiz"
                {...register('reference')}
                error={errors.reference?.message}
              />
            </SimpleGrid>

            <Group justify="flex-end" mt="md">
              <Button variant="default" onClick={() => navigate(-1)}>
                Abbrechen
              </Button>
              <Button type="submit" loading={isSubmitting} disabled={!member}>
                Zahlung speichern
              </Button>
            </Group>
          </Stack>
        </form>
      </Paper>
    </>
  );
}
