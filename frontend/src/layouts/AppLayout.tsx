import { Layout, Menu, Button, Typography, Space, Avatar } from 'antd';
import type { MenuProps } from 'antd';
import {
  AlertOutlined,
  AuditOutlined,
  BankOutlined,
  DashboardOutlined,
  DatabaseOutlined,
  FileDoneOutlined,
  FileTextOutlined,
  GlobalOutlined,
  GroupOutlined,
  LogoutOutlined,
  MessageOutlined,
  PaperClipOutlined,
  SafetyCertificateOutlined,
  SolutionOutlined,
  TableOutlined,
  TeamOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '@/store/authStore';
import { PERMISSIONS } from '@/hooks/usePermission';
import { GlobalFilterBar } from '@/components/GlobalFilterBar/GlobalFilterBar';

const { Header, Sider, Content } = Layout;

type Item = { key: string; icon: React.ReactNode; label: string; perm?: string; roles?: string[] };

/** Main authenticated shell: grouped sidebar nav + global hospital/date filter + content outlet. */
export function AppLayout() {
  const navigate = useNavigate();
  const location = useLocation();
  const { t, i18n } = useTranslation();
  const user = useAuthStore((s) => s.user);
  const logout = useAuthStore((s) => s.logout);
  const hasAnyRole = useAuthStore((s) => s.hasAnyRole);
  const can = useAuthStore((s) => s.can);

  const visible = (items: Item[]) =>
    items
      .filter((i) => (!i.perm || can(i.perm)) && (!i.roles || hasAnyRole(i.roles)))
      .map(({ key, icon, label }) => ({ key, icon, label }));

  const rcmItems = visible([
    { key: '/rcm/eligibility', icon: <SafetyCertificateOutlined />, label: t('nav.eligibility'), perm: PERMISSIONS.EligibilityRead },
    { key: '/rcm/preauths', icon: <SolutionOutlined />, label: t('nav.preauths'), perm: PERMISSIONS.PreAuthRead },
    { key: '/rcm/claims', icon: <FileTextOutlined />, label: t('nav.rcmClaims'), perm: PERMISSIONS.ClaimsRead },
    { key: '/rcm/batches', icon: <GroupOutlined />, label: t('nav.batches'), perm: PERMISSIONS.ClaimsRead },
    { key: '/rcm/communications', icon: <MessageOutlined />, label: t('nav.communications'), perm: PERMISSIONS.ClaimsRead },
    { key: '/rcm/documents', icon: <PaperClipOutlined />, label: t('nav.documents'), perm: PERMISSIONS.ClaimsRead },
    { key: '/rcm/payments', icon: <BankOutlined />, label: t('nav.payments'), perm: PERMISSIONS.PaymentsRead },
    { key: '/rcm/denials', icon: <AlertOutlined />, label: t('nav.denials'), perm: PERMISSIONS.ClaimsRead },
  ]);
  const dataItems = visible([
    { key: '/data', icon: <TableOutlined />, label: t('nav.explorer') },
    { key: '/master-data', icon: <DatabaseOutlined />, label: t('nav.masterData') },
    { key: '/claims', icon: <AuditOutlined />, label: t('nav.claims') },
  ]);

  const menuItems: MenuProps['items'] = [
    { key: '/', icon: <DashboardOutlined />, label: t('nav.dashboard') },
    ...(rcmItems.length ? [{ type: 'group' as const, key: 'g-rcm', label: t('nav.rcm'), children: rcmItems }] : []),
    { type: 'group' as const, key: 'g-data', label: t('nav.data'), children: dataItems },
    ...(hasAnyRole(['Admin']) ? [{ key: '/users', icon: <TeamOutlined />, label: t('nav.users') }] : []),
  ];

  const allKeys = ['/', ...rcmItems.map((i) => i.key), ...dataItems.map((i) => i.key), '/users'];
  const selected =
    allKeys
      .filter((k) => k !== '/' && (location.pathname === k || location.pathname.startsWith(`${k}/`)))
      .sort((a, b) => b.length - a.length)[0] ?? (location.pathname === '/' ? '/' : '');

  const toggleLang = () => {
    const next = i18n.language === 'en' ? 'ar' : 'en';
    void i18n.changeLanguage(next);
    document.dir = next === 'ar' ? 'rtl' : 'ltr';
  };

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider breakpoint="lg" collapsedWidth="0" width={232}>
        <div style={{ height: 48, margin: 16, color: '#fff', fontWeight: 600, display: 'flex', alignItems: 'center', gap: 8 }}>
          <FileDoneOutlined />
          {t('app.title')}
        </div>
        <Menu theme="dark" mode="inline" selectedKeys={[selected]} items={menuItems} onClick={({ key }) => navigate(key)} />
      </Sider>
      <Layout>
        <Header
          style={{
            background: '#fff',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: 16,
            paddingInline: 24,
            height: 'auto',
            minHeight: 64,
            lineHeight: 'normal',
            flexWrap: 'wrap',
          }}
        >
          <GlobalFilterBar />
          <Space size="middle">
            <Button icon={<GlobalOutlined />} onClick={toggleLang}>
              {t('common.language')}
            </Button>
            <Space>
              <Avatar icon={<UserOutlined />} />
              <Typography.Text>{user?.username ?? 'user'}</Typography.Text>
            </Space>
            <Button
              icon={<LogoutOutlined />}
              onClick={() => {
                logout();
                navigate('/login');
              }}
            >
              {t('auth.logout')}
            </Button>
          </Space>
        </Header>
        <Content style={{ margin: 24 }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}