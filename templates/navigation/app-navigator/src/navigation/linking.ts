import { ROUTES } from './routes';

export const linkingConfig = {
  prefixes: ['myapp://'],
  config: {
    screens: {
      [ROUTES.auth.signIn]: 'sign-in',
      [ROUTES.main.home]: 'home',
      [ROUTES.tabs.settings]: 'settings',
    },
  },
};
