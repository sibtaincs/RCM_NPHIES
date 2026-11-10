import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { Result, Button } from 'antd';
import { useAuthStore } from '@/store/authStore';
import type { AppRole } from '@/types/auth';

interface ProtectedRouteProps {
  /** If provided, the user must have at least one of these roles. */
roles?: AppRole[];
}

/**
 * Route guard:
 * - Redirects unauthenticated users to /login (preserving the intended path).
 * - Shows a 403 page if the user lacks the required role(s).
 */
export function ProtectedRoute({ roles }: ProtectedRouteProps) {
  const location = useLocation();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const hasAnyRole = useAuthStore((s) => s.hasAnyRole);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  if (roles && roles.length > 0 && !hasAnyRole(roles)) {
    return (
      <Result
     status="403"
        title="403"
     subTitle="You are not authorized to access this page."
        extra={
        <Button type="primary" href="/">
     Back Home
       </Button>
        }
      />
    );
  }

  return <Outlet />;
}
