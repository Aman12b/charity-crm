import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Button, Center, Paper, PasswordInput, Stack, TextInput, Title } from '@mantine/core';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Navigate, useLocation, useNavigate } from 'react-router';
import { z } from 'zod';
import { useAuth } from '../../auth/useAuth';
import { errorMessage } from '../../lib/forms';

const schema = z.object({
  email: z.string().trim().min(1, 'Bitte die E-Mail-Adresse eingeben.'),
  password: z.string().min(1, 'Bitte das Passwort eingeben.'),
});
type FormValues = z.infer<typeof schema>;

export function LoginPage() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [error, setError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { email: '', password: '' } });

  if (user) {
    return <Navigate to="/" replace />;
  }

  const onSubmit = handleSubmit(async (values) => {
    setError(null);
    try {
      const loggedIn = await login(values.email, values.password);
      const from = (location.state as { from?: string } | null)?.from;
      navigate(loggedIn.mustChangePassword ? '/passwort-aendern' : (from ?? '/'), { replace: true });
    } catch (e) {
      setError(errorMessage(e));
    }
  });

  return (
    <Center mih="100vh" p="md" bg="gray.0">
      <Paper withBorder shadow="sm" p="xl" w={380} maw="100%">
        <form onSubmit={onSubmit} noValidate>
          <Stack>
            <Title order={2} ta="center">
              Charity CRM
            </Title>
            <Title order={4} ta="center" c="dimmed" fw={400}>
              Anmeldung
            </Title>
            {error && (
              <Alert color="red" role="alert">
                {error}
              </Alert>
            )}
            <TextInput
              label="E-Mail"
              type="email"
              autoComplete="username"
              autoFocus
              {...register('email')}
              error={errors.email?.message}
            />
            <PasswordInput
              label="Passwort"
              autoComplete="current-password"
              {...register('password')}
              error={errors.password?.message}
            />
            <Button type="submit" loading={isSubmitting} fullWidth>
              Anmelden
            </Button>
          </Stack>
        </form>
      </Paper>
    </Center>
  );
}
