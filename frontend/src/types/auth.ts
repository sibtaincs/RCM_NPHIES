/** Application role name. Roles are data-driven (managed in the Roles tab),
 * so this is a plain string rather than a fixed union.
 */
export type AppRole = string;

/** Authenticated user context returned by the API (UserSecurityContext). */
export interface AuthUser {
  userId: string;
  username: string;
  email?: string | null;
  roles: string[];
  permissions: string[];
  organizationId?: string | null;
  departmentId?: string | null;
  isMfaVerified?: boolean;
}

/** Login request body ? POST /api/auth/login. */
export interface LoginRequest {
  username: string;
  password: string;
}

/** JWT payload returned inside LoginResponse.token. */
export interface JwtToken {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  issuedAt?: string;
  expiresAt?: string;
}

/** Login response from the backend AuthController. */
export interface LoginResponse {
  success: boolean;
  message?: string | null;
  /** NOTE: this is an object, not a string. */
  token?: JwtToken | null;
  user?: AuthUser | null;
  mfaRequired?: boolean;
}
