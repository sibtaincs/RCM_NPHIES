import axios, {
  type AxiosError,
  type AxiosInstance,
  type InternalAxiosRequestConfig,
} from 'axios';
import { useAuthStore } from '@/store/authStore';

const baseURL = import.meta.env.VITE_API_BASE_URL ?? '/api';

/**
 * Shared Axios instance for all API calls.
 * - Attaches the JWT access token on every request.
 * - On 401, attempts a single token refresh, then retries the original request.
 * - On refresh failure, clears the session and redirects to /login.
 */
export const apiClient: AxiosInstance = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
});

// ---- Request interceptor: attach bearer token ----
apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// ---- Response interceptor: refresh-on-401 ----
let isRefreshing = false;
let pendingQueue: Array<(token: string | null) => void> = [];

function flushQueue(token: string | null) {
  pendingQueue.forEach((cb) => cb(token));
  pendingQueue = [];
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status !== 401 || original._retry) {
      return Promise.reject(error);
    }

    original._retry = true;

    if (isRefreshing) {
  // Queue requests while a refresh is already in flight.
      return new Promise((resolve, reject) => {
  pendingQueue.push((token) => {
          if (token) {
  original.headers.Authorization = `Bearer ${token}`;
      resolve(apiClient(original));
     } else {
   reject(error);
   }
        });
      });
 }

isRefreshing = true;
    try {
      const newToken = await useAuthStore.getState().refresh();
      flushQueue(newToken);
      if (newToken) {
        original.headers.Authorization = `Bearer ${newToken}`;
        return apiClient(original);
      }
      return Promise.reject(error);
    } catch (refreshError) {
   flushQueue(null);
      useAuthStore.getState().logout();
      window.location.href = '/login';
      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  },
);
