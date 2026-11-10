import type { AppRole } from '@/types/auth';

/** User projection returned by /api/users (never includes password data). */
export interface UserRecord {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: AppRole[];
  isActive: boolean;
  isLocked: boolean;
  failedLoginAttempts: number;
  lastLoginAt?: string | null;
  createdAt: string;
  organizationId?: string | null;
  departmentId?: string | null;
}

/** Payload for POST /api/users. */
export interface CreateUserPayload {
  username: string;
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  roles: AppRole[];
  organizationId?: string | null;
  departmentId?: string | null;
}

/** Payload for PUT /api/users/{id}. Omitted members are left unchanged. */
export interface UpdateUserPayload {
  email?: string;
  firstName?: string;
  lastName?: string;
  roles?: AppRole[];
  organizationId?: string | null;
  departmentId?: string | null;
}

export interface UsersPage {
  items: UserRecord[];
  total: number;
}
