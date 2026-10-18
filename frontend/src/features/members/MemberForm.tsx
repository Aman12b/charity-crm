import { zodResolver } from '@hookform/resolvers/zod';
import { Button, Group, Select, SimpleGrid, Stack, Textarea, TextInput, Title } from '@mantine/core';
import { Controller, useForm, type UseFormSetError } from 'react-hook-form';
import { intervalOptions, type MemberInput } from '../../api/types';
import { DateField } from '../../components/DateField';
import { memberSchema } from './memberSchema';

interface MemberFormProps {
  initialValues: MemberInput;
  submitLabel: string;
  onSubmit: (values: MemberInput, setError: UseFormSetError<MemberInput>) => Promise<void>;
  onCancel: () => void;
}

/** Create/edit form for members (FR-MEM-01, FR-MEM-04). */
export function MemberForm({ initialValues, submitLabel, onSubmit, onCancel }: MemberFormProps) {
  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<MemberInput>({ resolver: zodResolver(memberSchema), defaultValues: initialValues });

  return (
    <form onSubmit={handleSubmit((values) => onSubmit(values, setError))} noValidate>
      <Stack>
        <Title order={4}>Person</Title>
        <SimpleGrid cols={{ base: 1, sm: 3 }}>
          <TextInput
            label="Vorname"
            withAsterisk
            {...register('firstName')}
            error={errors.firstName?.message}
          />
          <TextInput
            label="Nachname"
            withAsterisk
            {...register('lastName')}
            error={errors.lastName?.message}
          />
          <Controller
            control={control}
            name="dateOfBirth"
            render={({ field }) => (
              <DateField
                label="Geburtsdatum"
                withAsterisk
                value={field.value}
                onChange={field.onChange}
                onBlur={field.onBlur}
                error={errors.dateOfBirth?.message}
              />
            )}
          />
        </SimpleGrid>

        <Title order={4} mt="sm">
          Adresse
        </Title>
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

        <Title order={4} mt="sm">
          Kontakt
        </Title>
        <SimpleGrid cols={{ base: 1, sm: 2 }}>
          <TextInput
            label="E-Mail"
            type="email"
            description="Für den Versand der Rechnungen"
            {...register('email')}
            error={errors.email?.message}
          />
          <TextInput label="Telefon" type="tel" {...register('phone')} error={errors.phone?.message} />
        </SimpleGrid>

        <Title order={4} mt="sm">
          Mitgliedschaft
        </Title>
        <SimpleGrid cols={{ base: 1, sm: 2 }}>
          <Controller
            control={control}
            name="billingInterval"
            render={({ field }) => (
              <Select
                label="Zahlungsintervall"
                withAsterisk
                data={intervalOptions}
                allowDeselect={false}
                value={field.value}
                onChange={(v) => field.onChange(v)}
                error={errors.billingInterval?.message}
              />
            )}
          />
          <Controller
            control={control}
            name="joinDate"
            render={({ field }) => (
              <DateField
                label="Eintrittsdatum"
                withAsterisk
                value={field.value}
                onChange={field.onChange}
                onBlur={field.onBlur}
                error={errors.joinDate?.message}
              />
            )}
          />
        </SimpleGrid>
        <Textarea label="Notizen" autosize minRows={2} {...register('notes')} error={errors.notes?.message} />

        <Group justify="flex-end" mt="md">
          <Button variant="default" onClick={onCancel}>
            Abbrechen
          </Button>
          <Button type="submit" loading={isSubmitting}>
            {submitLabel}
          </Button>
        </Group>
      </Stack>
    </form>
  );
}
