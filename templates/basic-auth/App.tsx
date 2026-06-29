import React from 'react';
import { AuthProvider, useAuthSession } from './src/auth/AuthProvider';
import { HomeScreen, LoadingScreen, LoginScreen, SplashScreen } from './src/screens';

function AuthFlow() {
  const { signIn, signOut, state } = useAuthSession();

  if (state.status === 'checking') {
    return <SplashScreen />;
  }

  if (state.status === 'signingIn') {
    return <LoadingScreen message="Signing you in" />;
  }

  if (state.status === 'signedIn') {
    return <HomeScreen displayName={state.user.displayName} onSignOut={signOut} />;
  }

  return <LoginScreen isSubmitting={false} onSubmit={signIn} />;
}

export default function App() {
  return (
    <AuthProvider>
      <AuthFlow />
    </AuthProvider>
  );
}
