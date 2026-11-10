import { useState } from 'react';
import { Card, Form, Input, Button, Typography, Alert } from 'antd';
import { UserOutlined, LockOutlined } from '@ant-design/icons';
import { useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '@/store/authStore';
import type { LoginRequest } from '@/types/auth';

export function LoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const login = useAuthStore((s) => s.login);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const from = (location.state as { from?: { pathname: string } })?.from?.pathname ?? '/';

  const onFinish = async (values: LoginRequest) => {
    setLoading(true);
    setError(null);
    try {
      const ok = await login(values);
      if (ok) {
        navigate(from, { replace: true });
      } else {
    setError(t('auth.invalid'));
      }
    } catch {
      setError(t('auth.invalid'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
 style={{
        minHeight: '100vh',
        display: 'flex',
     alignItems: 'center',
        justifyContent: 'center',
        background: '#f0f2f5',
      }}
    >
      <Card style={{ width: 380 }}>
    <Typography.Title level={3} style={{ textAlign: 'center' }}>
     {t('app.title')}
    </Typography.Title>
   {error && <Alert type="error" message={error} style={{ marginBottom: 16 }} />}
      <Form layout="vertical" onFinish={onFinish} requiredMark={false}>
          <Form.Item
    name="username"
        label={t('auth.username')}
    rules={[{ required: true }]}
          >
     <Input prefix={<UserOutlined />} size="large" autoComplete="username" />
     </Form.Item>
          <Form.Item
            name="password"
    label={t('auth.password')}
     rules={[{ required: true }]}
          >
     <Input.Password prefix={<LockOutlined />} size="large" autoComplete="current-password" />
          </Form.Item>
          <Button type="primary" htmlType="submit" size="large" block loading={loading}>
      {t('auth.login')}
        </Button>
        </Form>
      </Card>
    </div>
  );
}
