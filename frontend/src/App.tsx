import { Text } from '@mantine/core';
import { Route, Routes } from 'react-router';
import { RequireAuth, RequireRole } from './auth/guards';
import { useAuth } from './auth/useAuth';
import { AppLayout } from './components/AppLayout';
import { PageHeader } from './components/PageHeader';
import { UserMenu } from './components/UserMenu';
import { ChangePasswordPage } from './features/auth/ChangePasswordPage';
import { LoginPage } from './features/auth/LoginPage';
import { UsersPage } from './features/users/UsersPage';

function DashboardPlaceholder() {
  return (
    <>
      <PageHeader title="Dashboard" />
      <Text>Willkommen im Charity CRM.</Text>
    </>
  );
}

function NotFoundPage() {
  return (
    <>
      <PageHeader title="Seite nicht gefunden" />
      <Text>Die angeforderte Seite existiert nicht.</Text>
    </>
  );
}

export default function App() {
  const { user } = useAuth();

  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<RequireAuth />}>
        <Route
          element={
            <AppLayout role={user?.mustChangePassword ? undefined : user?.role} headerRight={<UserMenu />} />
          }
        >
          <Route path="/passwort-aendern" element={<ChangePasswordPage />} />
          <Route index element={<DashboardPlaceholder />} />
          <Route element={<RequireRole roles={['Admin']} />}>
            <Route path="/benutzer" element={<UsersPage />} />
          </Route>
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Route>
    </Routes>
  );
}
