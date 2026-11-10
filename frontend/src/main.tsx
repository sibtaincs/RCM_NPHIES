import React from 'react';
import ReactDOM from 'react-dom/client';
import { ConfigProvider } from 'antd';
import enUS from 'antd/locale/en_US';
import { QueryClientProvider } from '@tanstack/react-query';
import { queryClient } from '@/api/queryClient';
import { App } from '@/App';
import '@/locales/i18n';
import 'antd/dist/reset.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ConfigProvider locale={enUS} theme={{ token: { colorPrimary: '#1677ff' } }}>
   <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
  </ConfigProvider>
  </React.StrictMode>,
);
