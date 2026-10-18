import { zodResolver } from '@hookform/resolvers/zod';
import { Button, Group, Paper, SimpleGrid, Stack, Text, Textarea, TextInput, Title } from '@mantine/core';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import { MoneyInput } from '../../components/badges';
import { ErrorState, LoadingState } from '../../components/common';
import { PageHeader } from '../../components/PageHeader';
import { formatDateTime } from '../../lib/format';
import { handleFormError, notifySuccess } from '../../lib/forms';
import { optionalEmailSchema, optionalText, requiredText } from '../../lib/schemas';
import { useSettings, useUpdateSettings, type SettingsInput } from '../finance';

const fee = z.number({ message: 'Bitte einen Betrag eingeben.' }).gt(0, 'Der Betrag muss größer als 0 sein.');

const schema = z.object({
  name: requiredText(200),
  street: requiredText(200),
  postalCode: requiredText(10),
  city: requiredText(100),
  country: optionalText(100),
  email: optionalEmailSchema,
  phone: optionalText(30),
  iban: z
    .string()
    .trim()
    .refine((v) => v === '' || /^[A-Z]{2}\d{2}[A-Z0-9]{11,30}$/.test(v.replace(/\s/g, '').toUpperCase()), {
      message: 'Bitte eine gültige IBAN eingeben.',
    }),
  bic: z
    .string()
    .trim()
    .refine(
      (v) => v === '' || /^[A-Za-z0-9]{8}([A-Za-z0-9]{3})?$/.test(v),
      'Bitte einen gültigen BIC eingeben.',
    ),
  zvrNumber: optionalText(20),
  invoiceFooterText: optionalText(1000),
  monthlyFee: fee,
  yearlyFee: fee,
});
type FormValues = z.infer<typeof schema>;

const fields = Object.keys(schema.shape);

/** FR-FEE-01/02: organization data and default fees (Admin only). */
export function SettingsPage() {
  const { data, isLoading, error } = useSettings();
  const update = useUpdateSettings();
  const {
    register,
    control,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  useEffect(() => {
    if (data) {
      reset({
        ...data,
        email: data.email ?? '',
        phone: data.phone ?? '',
        iban: data.iban ?? '',
        bic: data.bic ?? '',
        zvrNumber: data.zvrNumber ?? '',
        invoiceFooterText: data.invoiceFooterText ?? '',
      });
    }
  }, [data, reset]);

  if (isLoading) return <LoadingState />;
  if (error) return <ErrorState error={error} />;

  const onSubmit = handleSubmit(async (values) => {
    const input: SettingsInput = {
      ...values,
      email: values.email || null,
      phone: values.phone || null,
      iban: values.iban || null,
      bic: values.bic || null,
      zvrNumber: values.zvrNumber || null,
      invoiceFooterText: values.invoiceFooterText || null,
    };
    try {
      await update.mutateAsync(input);
      notifySuccess('Die Einstellungen wurden gespeichert.');
    } catch (e) {
      handleFormError(e, setError, fields);
    }
  });

  return (
    <>
      <PageHeader title="Organisation & Beiträge" />
      <form onSubmit={onSubmit} noValidate>
        <Stack maw={900}>
          <Paper withBorder p="lg">
            <Title order={4} mb="sm">
              Organisation
            </Title>
            <Text size="sm" c="dimmed" mb="md">
              Diese Angaben erscheinen auf neuen Rechnungen. Bereits ausgestellte Rechnungen bleiben
              unverändert.
            </Text>
            <Stack>
              <TextInput label="Name" withAsterisk {...register('name')} error={errors.name?.message} />
              <TextInput
                label="Straße und Hausnummer"
                withAsterisk
                {...register('street')}
                error={errors.street?.message}
              />
              <SimpleGrid cols={{ base: 1, sm: 3 }}>
                <TextInput
                  label="PLZ"
                  withAsterisk
                  {...register('postalCode')}
                  error={errors.postalCode?.message}
                />
                <TextInput label="Ort" withAsterisk {...register('city')} error={errors.city?.message} />
                <TextInput label="Land" {...register('country')} error={errors.country?.message} />
              </SimpleGrid>
              <SimpleGrid cols={{ base: 1, sm: 3 }}>
                <TextInput label="E-Mail" type="email" {...register('email')} error={errors.email?.message} />
                <TextInput label="Telefon" {...register('phone')} error={errors.phone?.message} />
                <TextInput label="ZVR-Zahl" {...register('zvrNumber')} error={errors.zvrNumber?.message} />
              </SimpleGrid>
              <SimpleGrid cols={{ base: 1, sm: 2 }}>
                <TextInput label="IBAN" {...register('iban')} error={errors.iban?.message} />
                <TextInput label="BIC" {...register('bic')} error={errors.bic?.message} />
              </SimpleGrid>
              <Textarea
                label="Fußzeile auf Rechnungen"
                autosize
                minRows={2}
                {...register('invoiceFooterText')}
                error={errors.invoiceFooterText?.message}
              />
            </Stack>
          </Paper>

          <Paper withBorder p="lg">
            <Title order={4} mb="sm">
              Standardbeiträge
            </Title>
            <Text size="sm" c="dimmed" mb="md">
              Werden beim Erfassen einer Zahlung vorgeschlagen. Änderungen gelten nur für künftige Zahlungen.
            </Text>
            <SimpleGrid cols={{ base: 1, sm: 2 }}>
              <Controller
                control={control}
                name="monthlyFee"
                render={({ field }) => (
                  <MoneyInput
                    label="Monatsbeitrag"
                    withAsterisk
                    value={field.value}
                    onChange={(v) => field.onChange(typeof v === 'number' ? v : undefined)}
                    error={errors.monthlyFee?.message}
                  />
                )}
              />
              <Controller
                control={control}
                name="yearlyFee"
                render={({ field }) => (
                  <MoneyInput
                    label="Jahresbeitrag"
                    withAsterisk
                    value={field.value}
                    onChange={(v) => field.onChange(typeof v === 'number' ? v : undefined)}
                    error={errors.yearlyFee?.message}
                  />
                )}
              />
            </SimpleGrid>
          </Paper>

          <Group justify="space-between">
            <Text size="xs" c="dimmed">
              Zuletzt geändert: {formatDateTime(data?.updatedAt)}
            </Text>
            <Button type="submit" loading={isSubmitting} disabled={!isDirty}>
              Speichern
            </Button>
          </Group>
        </Stack>
      </form>
    </>
  );
}
