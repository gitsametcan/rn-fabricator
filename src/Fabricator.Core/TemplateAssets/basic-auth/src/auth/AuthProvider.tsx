import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';

export type AuthUser = {
  id: string;
  displayName: string;
  email: string;
};

export type AuthState =
  | { status: 'checking' }
  | { status: 'signedOut' }
  | { status: 'signingIn' }
  | { status: 'signedIn'; user: AuthUser };

type AuthContextValue = {
  state: AuthState;
  signIn: (email: string, password: string) => void;
  signOut: () => void;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

type AuthProviderProps = {
  children: React.ReactNode;
};

export function AuthProvider({ children }: AuthProviderProps) {
  const [state, setState] = useState<AuthState>({ status: 'checking' });

  useEffect(() => {
    const timer = setTimeout(() => {
      setState({ status: 'signedOut' });
    }, 350);

    return () => clearTimeout(timer);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      state,
      signIn: (email: string, _password: string) => {
        setState({ status: 'signingIn' });

        setTimeout(() => {
          setState({
            status: 'signedIn',
            user: {
              id: 'local-user',
              displayName: email.split('@')[0] || 'Developer',
              email,
            },
          });
        }, 350);
      },
      signOut: () => {
        setState({ status: 'signedOut' });
      },
    }),
    [state],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuthSession() {
  const value = useContext(AuthContext);

  if (!value) {
    throw new Error('useAuthSession must be used inside AuthProvider.');
  }

  return value;
}
