import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ProtectedRoute } from '@/routes/ProtectedRoute';
import { AppLayout } from '@/layouts/AppLayout';
import { LoginPage } from '@/features/auth/LoginPage';
import { DashboardPage } from '@/features/rcm-dashboard/DashboardPage';
import { MasterDataPage } from '@/features/master-data/MasterDataPage';
import { ClaimsPage } from '@/features/claims/ClaimsPage';
import { UsersPage } from '@/features/user-management/UsersPage';
import { DataExplorerPage } from '@/features/data-explorer/DataExplorerPage';
import { EligibilityPage } from '@/features/rcm/EligibilityPage';
import { RcmClaimsPage } from '@/features/rcm/RcmClaimsPage';
import { BatchesPage } from '@/features/rcm/BatchesPage';
import { CommunicationsPage } from '@/features/rcm/CommunicationsPage';
import { PaymentsPage } from '@/features/rcm/PaymentsPage';
import { DocumentsPage } from '@/features/rcm/DocumentsPage';
import { DenialsPage } from '@/features/rcm/DenialsPage';

export function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />

        {/* Authenticated shell */}
        <Route element={<ProtectedRoute />}>
          <Route element={<AppLayout />}>
            <Route path="/" element={<DashboardPage />} />
            <Route path="/rcm/eligibility" element={<EligibilityPage />} />
            <Route path="/rcm/preauths" element={<RcmClaimsPage key="preauth" mode="preauth" />} />
            <Route path="/rcm/claims" element={<RcmClaimsPage key="claim" mode="claim" />} />
            <Route path="/rcm/batches" element={<BatchesPage />} />
            <Route path="/rcm/communications" element={<CommunicationsPage />} />
            <Route path="/rcm/payments" element={<PaymentsPage />} />
            <Route path="/rcm/documents" element={<DocumentsPage />} />
            <Route path="/rcm/denials" element={<DenialsPage />} />
            <Route path="/data/:table?" element={<DataExplorerPage />} />
            <Route path="/master-data" element={<MasterDataPage />} />
            <Route path="/claims" element={<ClaimsPage />} />
          </Route>
        </Route>

        {/* Admin-only section */}
        <Route element={<ProtectedRoute roles={['Admin']} />}>
          <Route element={<AppLayout />}>
            <Route path="/users" element={<UsersPage />} />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}