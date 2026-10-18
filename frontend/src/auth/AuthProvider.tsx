import { useQuery, useQueryClient } from '@tanstack/react-query';
import { createContext, useCallback, useEffect, useMemo, type ReactNode } from 'react';
import { ApiError, apiGet, apiPost, setUnauthorizedHandler } from '../api/client';
import type { CurrentUser } from '../api/types';

export interface AuthContextValue {
  user: CurrentUser | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<CurrentUser>;
  logout: () => Promise<void>;
  setUser: (user: CurrentUser) => void;
}

// eslint-disable-next-line react-refresh/only-export-components
export const AuthContext = createContext<AuthContextValue | null>(null);

const meKey = ['auth', 'me'];

async function fetchMe(): Promise<CurrentUser | null> {
  try {
    return await apiGet<CurrentUser>('/auth/me');
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      return null;
    }
    throw error;
  }
}

/** Loads the session from /api/auth/me on start; a 401 anywhere returns the user to the login page. */
export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const { data, isLoading } = useQuery({ queryKey: meKey, queryFn: fetchMe, staleTime: Infinity });

  const clearSession = useCallback(() => {
    queryClient.removeQueries({ predicate: (q) => q.queryKey[0] !== 'auth' });
    queryClient.setQueryData(meKey, null);
  }, [queryClient]);

  useEffect(() => {
    setUnauthorizedHandler(clearSession);
    return () => setUnauthorizedHandler(null);
  }, [clearSession]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user: data ?? null,
      isLoading,
      login: async (email, password) => {
        const user = await apiPost<CurrentUser>('/auth/login', { email, password });
        queryClient.setQueryData(meKey, user);
        return user;
      },
      logout: async () => {
        try {
          await apiPost('/auth/logout');
        } finally {
          clearSession();
        }
      },
      setUser: (user) => queryClient.setQueryData(meKey, user),
    }),
    [data, isLoading, queryClient, clearSession],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
