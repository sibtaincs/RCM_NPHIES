import { useAuthStore } from '@/store/authStore';

/**
 * Permission helpers for conditionally rendering UI.
 *
 * ```tsx
 * const { can } = usePermission();
 * {can(PERMISSIONS.ClaimsSubmit) && <Button>Submit</Button>}
 * ```
 */
export function usePermission() {
  const can = useAuthStore((s) => s.can);
  const hasAnyRole = useAuthStore((s) => s.hasAnyRole);
  const permissions = useAuthStore((s) => s.user?.permissions ?? []);

  return {
    can,
 /** True when the user holds at least one of the permissions. */
    canAny: (list: string[]) => list.some((p) => can(p)),
    /** True when the user holds every permission in the list. */
    canAll: (list: string[]) => list.every((p) => can(p)),
    hasAnyRole,
    permissions,
  };
}

/** Permission name constants — mirrors ApiService/Security/Permissions.cs. */
export const PERMISSIONS = {
  ClaimsRead: 'claims:read',
  ClaimsCreate: 'claims:create',
  ClaimsUpdate: 'claims:update',
  ClaimsDelete: 'claims:delete',
  ClaimsSubmit: 'claims:submit',
  ClaimsTechnicalReview: 'claims:technical-review',
  ClaimsMedicalReview: 'claims:medical-review',

  PreAuthRead: 'preauth:read',
  PreAuthCreate: 'preauth:create',
  PreAuthSubmit: 'preauth:submit',
  PreAuthApprove: 'preauth:approve',

  EligibilityRead: 'eligibility:read',
  EligibilityCheck: 'eligibility:check',

  PatientsRead: 'patients:read',
  PatientsCreate: 'patients:create',
  PatientsUpdate: 'patients:update',

  MasterDataRead: 'masterdata:read',
  MasterDataManage: 'masterdata:manage',

  PaymentsRead: 'payments:read',
  PaymentsManage: 'payments:manage',

  ReportsRead: 'reports:read',
  ReportsExport: 'reports:export',

  UsersRead: 'users:read',
  UsersManage: 'users:manage',
  RolesRead: 'roles:read',
  RolesManage: 'roles:manage',

  AuditRead: 'audit:read',
} as const;
