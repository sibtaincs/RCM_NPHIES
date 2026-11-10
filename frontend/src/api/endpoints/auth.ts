import { apiClient } from '@/api/client';
import type { LoginRequest, LoginResponse, AuthUser } from '@/types/auth';

/** Typed wrappers around the backend AuthController (/api/auth). */
export const authApi = {
  login: async (body: LoginRequest): Promise<LoginResponse> => {
    const { data } = await apiClient.post<LoginResponse>('/auth/login', body);
    return data;
  },

  refresh: async (refreshToken: string): Promise<LoginResponse> => {
    const { data } = await apiClient.post<LoginResponse>('/auth/refresh', { refreshToken });
    return data;
  },

  logout: async (): Promise<void> => {
    await apiClient.post('/auth/logout');
  },

  me: async (): Promise<AuthUser> => {
    // /api/auth/me wraps the payload in ApiResponse<T> { success, message, data }.
    const { data } = await apiClient.get<{ data: AuthUser }>('/auth/me');
    return data.data;
  },
};
