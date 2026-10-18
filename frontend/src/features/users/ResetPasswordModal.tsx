import { zodResolver } from '@hookform/resolvers/zod';
import { Button, Group, Modal, PasswordInput, Stack, Text } from '@mantine/core';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import type { User } from '../../api/types';
import { handleFormError, notifySuccess } from '../../lib/forms';
import { passwordSchema } from '../../lib/schemas';
import { useUserMutations } from './usersApi';

const schema = z.object({ temporaryPassword: passwordSchema });
type FormValues = z.infer<typeof schema>;

/** FR-USR-05 */
export function ResetPasswordModal({ user, onClose }: { user: User | null; onClose: () => void }) {
  const { resetPassword } = useUserMutations();
  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { temporaryPassword: '' } });

  const close = () => {
    reset();
    onClose();
  };

  const onSubmit = handleSubmit(async ({ temporaryPassword }) => {
    if (!user) return;
    try {
      await resetPassword.mutateAsync({ id: user.id, temporaryPassword });
      notifySuccess('Das Passwort wurde zurückgesetzt.');
      close();
    } catch (e) {
      handleFormError(e, setError, ['temporaryPassword']);
    }
  });

  return (
    <Modal opened={!!user} onClose={close} title="Passwort zurücksetzen">
      <form onSubmit={onSubmit} noValidate>
        <Stack>
          <Text size="sm">
            Neues temporäres Passwort für{' '}
            <b>
              {user?.firstName} {user?.lastName}
            </b>
            . Der Benutzer muss es bei der nächsten Anmeldung ändern.
          </Text>
          <PasswordInput
            label="Temporäres Passwort"
            withAsterisk
            autoComplete="new-password"
            {...register('temporaryPassword')}
            error={errors.temporaryPassword?.message}
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={close}>
              Abbrechen
            </Button>
            <Button type="submit" loading={isSubmitting}>
              Zurücksetzen
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}
