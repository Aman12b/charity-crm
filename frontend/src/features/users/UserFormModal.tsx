import { zodResolver } from '@hookform/resolvers/zod';
import { Button, Group, Modal, PasswordInput, Select, SimpleGrid, Stack, TextInput } from '@mantine/core';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import { roleOptions, type User } from '../../api/types';
import { handleFormError, notifySuccess } from '../../lib/forms';
import { emailSchema, passwordSchema, requiredText, roleSchema } from '../../lib/schemas';
import { useUserMutations } from './usersApi';

const baseSchema = z.object({
  firstName: requiredText(100),
  lastName: requiredText(100),
  email: emailSchema,
  role: roleSchema,
  temporaryPassword: z.string(),
});
const createSchema = baseSchema.extend({ temporaryPassword: passwordSchema });
type FormValues = z.infer<typeof baseSchema>;

/** Create (FR-USR-01) or edit (FR-USR-03) a user. */
export function UserFormModal({
  user,
  opened,
  onClose,
}: {
  user: User | null;
  opened: boolean;
  onClose: () => void;
}) {
  const isEdit = !!user;
  const { create, update } = useUserMutations();
  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(isEdit ? baseSchema : createSchema),
    values: {
      firstName: user?.firstName ?? '',
      lastName: user?.lastName ?? '',
      email: user?.email ?? '',
      role: user?.role ?? 'Secretary',
      temporaryPassword: '',
    },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      if (user) {
        const { firstName, lastName, email, role } = values;
        await update.mutateAsync({ firstName, lastName, email, role, id: user.id, version: user.version });
        notifySuccess('Der Benutzer wurde aktualisiert.');
      } else {
        await create.mutateAsync(values);
        notifySuccess('Der Benutzer wurde angelegt.');
      }
      onClose();
    } catch (e) {
      handleFormError(e, setError, ['firstName', 'lastName', 'email', 'role', 'temporaryPassword']);
    }
  });

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={isEdit ? 'Benutzer bearbeiten' : 'Neuer Benutzer'}
      size="lg"
    >
      <form onSubmit={onSubmit} noValidate>
        <Stack>
          <SimpleGrid cols={{ base: 1, sm: 2 }}>
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
          </SimpleGrid>
          <TextInput
            label="E-Mail"
            type="email"
            withAsterisk
            {...register('email')}
            error={errors.email?.message}
          />
          <Controller
            control={control}
            name="role"
            render={({ field }) => (
              <Select
                label="Rolle"
                withAsterisk
                data={roleOptions}
                allowDeselect={false}
                value={field.value}
                onChange={(v) => field.onChange(v)}
                error={errors.role?.message}
              />
            )}
          />
          {!isEdit && (
            <PasswordInput
              label="Temporäres Passwort"
              description="Der Benutzer muss es bei der ersten Anmeldung ändern."
              withAsterisk
              autoComplete="new-password"
              {...register('temporaryPassword')}
              error={errors.temporaryPassword?.message}
            />
          )}
          <Group justify="flex-end">
            <Button variant="default" onClick={onClose}>
              Abbrechen
            </Button>
            <Button type="submit" loading={isSubmitting}>
              Speichern
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
