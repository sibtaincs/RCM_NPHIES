/** A role from /api/access/roles. */
export interface Role {
  id: string;
  name: string;
  displayName: string;
  description?: string | null;
  isSystemRole: boolean;
  isActive: boolean;
  permissionIds: string[];
  permissionNames: string[];
}

/** A permission from /api/access/permissions. */
export interface Permission {
  id: string;
  name: string;
  category: string;
  action: string;
  description?: string | null;
  isSystemPermission: boolean;
}

export interface SaveRolePayload {
  name?: string;
  displayName?: string;
  description?: string | null;
  isActive?: boolean;
  permissionIds?: string[];
}

export interface SavePermissionPayload {
  name?: string;
  category?: string;
  action?: string;
  description?: string | null;
}

/** One effective permission plus which roles granted it. */
export interface EffectivePermission {
  name: string;
  category: string;
  grantedByRoles: string[];
}

export interface EffectiveAccess {
  userId: string;
  username: string;
  roles: string[];
  permissions: EffectivePermission[];
}
