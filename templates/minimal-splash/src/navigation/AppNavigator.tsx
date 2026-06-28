import React from 'react';
import { AuthNavigator } from './AuthNavigator';
import { MainNavigator } from './MainNavigator';
import type { AppNavigatorProps } from './types';

export function AppNavigator({ isAuthenticated = true }: AppNavigatorProps) {
  return isAuthenticated ? <MainNavigator /> : <AuthNavigator />;
}
