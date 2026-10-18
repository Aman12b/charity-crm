import { Text } from '@mantine/core';
import { Route, Routes } from 'react-router';
import { AppLayout } from './components/AppLayout';
import { PageHeader } from './components/PageHeader';

export default function App() {
  return (
    <Routes>
      <Route element={<AppLayout role="Admin" />}>
        <Route
          index
          element={
            <>
              <PageHeader title="Dashboard" />
              <Text>Willkommen im Charity CRM.</Text>
            </>
          }
        />
      </Route>
    </Routes>
  );
}
