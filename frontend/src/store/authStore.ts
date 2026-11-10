import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { authApi } from '@/api/endpoints/auth';
import type { AppRole, AuthUser, LoginRequest } from '@/types/auth';

interface AuthState {
    accessToken: string | null;
    refreshToken: string | null;
    user: AuthUser | null;
    isAuthenticated: boolean;

    login: (credentials: LoginRequest) => Promise<boolean>;
    logout: () => void;
    /** Returns a fresh access token, or null if refresh failed. */
    refresh: () => Promise<string | null>;
    hasAnyRole: (roles: AppRole[]) => boolean;
    /** True when the user holds the permission (Admin implicitly holds all). */
    can: (permission: string) => boolean;
}

/**
 * Global auth/session store (persisted to localStorage).
 * The Axios client reads tokens from here and calls `refresh()` on 401.
 */
export const useAuthStore = create<AuthState>()(
    persist(
        (set, get) => ({
            accessToken: null,
            refreshToken: null,
            user: null,
            isAuthenticated: false,

            login: async (credentials) => {
                const res = await authApi.login(credentials);
                // The backend nests the JWT inside `token` as an object.
                const accessToken = res.token?.accessToken ?? null;
                if (!res.success || !accessToken) {
                    return false;
                }
                set({
                    accessToken,
                    refreshToken: res.token?.refreshToken ?? null,
                    user: res.user ?? null,
                    isAuthenticated: true,
                });
                return true;
            },

            logout: () => {
                set({
                    accessToken: null,
                    refreshToken: null,
                    user: null,
                    isAuthenticated: false,
                });
            },

            refresh: async () => {
                const rt = get().refreshToken;
                if (!rt) return null;
                try {
                    const res = await authApi.refresh(rt);
                    const accessToken = res.token?.accessToken ?? null;
                    if (res.success && accessToken) {
                        set({
                            accessToken,
                            refreshToken: res.token?.refreshToken ?? rt,
                            user: res.user ?? get().user,
                        });
                        return accessToken;
                    }
                    return null;
                } catch {
                    return null;
                }
            },

            hasAnyRole: (roles) => {
                const userRoles = get().user?.roles ?? [];
                return roles.some((r) => userRoles.includes(r));
            },

            can: (permission) => {
                const user = get().user;
                if (!user) return false;
                // Admin implicitly has every permission (mirrors the backend handler).
                if (user.roles?.includes('Admin')) return true;
                return user.permissions?.includes(permission) ?? false;
            },
        }),
        { name: 'nphies-auth' },
    ),
);
