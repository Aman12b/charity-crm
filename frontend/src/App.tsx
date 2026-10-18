import { Text } from '@mantine/core';
import { Route, Routes } from 'react-router';
import { RequireAuth, RequireRole } from './auth/guards';
import { useAuth } from './auth/useAuth';
import { AppLayout } from './components/AppLayout';
import { PageHeader } from './components/PageHeader';
import { UserMenu } from './components/UserMenu';
import { ChangePasswordPage } from './features/auth/ChangePasswordPage';
import { LoginPage } from './features/auth/LoginPage';
import { MemberCreatePage } from './features/members/MemberCreatePage';
import { MemberDetailPage } from './features/members/MemberDetailPage';
import { MembersPage } from './features/members/MembersPage';
import { InvoiceDetailPage } from './features/invoices/InvoiceDetailPage';
import { InvoicesPage } from './features/invoices/InvoicesPage';
import { PaymentsPage } from './features/payments/PaymentsPage';
import { RecordPaymentPage } from './features/payments/RecordPaymentPage';
import { SettingsPage } from './features/settings/SettingsPage';
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
          <Route path="/mitglieder" element={<MembersPage />} />
          <Route path="/mitglieder/:id" element={<MemberDetailPage />} />
          <Route element={<RequireRole roles={['Admin', 'Secretary']} />}>
            <Route path="/mitglieder/neu" element={<MemberCreatePage />} />
          </Route>
          <Route element={<RequireRole roles={['Admin', 'Cashier']} />}>
            <Route path="/zahlungen" element={<PaymentsPage />} />
            <Route path="/zahlungen/neu" element={<RecordPaymentPage />} />
            <Route path="/rechnungen" element={<InvoicesPage />} />
            <Route path="/rechnungen/:id" element={<InvoiceDetailPage />} />
          </Route>
          <Route element={<RequireRole roles={['Admin']} />}>
            <Route path="/benutzer" element={<UsersPage />} />
            <Route path="/einstellungen" element={<SettingsPage />} />
          </Route>
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Route>
    </Routes>
  );
}
