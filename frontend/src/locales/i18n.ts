import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

const en = {
  app: { title: 'NPHIES RCM Console' },
  nav: {
    dashboard: 'Dashboard',
    rcm: 'Revenue cycle',
    eligibility: 'Eligibility',
    preauths: 'Prior authorizations',
    rcmClaims: 'Claims',
    batches: 'Batches',
    communications: 'Communications',
    payments: 'Payments',
    documents: 'Claim documents',
    denials: 'Denials & appeals',
    data: 'Data',
    explorer: 'Data explorer',
    masterData: 'Master Data',
    claims: 'Claim review',
    users: 'User Management',
  },
  filters: {
    allHospitals: 'All hospitals',
    dateHint: 'Date range applied to every page',
    today: 'Today',
    last7: 'Last 7 days',
    last30: 'Last 30 days',
    thisMonth: 'This month',
    last90: 'Last 90 days',
    thisYear: 'This year',
  },
  auth: {
    login: 'Sign in',
    username: 'Username',
    password: 'Password',
    logout: 'Sign out',
    invalid: 'Invalid username or password',
  },
  common: { language: 'العربية' },
};

const ar = {
  app: { title: 'منصة إدارة دورة الإيرادات - نفيس' },
  nav: {
    dashboard: 'لوحة المعلومات',
    rcm: 'دورة الإيرادات',
    eligibility: 'الأهلية',
    preauths: 'الموافقات المسبقة',
    rcmClaims: 'المطالبات',
    batches: 'الدفعات',
    communications: 'المراسلات',
    payments: 'المدفوعات',
    documents: 'مستندات المطالبات',
    denials: 'الرفض والاعتراضات',
    data: 'البيانات',
    explorer: 'مستكشف البيانات',
    masterData: 'البيانات الرئيسية',
    claims: 'مراجعة المطالبات',
    users: 'إدارة المستخدمين',
  },
  filters: {
    allHospitals: 'كل المستشفيات',
    dateHint: 'نطاق التاريخ مطبق على جميع الصفحات',
    today: 'اليوم',
    last7: 'آخر 7 أيام',
    last30: 'آخر 30 يومًا',
    thisMonth: 'هذا الشهر',
    last90: 'آخر 90 يومًا',
    thisYear: 'هذه السنة',
  },
  auth: {
    login: 'تسجيل الدخول',
    username: 'اسم المستخدم',
    password: 'كلمة المرور',
    logout: 'تسجيل الخروج',
    invalid: 'اسم المستخدم أو كلمة المرور غير صحيحة',
  },
  common: { language: 'English' },
};

void i18n.use(initReactI18next).init({
  resources: {
    en: { translation: en },
    ar: { translation: ar },
  },
  lng: import.meta.env.VITE_DEFAULT_LOCALE ?? 'en',
  fallbackLng: 'en',
  interpolation: { escapeValue: false },
});

export default i18n;