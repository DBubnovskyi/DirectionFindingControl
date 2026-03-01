import React from 'react';
import { createRoot } from 'react-dom/client';
import { MantineProvider } from '@mantine/core';
import App from './App';
import '@mantine/core/styles.css';
import './zsu-colors.css';

const container = document.getElementById('root');
const root = createRoot(container);
root.render(
  <MantineProvider defaultColorScheme="dark">
    <App />
  </MantineProvider>
);
