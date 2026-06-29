import React, { useEffect, useState } from 'react';
import { MainScreen, SplashScreen } from './src/screens';

export default function App() {
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    const timer = setTimeout(() => setIsReady(true), 900);

    return () => clearTimeout(timer);
  }, []);

  return isReady ? <MainScreen /> : <SplashScreen />;
}
