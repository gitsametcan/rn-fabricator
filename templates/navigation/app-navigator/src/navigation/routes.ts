export const ROUTES = {
  auth: {
    signIn: 'auth.signIn',
    forgotPassword: 'auth.forgotPassword',
  },
  main: {
    home: 'main.home',
  },
  tabs: {
    home: 'tabs.home',
    settings: 'tabs.settings',
  },
} as const;

export const TAB_ROUTES = [
  { name: ROUTES.tabs.home, title: 'Home' },
  { name: ROUTES.tabs.settings, title: 'Settings' },
] as const;
