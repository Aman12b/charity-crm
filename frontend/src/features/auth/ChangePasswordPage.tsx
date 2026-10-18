import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Button, Paper, PasswordInput, Stack, Text } from '@mantine/core';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router';
import { z } from 'zod';
import { apiPost } from '../../api/client';
import type { CurrentUser } from '../../api/types';
import { useAuth } from '../../auth/useAuth';
import { PageHeader } from '../../components/PageHeader';
import { handleFormError, notifySuccess } from '../../lib/forms';
import { passwordSchema } from '../../lib/schemas';

const schema = z
  .object({
    currentPassword: z.string().min(1, 'Bitte das aktuelle Passwort eingeben.'),
    newPassword: passwordSchema,
    confirmPassword: z.string(),
  })
  .refine((v) => v.newPassword === v.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Die Passwörter stimmen nicht überein.',
  });
type FormValues = z.infer<typeof schema>;

/** FR-AUTH-05/06 */
export function ChangePasswordPage() {
  const { user, setUser } = useAuth();
  const navigate = useNavigate();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  });

  const onSubmit = handleSubmit(async ({ currentPassword, newPassword }) => {
    try {
      const updated = await apiPost<CurrentUser>('/auth/change-password', { currentPassword, newPassword });
      setUser(updated);
      notifySuccess('Ihr Passwort wurde geändert.');
      navigate('/', { replace: true });
    } catch (e) {
      handleFormError(e, setError, ['currentPassword', 'newPassword']);
    }
  });

  return (
    <>
      <PageHeader title="Passwort ändern" />
      <Paper withBorder p="lg" maw={460}>
        <form onSubmit={onSubmit} noValidate>
          <Stack>
            {user?.mustChangePassword && (
              <Alert color="yellow">Bitte vergeben Sie ein neues Passwort, bevor Sie fortfahren.</Alert>
            )}
            <PasswordInput
              label="Aktuelles Passwort"
              autoComplete="current-password"
              {...register('currentPassword')}
              error={errors.currentPassword?.message}
            />
            <PasswordInput
              label="Neues Passwort"
              autoComplete="new-password"
              {...register('newPassword')}
              error={errors.newPassword?.message}
            />
            <PasswordInput
              label="Neues Passwort wiederholen"
              autoComplete="new-password"
              {...register('confirmPassword')}
              error={errors.confirmPassword?.message}
            />
            <Text size="sm" c="dimmed">
              Mindestens 10 Zeichen, davon mindestens ein Buchstabe und eine Ziffer.
            </Text>
            <Button type="submit" loading={isSubmitting}>
              Passwort ändern
            </Button>
          </Stack>
        </form>
      </Paper>
    </>
  );
}
