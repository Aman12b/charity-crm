import { useContext } from 'react';
import type { Role } from '../api/types';
import { AuthContext } from './AuthProvider';

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside <AuthProvider>.');
  }
  return context;
}

/** UI-only role check; the API enforces access itself (NFR-SEC-01). */
export function useHasRole(...roles: Role[]): boolean {
  const { user } = useAuth();
  return !!user && roles.includes(user.role);
}
