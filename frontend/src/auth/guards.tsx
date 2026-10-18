import { Center, Loader } from '@mantine/core';
import { Navigate, Outlet, useLocation } from 'react-router';
import type { Role } from '../api/types';
import { useAuth } from './useAuth';

export const changePasswordPath = '/passwort-aendern';

/** Requires a session; while a password change is pending only that page is reachable (FR-AUTH-06). */
export function RequireAuth() {
  const { user, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <Center h="100vh">
        <Loader aria-label="Wird geladen" />
      </Center>
    );
  }
  if (!user) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }
  if (user.mustChangePassword && location.pathname !== changePasswordPath) {
    return <Navigate to={changePasswordPath} replace />;
  }
  return <Outlet />;
}

/** Hides routes the role may not use (NFR-UX-03). */
export function RequireRole({ roles }: { roles: Role[] }) {
  const { user } = useAuth();
  return user && roles.includes(user.role) ? <Outlet /> : <Navigate to="/" replace />;
}
