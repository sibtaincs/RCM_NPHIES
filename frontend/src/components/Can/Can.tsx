import type { ReactNode } from 'react';
import { usePermission } from '@/hooks/usePermission';

interface CanProps {
  /** Required permission. */
  permission?: string;
  /** Any one of these permissions is sufficient. */
  anyOf?: string[];
  /** All of these permissions are required. */
  allOf?: string[];
  /** Rendered when the check fails. Defaults to nothing. */
  fallback?: ReactNode;
  children: ReactNode;
}

/**
 * Renders children only when the current user holds the required permission(s).
 *
 * ```tsx
 * <Can permission={PERMISSIONS.ClaimsSubmit}>
 *   <Button>Submit to NPHIES</Button>
 * </Can>
 * ```
 */
export function Can({ permission, anyOf, allOf, fallback = null, children }: CanProps) {
  const { can, canAny, canAll } = usePermission();

  let allowed = true;
  if (permission) allowed = allowed && can(permission);
  if (anyOf?.length) allowed = allowed && canAny(anyOf);
  if (allOf?.length) allowed = allowed && canAll(allOf);

  return <>{allowed ? children : fallback}</>;
}
