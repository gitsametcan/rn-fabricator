import { ROUTES } from './routes';

export type AuthRouteName =
  | typeof ROUTES.auth.signIn
  | typeof ROUTES.auth.forgotPassword;

export type MainRouteName = typeof ROUTES.main.home;

export type TabRouteName =
  | typeof ROUTES.tabs.home
  | typeof ROUTES.tabs.settings;

export type AppRouteName = AuthRouteName | MainRouteName | TabRouteName;

export type AppNavigatorProps = {
  isAuthenticated?: boolean;
};

export type NavigationCommand = {
  name: AppRouteName;
  params?: Record<string, unknown>;
};
